using AccountService.Application.Models;

namespace AccountService.Features.Accounts.Contracts;

public record AccountClosed(
    Guid EventId,
    DateTime OccurredAt,
    Guid AccountId
) : DomainEvent(EventId, OccurredAt);
