using AccountService.Application.Contracts;

namespace AccountService.Features.Accounts.Contracts;

/// <summary>
/// Событие, которое возникает при снятии блокировки клиента.
/// </summary>
/// <param name="EventId">Уникальный идентификатор события.</param>
/// <param name="OccurredAt">Дата и время возникновения события.</param>
/// <param name="ClientId">Идентификатор разблокированного клиента.</param>
public record ClientUnblocked(
    Guid EventId,
    DateTime OccurredAt,
    Guid ClientId
) : DomainEvent(EventId, OccurredAt);