using JetBrains.Annotations;
using MediatR;

namespace AccountService.Features.Accounts.UpdateAccount;

/// <summary>
/// Команда для обновления данных существующего счёта.
/// </summary>
/// <param name="accountId">Идентификатор счёта на изменение.</param>
/// <param name="ownerId">Новый владелец счета.</param>
/// <param name="type">Новый тип счета.</param>
/// <param name="currency">Новая валюта счета ISO 4217 (например, RUB, USD).</param>
/// <param name="balance">Новый баланс счета.</param>
/// <param name="interestRate">Новая процентная ставка (в процентах).</param>
/// <param name="openedAt">Новая дата открытия счета (опционально).</param>
[UsedImplicitly(ImplicitUseTargetFlags.Members)]
public class UpdateAccountCommand(
    Guid accountId,
    Guid ownerId,
    AccountType type,
    string currency,
    decimal balance,
    decimal? interestRate,
    DateTime? openedAt) : IRequest<Guid>
{
    public Guid AccountId { get; init; } = accountId;
    public Guid OwnerId { get; init; } = ownerId;
    public AccountType Type { get; init; } = type;
    public string Currency { get; init; } = currency;
    public decimal Balance { get; init; } = balance;
    public decimal? InterestRate { get; init; } = interestRate;
    public DateTime? OpenedAt { get; set; } = openedAt;
}

/// <summary>
/// Запрос на обновление процентной ставки.
/// </summary>
/// <param name="OwnerId">Новый владелец счета.</param>
/// <param name="Type">Новый тип счета.</param>
/// <param name="Currency">Новая валюта счета ISO 4217 (например, RUB, USD).</param>
/// <param name="Balance">Новый баланс счета.</param>
/// <param name="InterestRate">Новая процентная ставка (в процентах).</param>
/// <param name="OpenedAt">Новая дата открытия счета (опционально).</param>
public record UpdateAccountRequest(
    Guid OwnerId,
    AccountType Type,
    string Currency,
    decimal Balance,
    decimal? InterestRate,
    DateTime? OpenedAt);