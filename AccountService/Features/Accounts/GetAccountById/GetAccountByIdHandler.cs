using AccountService.Application.Models;
using AccountService.Features.Accounts.Abstractions;
using AutoMapper;
using MediatR;

namespace AccountService.Features.Accounts.GetAccountById;

public class GetAccountByIdHandler(
    IAccountRepository repo, IMapper mapper) 
    : IRequestHandler<GetAccountByIdQuery, MbResult<AccountDto>>
{
    public async Task<MbResult<AccountDto>> Handle(GetAccountByIdQuery request, CancellationToken cancellationToken)
    {
        var account = await repo.GetByIdAsync(request.AccountId);

        if (account == null)
            throw new KeyNotFoundException($"Account with id {request.AccountId} not found");

        var accountDto = mapper.Map<AccountDto>(account);

        return new MbResult<AccountDto>(accountDto);
    }
}