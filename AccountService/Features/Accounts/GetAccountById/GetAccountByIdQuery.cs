using MediatR;

namespace AccountService.Features.Accounts.GetAccountById;

/// <summary>
/// Запрос на получение информации о счёте по его идентификатору.
/// </summary>
/// <param name="AccountId">ID счёта.</param>
public record GetAccountByIdQuery(Guid AccountId) : IRequest<AccountDto>;