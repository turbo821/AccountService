using AccountService.Application.Models;
using AccountService.Features.Accounts.Abstractions;
using AutoMapper;
using MediatR;
using System.Data;
using AccountService.Application.Abstractions;
using AccountService.Application.Contracts;
using AccountService.Features.Accounts.Contracts;

namespace AccountService.Features.Accounts.UpdateAccount;

public class UpdateAccountHandler(
    IMapper mapper, IAccountRepository accRepo,
    IOutboxRepository outboxRepo, ICurrencyValidator currencyValidator,
    IOwnerVerificator ownerVerificator) : IRequestHandler<UpdateAccountCommand, MbResult<Unit>>
{
    public async Task<MbResult<Unit>> Handle(UpdateAccountCommand request, CancellationToken cancellationToken)
    {
        if (!await currencyValidator.IsExists(request.Currency))
            throw new ArgumentException("Unsupported currency");

        await using var transaction = await accRepo.BeginTransactionAsync();

        try
        {
            var account = await accRepo.GetByIdAsync(request.AccountId, transaction);

            if (account is null)
                throw new KeyNotFoundException($"Account with id {request.AccountId} not found");

            if (!ownerVerificator.IsExists(request.OwnerId))
                throw new ArgumentException("Client with this ID not found");

            request.OpenedAt ??= account.OpenedAt;

            var newAccount = mapper.Map<Account>(request);

            newAccount.Id = account.Id;
            newAccount.Version = account.Version;

            var updated = await accRepo.UpdateAsync(newAccount, transaction);
            if (updated == 0)
                throw new DBConcurrencyException("Account was modified by another process");

            var accountUpdated = mapper.Map<AccountUpdated>(request);
            accountUpdated.Meta = new EventMeta(
                "account-service",
                Guid.NewGuid(), accountUpdated.EventId
            );  

            await outboxRepo.AddAsync(accountUpdated, "account.events", "account.updated", transaction);

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