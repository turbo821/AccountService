using AccountService.Features.Accounts.Abstractions;
using AccountService.Infrastructure.Persistence;
using AutoMapper;
using MediatR;

namespace AccountService.Features.Accounts.TransferBetweenAccounts;

public class TransferBetweenAccountsHandler(StubDbContext db, IMapper mapper,
    ICurrencyValidator currencyValidator) : IRequestHandler<TransferBetweenAccountsCommand>
{
    public Task Handle(TransferBetweenAccountsCommand request, CancellationToken cancellationToken)
    {
        var fromAccount =  db.Accounts.Find(a => a.Id == request.FromAccountId);
        var toAccount =  db.Accounts.Find(a => a.Id == request.ToAccountId);

        if (fromAccount is null)
            throw new KeyNotFoundException("Sender account not found");

        if (toAccount is null)
            throw new KeyNotFoundException("Recipient account not found");

        if (fromAccount.ClosedAt != null)
            throw new InvalidOperationException("The sender account has been closed");

        if (toAccount.ClosedAt != null)
            throw new InvalidOperationException("The recipient account has been closed");

        if (!currencyValidator.IsValid(request.Currency))
            throw new ArgumentException("Unsupported currency");

        if (!string.Equals(fromAccount.Currency, toAccount.Currency, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Валюты счетов не совпадают");

        if (fromAccount.Balance < request.Amount)
            throw new InvalidOperationException("Not enough money in the sender account");

        var transaction = mapper.Map<List<Transaction>>(request);

        fromAccount.Balance -= request.Amount;
        toAccount.Balance += request.Amount;
        db.Transactions.AddRange(transaction);


        return Task.CompletedTask;
    }
}