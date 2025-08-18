using AccountService.Application.Abstractions;
using AccountService.Application.Models;
using AccountService.Features.Accounts.Abstractions;
using AccountService.Features.Accounts.Contracts;
using AutoMapper;
using MediatR;
using System.Data;
using AccountService.Application.Contracts;

namespace AccountService.Features.Accounts.RegisterTransaction;

public class RegisterTransactionHandler(IAccountRepository accRepo, 
    IOutboxRepository outboxRepo, IMapper mapper,
    ICurrencyValidator currencyValidator) : IRequestHandler<RegisterTransactionCommand, MbResult<TransactionIdDto>>
{
    public async Task<MbResult<TransactionIdDto>> Handle(RegisterTransactionCommand request, CancellationToken cancellationToken)
    {
        if (!await currencyValidator.IsExists(request.Currency))
            throw new ArgumentException("Unsupported currency");

        Transaction transaction;

        await using var dbTransaction = await accRepo.BeginTransactionAsync(IsolationLevel.Serializable);
        try
        {
            var account = await accRepo.GetByIdForUpdateAsync(request.AccountId, dbTransaction);

            if (account == null)
                throw new KeyNotFoundException($"Account with id {request.AccountId} not found");

            transaction = mapper.Map<Transaction>(request);

            account.ConductTransaction(transaction);

            var updated = await accRepo.UpdateBalanceAsync(account, dbTransaction);
            if (updated == 0)
                throw new DBConcurrencyException("Account was modified by another transaction or frozen");

            await accRepo.AddTransactionAsync(transaction, dbTransaction);

            if (request.Type == TransactionType.Credit)
            {
                var @event = mapper.Map<MoneyCredited>(transaction);
                @event.Meta = new EventMeta(
                    "account-service",
                    Guid.NewGuid(), @event.EventId
                );
                await outboxRepo.AddAsync(@event, "account.events", "money.credited", dbTransaction);
            }
            else
            {
                var @event = mapper.Map<MoneyDebited>(transaction);
                @event.Meta = new EventMeta(
                    "account-service",
                    Guid.NewGuid(), @event.EventId
                );

                await outboxRepo.AddAsync(@event, "account.events", "money.debited", dbTransaction);
            }

            await dbTransaction.CommitAsync(cancellationToken);
        }
        catch
        {
            await dbTransaction.RollbackAsync(cancellationToken);
            throw;
        }

        var transactionIdDto = mapper.Map<TransactionIdDto>(transaction);
        return new MbResult<TransactionIdDto>(transactionIdDto);
    }
}