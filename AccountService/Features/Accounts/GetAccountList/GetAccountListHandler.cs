using AccountService.Application.Models;
using AccountService.Infrastructure.Persistence;
using AutoMapper;
using MediatR;

namespace AccountService.Features.Accounts.GetAccountList;

public class GetAccountListHandler(
    IMapper mapper, AppDbContext db)
    : IRequestHandler<GetAccountListQuery, MbResult<IReadOnlyList<AccountDto>>>
{
    public Task<MbResult<IReadOnlyList<AccountDto>>> Handle(GetAccountListQuery request, CancellationToken cancellationToken)
    {
        var accounts = db.Accounts2
            .Where(a => a.ClosedAt is null)
            .Where(a => request.OwnerId is null || a.OwnerId == request.OwnerId.Value).ToList();

        var accountsDto = mapper.Map<IReadOnlyList<AccountDto>>(accounts);
        return Task.FromResult(new MbResult<IReadOnlyList<AccountDto>>(accountsDto));
    }
}