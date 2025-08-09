using AccountService.Application.Models;
using AccountService.Features.Accounts.Abstractions;
using AutoMapper;
using MediatR;

namespace AccountService.Features.Accounts.TransferBetweenAccounts;

public class TransferBetweenAccountsHandler(IAccountRepository repo, IMapper mapper,
    ICurrencyValidator currencyValidator) : IRequestHandler<TransferBetweenAccountsCommand, MbResult<IReadOnlyList<TransactionIdDto>>>
{
    public async Task<MbResult<IReadOnlyList<TransactionIdDto>>> Handle(TransferBetweenAccountsCommand request, CancellationToken cancellationToken)
    {
        var fromAccount = await repo.GetByIdAsync(request.FromAccountId);
        var toAccount = await repo.GetByIdAsync(request.ToAccountId);

        if (fromAccount is null)
            throw new KeyNotFoundException("Sender account not found");

        if (toAccount is null)
            throw new KeyNotFoundException("Recipient account not found");

        if (!await currencyValidator.IsExists(request.Currency))
            throw new ArgumentException("Unsupported currency");

        if (!toAccount.Currency.Equals(fromAccount.Currency))
            throw new ArgumentException("Accounts2 with different currencies");

        if(!toAccount.Currency.Equals(request.Currency.ToUpperInvariant()))
            throw new ArgumentException("Transaction currency is different from recipient account currency");

        if (!fromAccount.Currency.Equals(request.Currency.ToUpperInvariant()))
            throw new ArgumentException("Transaction currency is different from sender account currency");

        var creditTransaction = new Transaction
        {
            AccountId = request.FromAccountId,
            CounterpartyAccountId = request.ToAccountId,
            Amount = request.Amount,
            Currency = request.Currency,
            Type = TransactionType.Credit,
            Description = request.Description
        };

        var debitTransaction = new Transaction
        {
            AccountId = request.ToAccountId,
            CounterpartyAccountId = request.FromAccountId,
            Amount = request.Amount,
            Currency = request.Currency,
            Type = TransactionType.Debit,
            Description = request.Description
        };

        fromAccount.ConductTransaction(creditTransaction);
        toAccount.ConductTransaction(debitTransaction);

        using var dbTransaction = repo.BeginTransaction();
        try
        {
            await repo.UpdateBalanceAsync(fromAccount, dbTransaction);
            await repo.UpdateBalanceAsync(toAccount, dbTransaction);

            await repo.AddTransactionAsync(debitTransaction, dbTransaction);
            await repo.AddTransactionAsync(creditTransaction, dbTransaction);

            dbTransaction.Commit();
        }
        catch
        {
            dbTransaction.Rollback();
            throw;
        }

        var debitTransactionIdDto = mapper.Map<TransactionIdDto>(debitTransaction);
        var creditTransactionIdDto = mapper.Map<TransactionIdDto>(creditTransaction);

        return new MbResult<IReadOnlyList<TransactionIdDto>>([debitTransactionIdDto, creditTransactionIdDto]);  
    }
}