namespace AccountService.Features.Accounts.CheckOwnerAccounts;

/// <summary>
/// DTO для проверки наличия счетов у владельца.
/// </summary>
public class CheckOwnerAccountsDto
{
    /// <summary>
    /// ID Владельца счёта.
    /// </summary>
    public Guid OwnerId { get; set; }
    /// <summary>
    /// Проверяет, есть ли у владельца счета хотя бы один счет.
    /// </summary>
    public bool AccountExists { get; set; }
    /// <summary>
    /// Количество счетов у владельца.
    /// </summary>
    public int AccountCount { get; set; }
}