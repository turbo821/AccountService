using MediatR;

namespace AccountService.Features.Accounts.GetAccounts;

public class GetAccountsQuery : IRequest<object?>
{
    public GetAccountsQuery(Guid? ownerId)
    {
        throw new NotImplementedException();
    }
}