using MediatR;

namespace AccountService.Features.Accounts.GetAccountTransactions;

/// <summary>
/// Запрос на получение информации о транзакциях конкретного счёта.
/// </summary>
/// <param name="AccountId">ID счёта.</param>
public record GetAccountTransactionsQuery(Guid AccountId) : IRequest<AccountTransactionsDto>;