using AccountService.Application.Models;
using MediatR;

namespace AccountService.Features.Accounts.CreateAccount;

/// <summary>
/// Команда для создания нового банковского счёта.
/// </summary>
/// <param name="OwnerId">ID владельца.</param>
/// <param name="Type">Тип счёта (Debit, Credit).</param>
/// <param name="Currency">Валюта счёта ISO 4217 (например, RUB, USD).</param>
/// <param name="InterestRate">Процентная ставка (только для сберегательных или кредитных счетов).</param>
public record CreateAccountCommand(
    Guid OwnerId, 
    AccountType Type, 
    string Currency, 
    decimal? InterestRate) : IRequest<MbResult<AccountIdDto>>;