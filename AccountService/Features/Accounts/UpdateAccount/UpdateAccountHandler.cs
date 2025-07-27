using AccountService.Infrastructure.Persistence;
using MediatR;

namespace AccountService.Features.Accounts.UpdateAccount;

public class UpdateAccountHandler(StubDbContext db) : IRequestHandler<UpdateAccountCommand>
{
    public Task Handle(UpdateAccountCommand request, CancellationToken cancellationToken)
    {
        var account = db.Accounts
            .Find(a => a.Id == request.AccountId && a.ClosedAt is null);

        if (account is null)
            throw new KeyNotFoundException($"Account with id {request.AccountId} not found");

        if (account.Type is AccountType.Checking)
            throw new ArgumentException("Interest rate must not be set for Checking accounts");

        account.InterestRate = request.InterestRate;

        return Task.CompletedTask;
    }
}