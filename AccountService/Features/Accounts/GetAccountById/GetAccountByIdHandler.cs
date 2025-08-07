using AccountService.Application.Models;
using AccountService.Infrastructure.Persistence;
using AutoMapper;
using MediatR;

namespace AccountService.Features.Accounts.GetAccountById;

public class GetAccountByIdHandler(
    StubDbContext db, IMapper mapper) 
    : IRequestHandler<GetAccountByIdQuery, MbResult<AccountDto>>
{
    public Task<MbResult<AccountDto>> Handle(GetAccountByIdQuery request, CancellationToken cancellationToken)
    {
        var account = db.Accounts.Find(a => a.Id == request.AccountId && a.ClosedAt is null);

        if (account == null)
            throw new KeyNotFoundException($"Account with id {request.AccountId} not found");

        var accountDto = mapper.Map<AccountDto>(account);

        return Task.FromResult(new MbResult<AccountDto>(accountDto));
    }
}