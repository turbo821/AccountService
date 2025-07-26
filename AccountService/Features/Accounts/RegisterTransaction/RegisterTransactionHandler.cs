using AccountService.Features.Accounts.Abstractions;
using AccountService.Infrastructure.Persistence;
using AutoMapper;
using MediatR;
using System;

namespace AccountService.Features.Accounts.RegisterTransaction;

public class RegisterTransactionHandler(StubDbContext db, IMapper mapper,
    ICurrencyValidator currencyValidator) : IRequestHandler<RegisterTransactionCommand, Guid>
{
    public Task<Guid> Handle(RegisterTransactionCommand request, CancellationToken cancellationToken)
    {
        var account = db.Accounts.Find(a => a.Id == request.AccountId);

        if (account == null)
            throw new KeyNotFoundException($"Account with id {request.AccountId} not found");

        if (account.ClosedAt != null)
            throw new InvalidOperationException("Cannot register transaction on closed account.");

        if (!currencyValidator.IsValid(request.Currency))
            throw new ArgumentException("Unsupported currency");

        if (account.Balance < request.Amount)
            throw new InvalidOperationException("Insufficient funds for this transaction");

        switch (request.Type)
        {
            case TransactionType.Credit:
                account.Balance -= request.Amount;
                break;
            case TransactionType.Debit:
                account.Balance += request.Amount;
                break;
            default:
                throw new ArgumentException("Invalid transaction type");
        }

        var transaction = mapper.Map<Transaction>(request);
        db.Transactions.Add(transaction);

        return Task.FromResult(transaction.Id);
    }
}