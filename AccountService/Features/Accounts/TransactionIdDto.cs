namespace AccountService.Features.Accounts;

/// <summary>
/// Данные для идентификации транзакции.
/// </summary>
public class TransactionIdDto
{
    /// <summary>
    /// ID транзакции.
    /// </summary>
    public Guid TransactionId { get; set; }
}