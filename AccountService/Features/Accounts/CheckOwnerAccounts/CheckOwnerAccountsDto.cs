namespace AccountService.Features.Accounts.CheckOwnerAccounts;

public class CheckOwnerAccountsDto
{
    public Guid OwnerId { get; set; }
    public bool Exists { get; set; }
    public IEnumerable<Guid> AccountIds { get; set; } = [];
}