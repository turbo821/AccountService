namespace AccountService.Features.Accounts;

/// <summary>
/// Типы банковских счетов.
/// </summary>
public enum AccountType
{
    /// <summary>
    /// Расчетный счёт (обычный счёт для операций).
    /// </summary>
    Checking,

    /// <summary>
    /// Сберегательный счёт (для накоплений с процентами).
    /// </summary>
    Deposit,

    /// <summary>
    /// Кредитный счёт (для получения кредитов).
    /// </summary>
    Credit
}