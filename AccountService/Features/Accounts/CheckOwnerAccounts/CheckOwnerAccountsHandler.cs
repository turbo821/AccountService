using AccountService.Application.Models;
using AccountService.Features.Accounts.Abstractions;
using MediatR;

namespace AccountService.Features.Accounts.CheckOwnerAccounts;

public class CheckOwnerAccountsHandler(IAccountRepository repo,
    IOwnerVerificator ownerVerificator)
    : IRequestHandler<CheckOwnerAccountsQuery, MbResult<CheckOwnerAccountsDto>>
{
    public async Task<MbResult<CheckOwnerAccountsDto>> Handle(CheckOwnerAccountsQuery request, CancellationToken cancellationToken)
    {
        if (!ownerVerificator.IsExists(request.OwnerId))
            throw new KeyNotFoundException("Client with this ID not found");

        var accountCount = await repo.GetCountByOwnerIdAsync(request.OwnerId);

        CheckOwnerAccountsDto dto;

        if (accountCount == 0)
        {
            dto = new CheckOwnerAccountsDto
            {
                OwnerId = request.OwnerId,
                AccountExists = false
            };
        }
        else
        {
            dto = new CheckOwnerAccountsDto
            {
                OwnerId = request.OwnerId,
                AccountExists = true,
                AccountCount = accountCount
            };
        }

        return new MbResult<CheckOwnerAccountsDto>(dto);
    }
}