using AccountService.Application.Contracts;

namespace AccountService.Features.Accounts.Contracts;

/// <summary>
/// Событие завершения перевода между двумя счетами.
/// </summary>
/// <param name="EventId">Уникальный идентификатор события.</param>
/// <param name="OccurredAt">Дата и время возникновения события.</param>
/// <param name="SourceAccountId">Идентификатор счёта-отправителя.</param>
/// <param name="DestinationAccountId">Идентификатор счёта-получателя.</param>
/// <param name="Amount">Сумма перевода.</param>
/// <param name="Currency">Валюта перевода.</param>
/// <param name="DebitTransactionId">Идентификатор транзакции списания.</param>
/// <param name="CreditTransactionId">Идентификатор транзакции зачисления.</param>
public record TransferCompleted(
    Guid EventId,
    DateTime OccurredAt,
    Guid SourceAccountId,
    Guid DestinationAccountId,
    decimal Amount,
    string Currency,
    Guid DebitTransactionId,
    Guid CreditTransactionId
) : DomainEvent(EventId, OccurredAt);