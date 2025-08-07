using AccountService.Application.Models;
using AccountService.Features.Accounts.Abstractions;
using AutoMapper;
using MediatR;

namespace AccountService.Features.Accounts.DeleteAccount;

public class DeleteAccountHandler(IAccountRepository repo, IMapper mapper) : IRequestHandler<DeleteAccountCommand, MbResult<AccountIdDto>>
{
    public  async Task<MbResult<AccountIdDto>> Handle(DeleteAccountCommand request, CancellationToken cancellationToken)
    {
        var account = await repo.GetByIdAsync(request.AccountId);

        if (account is null)
            throw new KeyNotFoundException($"Account {request.AccountId} not found");

        if (account.ClosedAt != null)
            throw new InvalidOperationException("Account is already closed.");

        await repo.SoftDeleteAsync(account);

        var accountIdDto = mapper.Map<AccountIdDto>(account);
        return new MbResult<AccountIdDto>(accountIdDto);
    }
}