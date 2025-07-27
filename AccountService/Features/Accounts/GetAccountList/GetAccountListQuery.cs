using MediatR;

namespace AccountService.Features.Accounts.GetAccountList;

/// <summary>
/// Запрос на получение списка всех счетов.
/// </summary>
public record GetAccountListQuery : IRequest<IEnumerable<AccountDto>>;
