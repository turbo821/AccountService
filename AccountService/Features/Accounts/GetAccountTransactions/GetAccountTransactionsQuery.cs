using MediatR;

namespace AccountService.Features.Accounts.GetAccountTransactions;

public record GetAccountTransactionsQuery(Guid AccountId) : IRequest<AccountTransactionsDto>;