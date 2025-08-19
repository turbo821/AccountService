using AccountService.Application.Contracts;

namespace AccountService.Features.Accounts.Contracts;

/// <summary>
/// Событие, которое возникает при обновлении процентной ставки по счёту.
/// </summary>
/// <param name="EventId">Уникальный идентификатор события.</param>
/// <param name="OccurredAt">Дата и время возникновения события.</param>
/// <param name="AccountId">Идентификатор счёта.</param>
/// <param name="OwnerId">Идентификатор владельца счёта.</param>
/// <param name="InterestRate">Новая процентная ставка по счёту.</param>
public record AccountInterestUpdated(
    Guid EventId,
    DateTime OccurredAt,
    Guid AccountId,
    Guid OwnerId,
    decimal InterestRate
) : DomainEvent(EventId, OccurredAt);