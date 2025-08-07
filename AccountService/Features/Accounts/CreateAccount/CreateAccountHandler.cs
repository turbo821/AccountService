using AccountService.Application.Models;
using AccountService.Features.Accounts.Abstractions;
using AccountService.Infrastructure.Persistence;
using AutoMapper;
using MediatR;

namespace AccountService.Features.Accounts.CreateAccount;

public class CreateAccountHandler(
    IMapper mapper, StubDbContext db,
    ICurrencyValidator currencyValidator,
    IOwnerVerificator ownerVerificator)
    : IRequestHandler<CreateAccountCommand, MbResult<AccountIdDto>>
{
    public Task<MbResult<AccountIdDto>> Handle(CreateAccountCommand request, CancellationToken cancellationToken)
    {
        if (!ownerVerificator.IsExists(request.OwnerId))
            throw new ArgumentException("Client with this ID not found");

        if (!currencyValidator.IsExists(request.Currency))
            throw new ArgumentException("Unsupported currency");

        var account = mapper.Map<Account>(request);

        db.Accounts.Add(account);

        var accountIdDto = mapper.Map<AccountIdDto>(account);
        return Task.FromResult(new MbResult<AccountIdDto>(accountIdDto));
    }
}