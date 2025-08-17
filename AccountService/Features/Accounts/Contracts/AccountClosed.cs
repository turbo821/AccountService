using AccountService.Application.Contracts;

namespace AccountService.Features.Accounts.Contracts;

public record AccountClosed(
    Guid EventId,
    DateTime OccurredAt,
    Guid AccountId
) : DomainEvent(EventId, OccurredAt);
