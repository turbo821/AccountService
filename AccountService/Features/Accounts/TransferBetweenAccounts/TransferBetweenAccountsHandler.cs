using AccountService.Features.Accounts.Abstractions;
using AccountService.Infrastructure.Persistence;
using MediatR;

namespace AccountService.Features.Accounts.TransferBetweenAccounts;

public class TransferBetweenAccountsHandler(StubDbContext db,
    ICurrencyValidator currencyValidator) : IRequestHandler<TransferBetweenAccountsCommand>
{
    public Task Handle(TransferBetweenAccountsCommand request, CancellationToken cancellationToken)
    {
        var fromAccount =  db.Accounts.Find(a => a.Id == request.FromAccountId && a.ClosedAt is null);
        var toAccount =  db.Accounts.Find(a => a.Id == request.ToAccountId && a.ClosedAt is null);

        if (fromAccount is null)
            throw new KeyNotFoundException("Sender account not found");

        if (toAccount is null)
            throw new KeyNotFoundException("Recipient account not found");

        if (!currencyValidator.IsExists(request.Currency))
            throw new ArgumentException("Unsupported currency");

        if (!toAccount.Currency.Equals(fromAccount.Currency))
            throw new ArgumentException("Accounts with different currencies");

        if(!toAccount.Currency.Equals(request.Currency.ToUpperInvariant()))
            throw new ArgumentException("Transaction currency is different from recipient account currency");

        if (!fromAccount.Currency.Equals(request.Currency.ToUpperInvariant()))
            throw new ArgumentException("Transaction currency is different from sender account currency");

        if (fromAccount.Balance < request.Amount)
            throw new InvalidOperationException("Insufficient funds in the sender account");

        var creditTransaction = new Transaction
        {
            AccountId = request.ToAccountId,
            CounterpartyAccountId = request.FromAccountId,
            Amount = request.Amount,
            Currency = request.Currency,
            Type = TransactionType.Credit,
            Description = request.Description
        };

        var debitTransaction = new Transaction
        {
            AccountId = request.FromAccountId,
            CounterpartyAccountId = request.ToAccountId,
            Amount = request.Amount,
            Currency = request.Currency,
            Type = TransactionType.Debit,
            Description = request.Description
        };

        fromAccount.ConductTransaction(creditTransaction);
        toAccount.ConductTransaction(debitTransaction);

        db.Transactions.AddRange(debitTransaction, creditTransaction);

        return Task.CompletedTask;
    }
}