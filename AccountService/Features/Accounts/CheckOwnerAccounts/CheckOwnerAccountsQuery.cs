using AccountService.Application.Models;
using MediatR;

namespace AccountService.Features.Accounts.CheckOwnerAccounts;

/// <summary>
/// Запрос на проверку наличия счетов у владельца.
/// </summary>
/// <param name="OwnerId">Идентификатор владельца.</param>
public record CheckOwnerAccountsQuery(Guid OwnerId) : IRequest<MbResult<CheckOwnerAccountsDto>>;