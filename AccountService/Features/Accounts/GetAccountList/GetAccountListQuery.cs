using AccountService.Application.Models;
using MediatR;

namespace AccountService.Features.Accounts.GetAccountList;

/// <summary>
/// Запрос на получение списка всех счетов.
/// </summary>
/// <param name="OwnerId">ID владельца счетов (опционально).</param>
public record GetAccountListQuery(Guid? OwnerId) : IRequest<MbResult<IReadOnlyList<AccountDto>>>;
