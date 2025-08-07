using AccountService.Application.Models;
using AccountService.Features.Accounts.Abstractions;
using AccountService.Infrastructure.Persistence;
using AutoMapper;
using MediatR;

namespace AccountService.Features.Accounts.UpdateAccount;

public class UpdateAccountHandler(
    IMapper mapper, AppDbContext db,
    ICurrencyValidator currencyValidator,
    IOwnerVerificator ownerVerificator) : IRequestHandler<UpdateAccountCommand, MbResult<Unit>>
{
    public Task<MbResult<Unit>> Handle(UpdateAccountCommand request, CancellationToken cancellationToken)
    {
        var account = db.Accounts2
            .Find(a => a.Id == request.AccountId && a.ClosedAt is null);

        if (account is null)
            throw new KeyNotFoundException($"Account with id {request.AccountId} not found");

        if (!ownerVerificator.IsExists(request.OwnerId))
            throw new ArgumentException("Client with this ID not found");

        if (!currencyValidator.IsExists(request.Currency))
            throw new ArgumentException("Unsupported currency");

        request.OpenedAt ??= account.OpenedAt;

        db.Accounts2.Remove(account); // Change to Update operation

        account = mapper.Map<Account>(request);
        
        db.Accounts2.Add(account); // Change to Update operation

        return Task.FromResult(new MbResult<Unit>(Unit.Value));
    }
}