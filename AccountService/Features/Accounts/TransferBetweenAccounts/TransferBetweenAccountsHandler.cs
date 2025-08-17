using AccountService.Application.Abstractions;
using AccountService.Application.Models;
using AccountService.Features.Accounts.Abstractions;
using AutoMapper;
using MediatR;
using System.Data;
using AccountService.Application.Contracts;
using AccountService.Features.Accounts.Contracts;

namespace AccountService.Features.Accounts.TransferBetweenAccounts;

public class TransferBetweenAccountsHandler(IAccountRepository accRepo,
    IOutboxRepository outboxRepo, IMapper mapper,
    ICurrencyValidator currencyValidator) : IRequestHandler<TransferBetweenAccountsCommand, MbResult<IReadOnlyList<TransactionIdDto>>>
{
    public async Task<MbResult<IReadOnlyList<TransactionIdDto>>> Handle(TransferBetweenAccountsCommand request,
        CancellationToken cancellationToken)
    {
        if (!await currencyValidator.IsExists(request.Currency))
            throw new ArgumentException("Unsupported currency");

        Transaction debitTransaction;
        Transaction creditTransaction;

        await using var dbTransaction = await accRepo.BeginTransactionAsync(IsolationLevel.Serializable);
        try
        {
            var fromAccount = await accRepo.GetByIdForUpdateAsync(request.FromAccountId, dbTransaction);
            var toAccount = await accRepo.GetByIdForUpdateAsync(request.ToAccountId, dbTransaction);

            if (fromAccount is null)
                throw new KeyNotFoundException("Sender account not found");

            if (toAccount is null)
                throw new KeyNotFoundException("Recipient account not found");

            if (!toAccount.Currency.Equals(fromAccount.Currency, StringComparison.OrdinalIgnoreCase))
                throw new ArgumentException("Accounts with different currencies");

            var totalBefore = fromAccount.Balance + toAccount.Balance;

            debitTransaction = new Transaction
            {
                AccountId = request.FromAccountId,
                CounterpartyAccountId = request.ToAccountId,
                Amount = request.Amount,
                Currency = request.Currency.ToUpperInvariant(),
                Type = TransactionType.Debit,
                Description = request.Description
            };

            creditTransaction = new Transaction
            {
                AccountId = request.ToAccountId,
                CounterpartyAccountId = request.FromAccountId,
                Amount = request.Amount,
                Currency = request.Currency.ToUpperInvariant(),
                Type = TransactionType.Credit,
                Description = request.Description
            };

            fromAccount.ConductTransaction(debitTransaction);
            toAccount.ConductTransaction(creditTransaction);

            await accRepo.AddTransactionAsync(debitTransaction, dbTransaction);
            await accRepo.AddTransactionAsync(creditTransaction, dbTransaction);

            var updatedFrom = await accRepo.UpdateBalanceAsync(fromAccount, dbTransaction);
            var updatedTo = await accRepo.UpdateBalanceAsync(toAccount, dbTransaction);

            if (updatedFrom == 0 || updatedTo == 0)
            {
                throw new DBConcurrencyException("Account was modified by another transaction");
            }

            var totalAfter = fromAccount.Balance + toAccount.Balance;
            if (totalBefore != totalAfter)
            {
                throw new InvalidOperationException("Final balances mismatch — rolling back");
            }

            var correlationId = Guid.NewGuid();

            var transferEvent = new TransferCompleted(
                Guid.NewGuid(), DateTime.UtcNow,
                fromAccount.Id, toAccount.Id,
                request.Amount, request.Currency,
                debitTransaction.Id, creditTransaction.Id);
            transferEvent.Meta = new EventMeta(
                "account-service",
                correlationId, transferEvent.EventId
            );

            var debitEvent = mapper.Map<MoneyDebited>(debitTransaction);
            debitEvent.Meta = new EventMeta(
                "account-service",
                correlationId, transferEvent.EventId
            );

            var creditEvent = mapper.Map<MoneyCredited>(creditTransaction);
            creditEvent.Meta = new EventMeta(
                "account-service",
                correlationId, transferEvent.EventId
            );

            await outboxRepo.AddAsync(transferEvent, "account.events", "money.transfer.completed", dbTransaction);
            await outboxRepo.AddAsync(debitEvent, "account.events", "money.debited", dbTransaction);
            await outboxRepo.AddAsync(creditEvent, "account.events", "money.credited", dbTransaction);

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