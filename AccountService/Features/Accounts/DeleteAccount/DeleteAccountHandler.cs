using AccountService.Application.Models;
using AccountService.Infrastructure.Persistence;
using AutoMapper;
using MediatR;

namespace AccountService.Features.Accounts.DeleteAccount;

public class DeleteAccountHandler(StubDbContext db, IMapper mapper) : IRequestHandler<DeleteAccountCommand, MbResult<AccountIdDto>>
{
    public  Task<MbResult<AccountIdDto>> Handle(DeleteAccountCommand request, CancellationToken cancellationToken)
    {
        var account = db.Accounts
            .Find(x => x.Id == request.AccountId);

        if (account is null)
            throw new KeyNotFoundException($"Account {request.AccountId} not found");

        if (account.ClosedAt != null)
            throw new InvalidOperationException("Account is already closed.");

        account.ClosedAt = DateTime.UtcNow;

        var accountIdDto = mapper.Map<AccountIdDto>(account);
        return Task.FromResult(new MbResult<AccountIdDto>(accountIdDto));
    }
}