namespace AccountService.Features.Accounts;

// <summary>
// Типы транзакций для банковских счетов.
// </summary>
public enum TransactionType
{
    /// <summary>
    /// Поступление средств на счёт.
    /// </summary>
    Credit = 0,

    /// <summary>
    /// Списание средств со счёта.
    /// </summary>
    Debit = 1
}