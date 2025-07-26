using MediatR;
using AccountService.Infrastructure.Persistence;

namespace AccountService.Features.Accounts.DeleteAccount;

public class DeleteAccountHandler(StubDbContext db) : IRequestHandler<DeleteAccountCommand, Guid>
{
    public  Task<Guid> Handle(DeleteAccountCommand request, CancellationToken cancellationToken)
    {
        var account = db.Accounts
            .Find(x => x.Id == request.Id);

        if (account is null)
            throw new KeyNotFoundException($"Account {request.Id} not found");

        if (account.ClosedAt != null)
            throw new InvalidOperationException("Account is already closed.");

        account.ClosedAt = DateTime.UtcNow;

        return Task.FromResult(account.Id);
    }
}