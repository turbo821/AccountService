using AccountService.Application.Models;
using AccountService.Features.Accounts.Abstractions;
using AutoMapper;
using MediatR;

namespace AccountService.Features.Accounts.GetAccountList;

public class GetAccountListHandler(
    IMapper mapper, IAccountRepository repo)
    : IRequestHandler<GetAccountListQuery, MbResult<IReadOnlyList<AccountDto>>>
{
    public async Task<MbResult<IReadOnlyList<AccountDto>>> Handle(GetAccountListQuery request, CancellationToken cancellationToken)
    {
        var accounts = await repo.GetAllAsync(request.OwnerId);

        var accountsDto = mapper.Map<IReadOnlyList<AccountDto>>(accounts);
        return new MbResult<IReadOnlyList<AccountDto>>(accountsDto);
    }
}