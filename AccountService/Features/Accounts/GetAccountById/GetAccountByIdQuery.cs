using MediatR;

namespace AccountService.Features.Accounts.GetAccountById;

public record GetAccountByIdQuery(Guid AccountId) : IRequest<AccountDto>;