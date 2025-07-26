using AccountService.Features.Accounts.Abstractions;
using AccountService.Infrastructure.Persistence;
using MediatR;

namespace AccountService.Features.Accounts.CheckOwnerAccounts;

public class CheckOwnerAccountsHandler(StubDbContext db,
    IOwnerVerificator ownerVerificator)
    : IRequestHandler<CheckOwnerAccountsQuery, CheckOwnerAccountsDto>
{
    public Task<CheckOwnerAccountsDto> Handle(CheckOwnerAccountsQuery request, CancellationToken cancellationToken)
    {
        if (!ownerVerificator.IsExists(request.OwnerId))
            throw new KeyNotFoundException("Client with this ID not found");

        var accounts = db.Accounts
            .Where(a => a.OwnerId == request.OwnerId && a.ClosedAt is null)
            .Select(a => a.Id).ToList();

        CheckOwnerAccountsDto dto;

        if (accounts.Count == 0)
        {
            dto = new CheckOwnerAccountsDto
            {
                OwnerId = request.OwnerId,
                Exists = false
            };
        }
        else
        {
            dto = new CheckOwnerAccountsDto
            {
                OwnerId = request.OwnerId,
                Exists = true,
                AccountIds = accounts.ToList()
            };
        }

        return Task.FromResult(dto);
    }
}