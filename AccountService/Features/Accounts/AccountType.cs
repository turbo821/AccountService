namespace AccountService.Features.Accounts;

/// <summary>
/// Типы банковских счетов.
/// </summary>
public enum AccountType
{
    /// <summary>
    /// Расчетный счёт (обычный счёт для операций).
    /// </summary>
    Checking = 0,

    /// <summary>
    /// Сберегательный счёт (для накоплений с процентами).
    /// </summary>
    Deposit = 1,

    /// <summary>
    /// Кредитный счёт (для получения кредитов).
    /// </summary>
    Credit = 2
}