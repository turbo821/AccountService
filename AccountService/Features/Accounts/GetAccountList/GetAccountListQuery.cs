using MediatR;

namespace AccountService.Features.Accounts.GetAccountList;

public record GetAccountListQuery : IRequest<IEnumerable<AccountDto>>;
