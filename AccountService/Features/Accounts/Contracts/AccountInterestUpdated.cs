using AccountService.Application.Models;

namespace AccountService.Features.Accounts.Contracts;

public record AccountInterestUpdated(
    Guid EventId,
    DateTime OccurredAt,
    Guid AccountId,
    Guid OwnerId,
    decimal InterestRate
) : DomainEvent(EventId, OccurredAt);