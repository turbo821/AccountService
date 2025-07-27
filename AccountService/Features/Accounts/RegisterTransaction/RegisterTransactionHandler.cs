using AccountService.Features.Accounts.Abstractions;
using AccountService.Infrastructure.Persistence;
using AutoMapper;
using MediatR;

namespace AccountService.Features.Accounts.RegisterTransaction;

public class RegisterTransactionHandler(StubDbContext db, IMapper mapper,
    ICurrencyValidator currencyValidator) : IRequestHandler<RegisterTransactionCommand, Guid>
{
    public Task<Guid> Handle(RegisterTransactionCommand request, CancellationToken cancellationToken)
    {
        var account = db.Accounts.Find(a => a.Id == request.AccountId && a.ClosedAt is null);

        if (account == null)
            throw new KeyNotFoundException($"Account with id {request.AccountId} not found");

        if (!currencyValidator.IsExists(request.Currency))
            throw new ArgumentException("Unsupported currency");

        if (!account.Currency.Equals(request.Currency.ToUpperInvariant()))
            throw new ArgumentException("Transaction currency is different from account currency");

        var transaction = mapper.Map<Transaction>(request);

        account.ConductTransaction(transaction);
        db.Transactions.Add(transaction);

        return Task.FromResult(transaction.Id);
    }
}