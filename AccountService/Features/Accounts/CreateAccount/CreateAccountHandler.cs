using AccountService.Application.Abstractions;
using AccountService.Application.Models;
using AccountService.Features.Accounts.Abstractions;
using AccountService.Features.Accounts.Contracts;
using AutoMapper;
using MediatR;

namespace AccountService.Features.Accounts.CreateAccount;

public class CreateAccountHandler(
    IMapper mapper, IAccountRepository repo,
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

        var accountOpenedEvent = new AccountOpened(
            Guid.NewGuid(),
            DateTime.UtcNow,
            account.Id,
            account.OwnerId,
            account.Currency,
            account.Type.ToString()
        );

        await repo.AddAsync(account, accountOpenedEvent);

        var accountIdDto = mapper.Map<AccountIdDto>(account);
        return new MbResult<AccountIdDto>(accountIdDto);
    }
}