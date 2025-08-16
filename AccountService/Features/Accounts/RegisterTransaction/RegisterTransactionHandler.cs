using AccountService.Application.Abstractions;
using AccountService.Application.Models;
using AccountService.Features.Accounts.Abstractions;
using AccountService.Features.Accounts.Contracts;
using AutoMapper;
using MediatR;
using Microsoft.Extensions.Logging;
using System.Data;

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

            if (request.Type == TransactionType.Credit)
            {
                var @event = mapper.Map<MoneyCredited>(transaction);
                await outboxRepo.AddAsync(@event, "account.events", "money.credited");
            }
            else
            {
                var @event = mapper.Map<MoneyDebited>(transaction);
                await outboxRepo.AddAsync(@event, "account.events", "money.debited");

            }

            var updated = await accRepo.UpdateBalanceAsync(account, dbTransaction);
            if (updated == 0)
                throw new DBConcurrencyException("Account was modified by another transaction");

            await accRepo.AddTransactionAsync(transaction, dbTransaction);
            
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