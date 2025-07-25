namespace AccountService.Features.Accounts.Abstractions;

public interface IOwnerVerificator
{
    bool IsExists(Guid ownerId);
}