using MediatR;

namespace AccountService.Features.Accounts.RegisterTransaction;

public record RegisterTransactionCommand(
    Guid AccountId,
    decimal Amount,
    string Currency,
    TransactionType Type,
    string Description
) : IRequest<Guid>;

public record RegisterTransactionRequest(
    decimal Amount,
    string Currency,
    TransactionType Type,
    string Description
);