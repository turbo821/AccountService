using AccountService.Features.Accounts.Abstractions;

namespace AccountService.Infrastructure.Services;

public class OwnerVerificatorStub : IOwnerVerificator
{
    public bool IsExists(Guid ownerId)
    {
        return true;
    }
}