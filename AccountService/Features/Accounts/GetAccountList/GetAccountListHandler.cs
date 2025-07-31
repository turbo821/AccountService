using AccountService.Infrastructure.Persistence;
using AutoMapper;
using MediatR;

namespace AccountService.Features.Accounts.GetAccountList;

public class GetAccountListHandler(
    IMapper mapper, StubDbContext db)
    : IRequestHandler<GetAccountListQuery, IReadOnlyList<AccountDto>>
{
    public Task<IReadOnlyList<AccountDto>> Handle(GetAccountListQuery request, CancellationToken cancellationToken)
    {
        var accounts = db.Accounts
            .Where(a => a.ClosedAt is null)
            .Where(a => request.OwnerId is null || a.OwnerId == request.OwnerId.Value).ToList();

        var accountsDto = mapper.Map<IReadOnlyList<AccountDto>>(accounts);
        return Task.FromResult(accountsDto);
    }
}