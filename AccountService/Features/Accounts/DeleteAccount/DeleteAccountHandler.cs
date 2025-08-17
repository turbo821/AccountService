using AccountService.Application.Models;
using AccountService.Features.Accounts.Abstractions;
using MediatR;
using AccountService.Application.Abstractions;
using AccountService.Application.Contracts;
using AccountService.Features.Accounts.Contracts;

namespace AccountService.Features.Accounts.DeleteAccount;

public class DeleteAccountHandler(IAccountRepository accRepo, 
    IOutboxRepository outboxRepo) : IRequestHandler<DeleteAccountCommand, MbResult<AccountIdDto>>
{
    public  async Task<MbResult<AccountIdDto>> Handle(DeleteAccountCommand request, CancellationToken cancellationToken)
    {
        Guid? accountId;

        await using var transaction = await accRepo.BeginTransactionAsync();
        try
        {
            accountId = await accRepo.SoftDeleteAsync(request.AccountId, transaction);

            if (accountId is null)
                throw new KeyNotFoundException($"Account {request.AccountId} not found.");

            var accountClosed = new AccountClosed(Guid.NewGuid(), DateTime.UtcNow, accountId.Value);
            accountClosed.Meta = new EventMeta(
                "account-service",
                Guid.NewGuid(), accountClosed.EventId
            );

            await outboxRepo.AddAsync(accountClosed, "account.events", "account.closed", transaction);

            await transaction.CommitAsync(cancellationToken);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }

        var accountIdDto = new AccountIdDto { AccountId = accountId.Value };

        return new MbResult<AccountIdDto>(accountIdDto);
    }
}