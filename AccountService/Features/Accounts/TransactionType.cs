namespace AccountService.Features.Accounts;

// <summary>
// Типы транзакций для банковских счетов.
// </summary>
public enum TransactionType
{
    /// <summary>
    /// Поступление средств на счёт.
    /// </summary>
    Debit = 0,

    /// <summary>
    /// Списание средств со счёта.
    /// </summary>
    Credit = 1
}