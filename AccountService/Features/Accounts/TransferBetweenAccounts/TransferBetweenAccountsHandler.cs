using AccountService.Application.Models;
using AccountService.Features.Accounts.Abstractions;
using AutoMapper;
using MediatR;
using System.Data;

namespace AccountService.Features.Accounts.TransferBetweenAccounts;

public class TransferBetweenAccountsHandler(IAccountRepository repo, IMapper mapper,
    ICurrencyValidator currencyValidator) : IRequestHandler<TransferBetweenAccountsCommand, MbResult<IReadOnlyList<TransactionIdDto>>>
{
    public async Task<MbResult<IReadOnlyList<TransactionIdDto>>> Handle(TransferBetweenAccountsCommand request,
        CancellationToken cancellationToken)
    {
        if (!await currencyValidator.IsExists(request.Currency))
            throw new ArgumentException("Unsupported currency");

        Transaction creditTransaction;
        Transaction debitTransaction;

        await using var dbTransaction = await repo.BeginTransactionAsync(IsolationLevel.Serializable);
        try
        {
            var fromAccount = await repo.GetByIdForUpdateAsync(request.FromAccountId, dbTransaction);
            var toAccount = await repo.GetByIdForUpdateAsync(request.ToAccountId, dbTransaction);

            if (fromAccount is null)
                throw new KeyNotFoundException("Sender account not found");

            if (toAccount is null)
                throw new KeyNotFoundException("Recipient account not found");

            if (!toAccount.Currency.Equals(fromAccount.Currency, StringComparison.OrdinalIgnoreCase))
                throw new ArgumentException("Accounts with different currencies");

            creditTransaction = new Transaction
            {
                AccountId = request.FromAccountId,
                CounterpartyAccountId = request.ToAccountId,
                Amount = request.Amount,
                Currency = request.Currency.ToUpperInvariant(),
                Type = TransactionType.Credit,
                Description = request.Description
            };

            var totalBefore = fromAccount.Balance + toAccount.Balance;

            debitTransaction = new Transaction
            {
                AccountId = request.ToAccountId,
                CounterpartyAccountId = request.FromAccountId,
                Amount = request.Amount,
                Currency = request.Currency.ToUpperInvariant(),
                Type = TransactionType.Debit,
                Description = request.Description
            };

            fromAccount.ConductTransaction(creditTransaction);
            toAccount.ConductTransaction(debitTransaction);

            await repo.AddTransactionAsync(debitTransaction, dbTransaction);
            await repo.AddTransactionAsync(creditTransaction, dbTransaction);

            var updatedFrom = await repo.UpdateBalanceAsync(fromAccount, dbTransaction);
            var updatedTo = await repo.UpdateBalanceAsync(toAccount, dbTransaction);

            if (updatedFrom == 0 || updatedTo == 0)
            {
                throw new DBConcurrencyException("Account was modified by another transaction");
            }

            var totalAfter = fromAccount.Balance + toAccount.Balance;
            if (totalBefore != totalAfter)
            {
                throw new InvalidOperationException("Final balances mismatch — rolling back");
            }

            await dbTransaction.CommitAsync(cancellationToken);
        }
        catch
        {
            await dbTransaction.RollbackAsync(cancellationToken);
            throw;
        }

        var debitTransactionIdDto = mapper.Map<TransactionIdDto>(debitTransaction);
        var creditTransactionIdDto = mapper.Map<TransactionIdDto>(creditTransaction);

        return new MbResult<IReadOnlyList<TransactionIdDto>>([debitTransactionIdDto, creditTransactionIdDto]);  
    }
}