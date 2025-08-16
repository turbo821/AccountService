using AccountService.Application.Models;

namespace AccountService.Features.Accounts.Contracts;

public record InterestAccrued(
    Guid EventId,
    DateTime OccurredAt,
    Guid AccountId,
    DateTime PeriodFrom,
    DateTime PeriodTo,
    decimal Amount
) : DomainEvent(EventId, OccurredAt);