using AccountService.Features.Accounts.Abstractions;
using AutoMapper;
using MediatR;

namespace AccountService.Features.Accounts.CreateAccount;

public class CreateAccountHandler(
    IMapper mapper,
    IAccountRepository repo,
    ICurrencyValidator currencyValidator,
    IOwnerVerificator ownerVerificator)
    : IRequestHandler<CreateAccountCommand, Guid>
{
    public async Task<Guid> Handle(CreateAccountCommand request, CancellationToken cancellationToken)
    {
        if (!ownerVerificator.IsExists(request.OwnerId))
            throw new ArgumentException("Client with this ID not found");

        if (!currencyValidator.IsValid(request.Currency))
            throw new ArgumentException("Unsupported currency");

        var account = mapper.Map<Account>(request);

        await repo.Add(account);

        return account.Id;
    }
}