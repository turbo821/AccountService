using AccountService.Application.Models;
using AccountService.Infrastructure.Persistence;
using AutoMapper;
using MediatR;

namespace AccountService.Features.Accounts.GetAccountStatement;

public class GetAccountStatementHandler(StubDbContext db, IMapper mapper)
    : IRequestHandler<GetAccountStatementQuery, MbResult<AccountStatementDto>>
{
    public Task<MbResult<AccountStatementDto>> Handle(GetAccountStatementQuery request, CancellationToken cancellationToken)
    {
        var account = db.Accounts
            .Find(a => a.Id == request.AccountId && a.ClosedAt is null);

        if (account == null)
            throw new KeyNotFoundException($"Account {request.AccountId} not found");

        var transactions = db.Transactions
            .Where(t => t.AccountId == account.Id)
            .Where(t => !request.From.HasValue || t.Timestamp >= request.From.Value)
            .Where(t => !request.To.HasValue || t.Timestamp <= request.To.Value)
            .ToList();

        account.Transactions = transactions;
        var accountTransactionsDto = mapper.Map<AccountStatementDto>(account);

        return Task.FromResult(new MbResult<AccountStatementDto>(accountTransactionsDto));
    }
}