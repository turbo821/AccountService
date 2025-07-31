using JetBrains.Annotations;

namespace AccountService.Features.Accounts.GetAccountTransactions;

/// <summary>
/// DTO информации о транзакции.
/// </summary>
[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
public class TransactionDto
{
    public Guid TransactionId { get; set; }
    public Guid? CounterpartyAccountId { get; set; }
    public decimal Amount { get; set; }
    public required string Currency { get; set; }
    public TransactionType Type { get; set; }
    public required string Description { get; set; }
    public DateTime Timestamp { get; set; }
}