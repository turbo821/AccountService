using AccountService.Features.Accounts.Abstractions;
using AccountService.Infrastructure.Persistence;
using AutoMapper;
using MediatR;

namespace AccountService.Features.Accounts.CreateAccount;

public class CreateAccountHandler(
    IMapper mapper, StubDbContext db,
    ICurrencyValidator currencyValidator,
    IOwnerVerificator ownerVerificator)
    : IRequestHandler<CreateAccountCommand, Guid>
{
    public Task<Guid> Handle(CreateAccountCommand request, CancellationToken cancellationToken)
    {
        if (!ownerVerificator.IsExists(request.OwnerId))
            throw new ArgumentException("Client with this ID not found");

        if (!currencyValidator.IsValid(request.Currency))
            throw new ArgumentException("Unsupported currency");

        var account = mapper.Map<Account>(request);

        db.Accounts.Add(account);

        return Task.FromResult(account.Id);
    }
}