using AccountService.Application.Models;
using AccountService.Features.Accounts.Abstractions;
using AutoMapper;
using MediatR;

namespace AccountService.Features.Accounts.GetAccountStatement;

public class GetAccountStatementHandler(IAccountRepository repo, IMapper mapper)
    : IRequestHandler<GetAccountStatementQuery, MbResult<AccountStatementDto>>
{
    public async Task<MbResult<AccountStatementDto>> Handle(GetAccountStatementQuery request, CancellationToken cancellationToken)
    {
        var account = await repo
            .GetByIdWithTransactionsForPeriodAsync(request.AccountId, request.From, request.To);

        if (account == null)
            throw new KeyNotFoundException($"Account {request.AccountId} not found");

        var accountTransactionsDto = mapper.Map<AccountStatementDto>(account);

        return new MbResult<AccountStatementDto>(accountTransactionsDto);
    }
}