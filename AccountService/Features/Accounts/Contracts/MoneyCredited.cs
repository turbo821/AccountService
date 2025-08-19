using AccountService.Application.Contracts;

namespace AccountService.Features.Accounts.Contracts;

/// <summary>
/// Событие зачисления денег на счёт.
/// </summary>
/// <param name="EventId">Уникальный идентификатор события.</param>
/// <param name="OccurredAt">Дата и время возникновения события.</param>
/// <param name="AccountId">Идентификатор счёта, на который зачислены средства.</param>
/// <param name="Amount">Сумма зачисления.</param>
/// <param name="Currency">Валюта операции.</param>
/// <param name="OperationId">Идентификатор операции (транзакции).</param>
/// <param name="Reason">Причина или описание операции.</param>
public record MoneyCredited(
    Guid EventId,
    DateTime OccurredAt,
    Guid AccountId,
    decimal Amount,
    string Currency,
    Guid OperationId,
    string Reason
) : DomainEvent(EventId, OccurredAt);