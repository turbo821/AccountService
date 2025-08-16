using AccountService.Application.Models;

namespace AccountService.Features.Accounts.Contracts;

public record AccountUpdated(
    Guid EventId,
    DateTime OccurredAt,
    Guid AccountId,
    Guid OwnerId,
    string Currency,
    string Type,
    decimal Balance,
    decimal? InterestRate,
    DateTime OpenedAt) : DomainEvent(EventId, OccurredAt);