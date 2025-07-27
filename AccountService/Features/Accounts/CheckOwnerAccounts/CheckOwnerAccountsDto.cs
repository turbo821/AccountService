namespace AccountService.Features.Accounts.CheckOwnerAccounts;

/// <summary>
/// DTO для проверки наличия счетов у владельца.
/// </summary>
public class CheckOwnerAccountsDto
{
    public Guid OwnerId { get; set; }
    public bool AccountExists { get; set; }
    public IEnumerable<Guid> AccountIds { get; set; } = [];
}