using AccountService.Application.Contracts;

namespace AccountService.Features.Accounts.Contracts;

public record ClientBlocked(
    Guid EventId,
    DateTime OccurredAt,
    Guid ClientId
) :  DomainEvent(EventId, OccurredAt);