using AccountService.Application.Models;
using AccountService.Features.Accounts.Abstractions;
using MediatR;

namespace AccountService.Features.Accounts.DeleteAccount;

public class DeleteAccountHandler(IAccountRepository repo) : IRequestHandler<DeleteAccountCommand, MbResult<AccountIdDto>>
{
    public  async Task<MbResult<AccountIdDto>> Handle(DeleteAccountCommand request, CancellationToken cancellationToken)
    {
        var accountId = await repo.SoftDeleteAsync(request.AccountId);

        if (accountId is null)
            throw new KeyNotFoundException($"Account {request.AccountId} not found.");

        var accountIdDto = new AccountIdDto { AccountId = accountId.Value };

        return new MbResult<AccountIdDto>(accountIdDto);
    }
}