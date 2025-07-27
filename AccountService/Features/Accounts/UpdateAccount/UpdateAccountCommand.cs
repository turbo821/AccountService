using MediatR;

namespace AccountService.Features.Accounts.UpdateAccount;

/// <summary>
/// Команда для обновления процентной ставки существующего счёта.
/// </summary>
/// <param name="AccountId">Идентификатор счёта, процентная ставка которого изменяется.</param>
/// <param name="InterestRate">Новая процентная ставка (в процентах).</param>
public record UpdateAccountCommand(Guid AccountId, decimal InterestRate) : IRequest;

/// <summary>
/// Запрос на обновление процентной ставки.
/// </summary>
/// <param name="InterestRate">Новая процентная ставка (в процентах).</param>
public record UpdateInterestRateRequest(decimal InterestRate);