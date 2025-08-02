namespace AccountService.Features.Accounts;

/// <summary>
/// Данные для идентификации счёта.
/// </summary>
public class AccountIdDto
{
    /// <summary>
    /// ID счёта.
    /// </summary>
    public Guid AccountId { get; set; }
}