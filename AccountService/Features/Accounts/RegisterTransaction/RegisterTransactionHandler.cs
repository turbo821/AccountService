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
        var account = db.Accounts.Find(a => a.Id == request.AccountId && a.ClosedAt is null);

        if (account == null)
            throw new KeyNotFoundException($"Account with id {request.AccountId} not found");

        if (!currencyValidator.IsValid(request.Currency))
            throw new ArgumentException("Unsupported currency");

        if (!account.Currency.Equals(request.Currency.ToUpperInvariant()))
            throw new ArgumentException("Transaction currency is different from account currency");

        if (account.Balance < request.Amount && request.Type == TransactionType.Credit)
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

        account.Transactions.Add(transaction);
        db.Transactions.Add(transaction);

        return Task.FromResult(transaction.Id);
    }
}