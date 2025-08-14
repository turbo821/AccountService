using AccountService.Application.Models;
using AccountService.Features.Accounts.Abstractions;
using AutoMapper;
using MediatR;
using System.Data;

namespace AccountService.Features.Accounts.UpdateAccount;

public class UpdateAccountHandler(
    IMapper mapper, IAccountRepository repo,
    ICurrencyValidator currencyValidator,
    IOwnerVerificator ownerVerificator) : IRequestHandler<UpdateAccountCommand, MbResult<Unit>>
{
    public async Task<MbResult<Unit>> Handle(UpdateAccountCommand request, CancellationToken cancellationToken)
    {
        var account = await repo.GetByIdAsync(request.AccountId);

        if (account is null)
            throw new KeyNotFoundException($"Account with id {request.AccountId} not found");

        if (!ownerVerificator.IsExists(request.OwnerId))
            throw new ArgumentException("Client with this ID not found");

        if (!await currencyValidator.IsExists(request.Currency))
            throw new ArgumentException("Unsupported currency");

        request.OpenedAt ??= account.OpenedAt;

        var newAccount = mapper.Map<Account>(request);

        newAccount.Id = account.Id;
        newAccount.Version = account.Version;

        var updated = await repo.UpdateAsync(newAccount);
        if (updated == 0)
            throw new DBConcurrencyException("Account was modified by another process");

        return new MbResult<Unit>(Unit.Value);
    }
}