using AccountService.Application.Models;
using AccountService.Features.Accounts.Abstractions;
using MediatR;
using System.Data;
using AccountService.Application.Abstractions;
using AccountService.Features.Accounts.Contracts;
using AutoMapper;

namespace AccountService.Features.Accounts.UpdateInterestRate;

public class UpdateInterestRateHandler(IAccountRepository accRepo, 
    IOutboxRepository outboxRepo, IMapper mapper) : IRequestHandler<UpdateInterestRateCommand, MbResult<Unit>>
{
    public async Task<MbResult<Unit>> Handle(UpdateInterestRateCommand request, CancellationToken cancellationToken)
    {
        await using var transaction = await accRepo.BeginTransactionAsync();
        try
        {
            var account = await accRepo.GetByIdAsync(request.AccountId, transaction);

            if (account is null)
                throw new KeyNotFoundException($"Account with id {request.AccountId} not found");

            if (account.Type is AccountType.Checking)
                throw new ArgumentException("Interest rate must not be set for Checking accounts");

            account.InterestRate = request.InterestRate;

            var updated = await accRepo.UpdateInterestRateAsync(account, transaction);
            if (updated == 0)
                throw new DBConcurrencyException("Account was modified by another process");

            var accountInterestUpdated = mapper.Map<AccountInterestUpdated>(account);
            await outboxRepo.AddAsync(accountInterestUpdated, "account.events", "account.interest.updated",
                transaction);

            await transaction.CommitAsync(cancellationToken);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }

        return new MbResult<Unit>(Unit.Value);
    }
}