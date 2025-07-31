using AccountService.Infrastructure.Persistence;
using AutoMapper;
using MediatR;

namespace AccountService.Features.Accounts.GetAccountList;

public class GetAccountListHandler(
    IMapper mapper, StubDbContext db)
    : IRequestHandler<GetAccountListQuery, IEnumerable<AccountDto>>
{
    public Task<IEnumerable<AccountDto>> Handle(GetAccountListQuery request, CancellationToken cancellationToken)
    {
        var accounts = db.Accounts
            .Where(a => a.ClosedAt is null)
            .Where(a => request.OwnerId is null || a.OwnerId == request.OwnerId.Value);

        var accountsDto = mapper.Map<IEnumerable<AccountDto>>(accounts);
        return Task.FromResult(accountsDto);
    }
}