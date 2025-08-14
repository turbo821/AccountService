using AccountService.Application.Models;
using AccountService.Features.Accounts.Abstractions;
using MediatR;
using System.Data;

namespace AccountService.Features.Accounts.UpdateInterestRate;

public class UpdateInterestRateHandler(IAccountRepository repo) : IRequestHandler<UpdateInterestRateCommand, MbResult<Unit>>
{
    public async Task<MbResult<Unit>> Handle(UpdateInterestRateCommand request, CancellationToken cancellationToken)
    {
        var account = await repo.GetByIdAsync(request.AccountId);

        if (account is null)
            throw new KeyNotFoundException($"Account with id {request.AccountId} not found");

        if (account.Type is AccountType.Checking)
            throw new ArgumentException("Interest rate must not be set for Checking accounts");

        account.InterestRate = request.InterestRate;

        var updated = await repo.UpdateInterestRateAsync(account);
        if (updated == 0)
            throw new DBConcurrencyException("Account was modified by another process");

        return new MbResult<Unit>(Unit.Value);
    }
}