using AccountService.Application.Models;
using AccountService.Features.Accounts.Abstractions;
using AccountService.Infrastructure.Persistence;
using AutoMapper;
using MediatR;

namespace AccountService.Features.Accounts.RegisterTransaction;

public class RegisterTransactionHandler(AppDbContext db, IMapper mapper,
    ICurrencyValidator currencyValidator) : IRequestHandler<RegisterTransactionCommand, MbResult<TransactionIdDto>>
{
    public Task<MbResult<TransactionIdDto>> Handle(RegisterTransactionCommand request, CancellationToken cancellationToken)
    {
        var account = db.Accounts2.Find(a => a.Id == request.AccountId && a.ClosedAt is null);

        if (account == null)
            throw new KeyNotFoundException($"Account with id {request.AccountId} not found");

        if (!currencyValidator.IsExists(request.Currency))
            throw new ArgumentException("Unsupported currency");

        if (!account.Currency.Equals(request.Currency.ToUpperInvariant()))
            throw new ArgumentException("Transaction currency is different from account currency");

        var transaction = mapper.Map<Transaction>(request);

        account.ConductTransaction(transaction);
        db.Transactions2.Add(transaction);

        var transactionIdDto = mapper.Map<TransactionIdDto>(transaction);
        return Task.FromResult(new MbResult<TransactionIdDto>(transactionIdDto));
    }
}