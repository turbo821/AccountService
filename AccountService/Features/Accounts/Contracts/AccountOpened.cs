using AccountService.Application.Contracts;

namespace AccountService.Features.Accounts.Contracts;

/// <summary>
/// Событие, которое возникает при открытии нового счёта.
/// </summary>
/// <param name="EventId">Уникальный идентификатор события.</param>
/// <param name="OccurredAt">Дата и время возникновения события.</param>
/// <param name="AccountId">Идентификатор открытого счёта.</param>
/// <param name="OwnerId">Идентификатор владельца счёта.</param>
/// <param name="Currency">Валюта счёта.</param>
/// <param name="Type">Тип счёта (Checking, Credit, Deposit).</param>
public record AccountOpened(
    Guid EventId,
    DateTime OccurredAt,
    Guid AccountId,
    Guid OwnerId,
    string Currency,
    string Type
) : DomainEvent(EventId, OccurredAt);