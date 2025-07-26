using MediatR;

namespace AccountService.Features.Accounts.TransferBetweenAccounts;

public record TransferBetweenAccountsCommand(
    Guid FromAccountId,
    Guid ToAccountId,
    decimal Amount,
    string Currency,
    string Description
) : IRequest;