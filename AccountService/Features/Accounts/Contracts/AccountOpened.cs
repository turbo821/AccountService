using AccountService.Application.Contracts;

namespace AccountService.Features.Accounts.Contracts;

public record AccountOpened(
    Guid EventId,
    DateTime OccurredAt,
    Guid AccountId,
    Guid OwnerId,
    string Currency,
    string Type
) : DomainEvent(EventId, OccurredAt);