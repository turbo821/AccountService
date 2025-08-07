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

        var accounts = await repo.GetByOwnerIdAsync(request.OwnerId);
        accounts ??= [];

        CheckOwnerAccountsDto dto;


        if (accounts.Count == 0)
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
                AccountCount = accounts.Count
            };
        }

        return new MbResult<CheckOwnerAccountsDto>(dto);
    }
}