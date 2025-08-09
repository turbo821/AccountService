using AccountService.Application.Models;
using AccountService.Features.Accounts.Abstractions;
using AutoMapper;
using MediatR;

namespace AccountService.Features.Accounts.RegisterTransaction;

public class RegisterTransactionHandler(IAccountRepository repo, IMapper mapper,
    ICurrencyValidator currencyValidator) : IRequestHandler<RegisterTransactionCommand, MbResult<TransactionIdDto>>
{
    public async Task<MbResult<TransactionIdDto>> Handle(RegisterTransactionCommand request, CancellationToken cancellationToken)
    {
        var account = await repo.GetByIdAsync(request.AccountId);

        if (account == null)
            throw new KeyNotFoundException($"Account with id {request.AccountId} not found");

        if (!await currencyValidator.IsExists(request.Currency))
            throw new ArgumentException("Unsupported currency");

        if (!account.Currency.Equals(request.Currency.ToUpperInvariant()))
            throw new ArgumentException("Transaction currency is different from account currency");

        var transaction = mapper.Map<Transaction>(request);

        account.ConductTransaction(transaction);

        using var dbTransaction = repo.BeginTransaction();

        try
        {
            await repo.UpdateBalanceAsync(account, dbTransaction);
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