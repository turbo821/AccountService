using AccountService.Application.Contracts;

namespace AccountService.Features.Accounts.Contracts;

/// <summary>
/// Событие, которое возникает при блокировке клиента.
/// </summary>
/// <param name="EventId">Уникальный идентификатор события.</param>
/// <param name="OccurredAt">Дата и время возникновения события.</param>
/// <param name="ClientId">Идентификатор заблокированного клиента.</param>
public record ClientBlocked(
    Guid EventId,
    DateTime OccurredAt,
    Guid ClientId
) :  DomainEvent(EventId, OccurredAt);