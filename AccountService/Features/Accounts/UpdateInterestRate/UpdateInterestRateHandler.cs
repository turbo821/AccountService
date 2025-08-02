using AccountService.Application.Models;
using AccountService.Infrastructure.Persistence;
using MediatR;

namespace AccountService.Features.Accounts.UpdateInterestRate;

public class UpdateInterestRateHandler(StubDbContext db) : IRequestHandler<UpdateInterestRateCommand, MbResult<Unit>>
{
    public Task<MbResult<Unit>> Handle(UpdateInterestRateCommand request, CancellationToken cancellationToken)
    {
        var account = db.Accounts
            .Find(a => a.Id == request.AccountId && a.ClosedAt is null);

        if (account is null)
            throw new KeyNotFoundException($"Account with id {request.AccountId} not found");

        if (account.Type is AccountType.Checking)
            throw new ArgumentException("Interest rate must not be set for Checking accounts");

        account.InterestRate = request.InterestRate;

        return Task.FromResult(new MbResult<Unit>(Unit.Value));
    }
}