using AccountService.Application.Contracts;

namespace AccountService.Features.Accounts.Contracts;

/// <summary>
/// Событие начисления процентов на счёт за определённый период.
/// </summary>
/// <param name="EventId">Уникальный идентификатор события.</param>
/// <param name="OccurredAt">Дата и время возникновения события.</param>
/// <param name="AccountId">Идентификатор счёта, на который начислены проценты.</param>
/// <param name="PeriodFrom">Дата начала периода начисления процентов.</param>
/// <param name="PeriodTo">Дата окончания периода начисления процентов.</param>
/// <param name="Amount">Сумма начисленных процентов.</param>
public record InterestAccrued(
    Guid EventId,
    DateTime OccurredAt,
    Guid AccountId,
    DateTime PeriodFrom,
    DateTime PeriodTo,
    decimal Amount
) : DomainEvent(EventId, OccurredAt);