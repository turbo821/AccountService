using MediatR;
using AccountService.Infrastructure.Persistence;
using AutoMapper;

namespace AccountService.Features.Accounts.GetAccountTransactions;

public class GetAccountTransactionsHandler(StubDbContext db, IMapper mapper)
    : IRequestHandler<GetAccountTransactionsQuery, AccountTransactionsDto>
{
    public Task<AccountTransactionsDto> Handle(GetAccountTransactionsQuery request, CancellationToken cancellationToken)
    {
        var account = db.Accounts
            .Find(a => a.Id == request.AccountId && a.ClosedAt is null);

        if (account == null)
            throw new KeyNotFoundException($"Account {request.AccountId} not found");

        var accountTransactionsDto = mapper.Map<AccountTransactionsDto>(account);

        return Task.FromResult(accountTransactionsDto);
    }
}