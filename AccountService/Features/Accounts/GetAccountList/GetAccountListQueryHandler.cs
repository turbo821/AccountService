using AccountService.Features.Accounts.Abstractions;
using AutoMapper;
using MediatR;

namespace AccountService.Features.Accounts.GetAccountList;

public class GetAccountListQueryHandler(
    IMapper mapper,
    IAccountRepository repository) : IRequestHandler<GetAccountListQuery, IEnumerable<AccountDto>>
{
    public async Task<IEnumerable<AccountDto>> Handle(GetAccountListQuery request, CancellationToken cancellationToken)
    {
        var accounts = repository.GetAll();

        var response = mapper.Map<IEnumerable<AccountDto>>(accounts);
        return response;
    }
}