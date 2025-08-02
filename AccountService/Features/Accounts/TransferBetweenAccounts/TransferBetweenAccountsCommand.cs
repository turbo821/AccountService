using AccountService.Application.Models;
using MediatR;

namespace AccountService.Features.Accounts.TransferBetweenAccounts;

/// <summary>
/// Команда для перевода средств между двумя счетами.
/// </summary>
/// <param name="FromAccountId">ID счёта отправителя.</param>
/// <param name="ToAccountId">ID счёта получателя.</param>
/// <param name="Amount">Сумма перевода.</param>
/// <param name="Currency">Валюта перевода (в формате ISO 4217, например, "USD").</param>
/// <param name="Description">Описание перевода.</param>
public record TransferBetweenAccountsCommand(
    Guid FromAccountId,
    Guid ToAccountId,
    decimal Amount,
    string Currency,
    string Description
) : IRequest<MbResult<IReadOnlyList<TransactionIdDto>>>;