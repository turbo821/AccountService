using AccountService.Application.Contracts;

namespace AccountService.Features.Accounts.Contracts;

/// <summary>
/// Событие, которое возникает при обновлении информации о счёте.
/// </summary>
/// <param name="EventId">Уникальный идентификатор события.</param>
/// <param name="OccurredAt">Дата и время возникновения события.</param>
/// <param name="AccountId">Идентификатор счёта.</param>
/// <param name="OwnerId">Идентификатор владельца счёта.</param>
/// <param name="Currency">Валюта счёта.</param>
/// <param name="Type">Тип счёта.</param>
/// <param name="Balance">Текущий баланс счёта.</param>
/// <param name="InterestRate">Процентная ставка по счёту (если применимо).</param>
/// <param name="OpenedAt">Дата открытия счёта.</param>
public record AccountUpdated(
    Guid EventId,
    DateTime OccurredAt,
    Guid AccountId,
    Guid OwnerId,
    string Currency,
    string Type,
    decimal Balance,
    decimal? InterestRate,
    DateTime OpenedAt) : DomainEvent(EventId, OccurredAt);