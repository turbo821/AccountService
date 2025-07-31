using JetBrains.Annotations;

namespace AccountService.Features.Accounts.GetAccountStatement;

/// <summary>
/// DTO для отображения информации о счёте и связанных с ним транзакциях.
/// </summary>
[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
public class AccountStatementDto
{
    public Guid AccountId { get; set; }
    public Guid OwnerId { get; set; }
    public AccountType Type { get; set; }
    public decimal Balance { get; set; }
    public List<TransactionDto> Transactions { get; set; } = [];
}