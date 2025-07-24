namespace AccountService.Features.Accounts;

public class Transaction
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid AccountId { get; set; }
    public Guid? CounterpartyAccountId { get; set; }
    public decimal Amount { get; set; }
    public required string Currency { get; set; }
    public TransactionType Type { get; set; }
    public required string Description { get; set; }
    public DateTime Timestamp { get; set; }
}