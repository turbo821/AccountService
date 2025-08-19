using AccountService.Application.Contracts;

namespace AccountService.Features.Accounts.Contracts;

/// <summary>
/// Событие, которое возникает при закрытии счёта.
/// </summary>
/// <param name="EventId">Уникальный идентификатор события.</param>
/// <param name="OccurredAt">Дата и время возникновения события.</param>
/// <param name="AccountId">Идентификатор закрытого счёта.</param>
public record AccountClosed(
    Guid EventId,
    DateTime OccurredAt,
    Guid AccountId
) : DomainEvent(EventId, OccurredAt);
