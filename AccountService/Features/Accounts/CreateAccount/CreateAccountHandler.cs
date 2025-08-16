using AccountService.Application.Abstractions;
using AccountService.Application.Models;
using AccountService.Features.Accounts.Abstractions;
using AccountService.Features.Accounts.Contracts;
using AutoMapper;
using MediatR;

namespace AccountService.Features.Accounts.CreateAccount;

public class CreateAccountHandler(
    IMapper mapper, IAccountRepository accRepo, IOutboxRepository outboxRepo,
    ICurrencyValidator currencyValidator,
    IOwnerVerificator ownerVerificator)
    : IRequestHandler<CreateAccountCommand, MbResult<AccountIdDto>>
{
    public async Task<MbResult<AccountIdDto>> Handle(CreateAccountCommand request, CancellationToken cancellationToken)
    {
        if (!ownerVerificator.IsExists(request.OwnerId))
            throw new ArgumentException("Client with this ID not found");

        if (!await currencyValidator.IsExists(request.Currency))
            throw new ArgumentException("Unsupported currency");

        var account = mapper.Map<Account>(request);

        var accountOpenedEvent = mapper.Map<AccountOpened>(account);

        await using var transaction = await accRepo.BeginTransactionAsync();
        try
        {
            await accRepo.AddAsync(account);
            await outboxRepo.AddAsync(accountOpenedEvent, "account.events", "account.opened");
            await transaction.CommitAsync(cancellationToken);
        }
        catch 
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }

        var accountIdDto = mapper.Map<AccountIdDto>(account);
        return new MbResult<AccountIdDto>(accountIdDto);
    }
}