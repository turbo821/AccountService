using MediatR;

namespace AccountService.Features.Accounts.DeleteAccount;

/// <summary>
/// Команда для удаления банковского счёта по его ID.
/// </summary>
/// <param name="AccountId">ID счета</param>
public record DeleteAccountCommand(Guid AccountId) : IRequest<Guid>;