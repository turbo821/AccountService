using AccountService.Application.Contracts;

namespace AccountService.Features.Accounts.Contracts;

/// <summary>
/// Событие списания денег со счёта.
/// </summary>
/// <param name="EventId">Уникальный идентификатор события.</param>
/// <param name="OccurredAt">Дата и время возникновения события.</param>
/// <param name="AccountId">Идентификатор счёта, с которого списаны средства.</param>
/// <param name="Amount">Сумма списания.</param>
/// <param name="Currency">Валюта операции.</param>
/// <param name="OperationId">Идентификатор операции (транзакции).</param>
/// <param name="Reason">Причина или описание операции.</param>
public record MoneyDebited(
    Guid EventId,
    DateTime OccurredAt,
    Guid AccountId,
    decimal Amount,
    string Currency,
    Guid OperationId,
    string Reason
) : DomainEvent(EventId, OccurredAt);