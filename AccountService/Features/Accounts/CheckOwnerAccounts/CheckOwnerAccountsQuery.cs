using MediatR;

namespace AccountService.Features.Accounts.CheckOwnerAccounts;

public record CheckOwnerAccountsQuery(Guid OwnerId) : IRequest<CheckOwnerAccountsDto>;