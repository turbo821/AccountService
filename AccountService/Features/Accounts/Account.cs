namespace AccountService.Features.Accounts;

public class Account
{
    public Guid Id { get; set; } = Guid.NewGuid();  
    public Guid OwnerId { get; set; }
    public AccountType Type { get; set; }
    public required string Currency { get; set; }
    public decimal Balance { get; set; } = 0m;
    public decimal? InterestRate { get; set; }
    public DateTime OpenedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ClosedAt { get; set; }

    public List<Transaction> Transactions { get; set; } = [];
}