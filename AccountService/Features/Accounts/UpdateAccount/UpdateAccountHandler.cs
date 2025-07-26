using AccountService.Infrastructure.Persistence;
using MediatR;

namespace AccountService.Features.Accounts.UpdateAccount;

public class UpdateAccountHandler(StubDbContext db) : IRequestHandler<UpdateAccountCommand>
{
    public Task Handle(UpdateAccountCommand request, CancellationToken cancellationToken)
    {
        var account = db.Accounts
            .Find(a => a.Id == request.Id);

        if (account is null)
            throw new KeyNotFoundException($"Account with id {request.Id} not found");

        if (account.ClosedAt != null)
            throw new InvalidOperationException("Cannot update a closed account.");

        account.InterestRate = request.InterestRate;

        return Task.CompletedTask;
    }
}