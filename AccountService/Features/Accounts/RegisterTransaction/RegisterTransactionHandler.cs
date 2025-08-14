using AccountService.Application.Models;
using AccountService.Features.Accounts.Abstractions;
using AutoMapper;
using MediatR;
using System.Data;

namespace AccountService.Features.Accounts.RegisterTransaction;

public class RegisterTransactionHandler(IAccountRepository repo, IMapper mapper,
    ICurrencyValidator currencyValidator) : IRequestHandler<RegisterTransactionCommand, MbResult<TransactionIdDto>>
{
    public async Task<MbResult<TransactionIdDto>> Handle(RegisterTransactionCommand request, CancellationToken cancellationToken)
    {
        if (!await currencyValidator.IsExists(request.Currency))
            throw new ArgumentException("Unsupported currency");

        Transaction transaction;

        using var dbTransaction = await repo.BeginTransaction();
        try
        {
            var account = await repo.GetByIdForUpdateAsync(request.AccountId, dbTransaction);

            if (account == null)
                throw new KeyNotFoundException($"Account with id {request.AccountId} not found");

            transaction = mapper.Map<Transaction>(request);

            account.ConductTransaction(transaction);

            var updated = await repo.UpdateBalanceAsync(account, dbTransaction);
            if (updated == 0)
                throw new DBConcurrencyException("Account was modified by another transaction");

            await repo.AddTransactionAsync(transaction, dbTransaction);

            dbTransaction.Commit();
        }
        catch
        {
            dbTransaction.Rollback();
            throw;
        }

        var transactionIdDto = mapper.Map<TransactionIdDto>(transaction);
        return new MbResult<TransactionIdDto>(transactionIdDto);
    }
}