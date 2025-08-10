using Azure.Core;
using JetBrains.Annotations;

namespace AccountService.Features.Accounts;

[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
public class Account
{
    public Guid Id { get; set; } = Guid.NewGuid();  
    public Guid OwnerId { get; set; }
    public AccountType Type { get; set; }
    public required string Currency { get; set; }
    public decimal Balance { get; set; }
    public decimal? InterestRate { get; set; }
    public DateTime OpenedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ClosedAt { get; set; }
    public long Version { get; set; }

    public List<Transaction> Transactions { get; set; } = [];

    public void ConductTransaction(Transaction transaction)
    {
        if (!Currency.Equals(transaction.Currency, StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException("Transaction currency is different from account currency");

        switch (transaction.Type)
        {
            case TransactionType.Credit when Balance < transaction.Amount:
                throw new InvalidOperationException("Insufficient funds for this transaction");
            case TransactionType.Credit:
                Balance -= transaction.Amount;
                break;
            case TransactionType.Debit:
                Balance += transaction.Amount;
                break;
            default:
                throw new ArgumentException("Invalid transaction type");
        }
        
        transaction.AccountId = Id;
        Transactions.Add(transaction);
    }
}