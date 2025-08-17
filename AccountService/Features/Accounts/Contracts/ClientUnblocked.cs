using AccountService.Application.Contracts;

namespace AccountService.Features.Accounts.Contracts;

public record ClientUnblocked(
    Guid EventId,
    DateTime OccurredAt,
    Guid ClientId
) : DomainEvent(EventId, OccurredAt);