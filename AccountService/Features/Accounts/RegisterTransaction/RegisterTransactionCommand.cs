using AccountService.Application.Models;
using MediatR;

namespace AccountService.Features.Accounts.RegisterTransaction;

/// <summary>
/// Команда для регистрации транзакции на счёте.
/// </summary>
/// <param name="AccountId">Идентификатор счёта, на котором производится операция.</param>
/// <param name="Amount">Сумма транзакции.</param>
/// <param name="Currency">Валюта транзакции (в формате ISO 4217, например, "USD").</param>
/// <param name="Type">Тип транзакции: пополнение или списание.</param>
/// <param name="Description">Описание транзакции.</param>
public record RegisterTransactionCommand(
    Guid AccountId,
    decimal Amount,
    string Currency,
    TransactionType Type,
    string Description
) : IRequest<MbResult<TransactionIdDto>>;

/// <summary>
/// Запрос на регистрацию транзакции.
/// </summary>
/// <param name="Amount">Сумма транзакции.</param>
/// <param name="Currency">Валюта транзакции (в формате ISO 4217, например, "USD").</param>
/// <param name="Type">Тип транзакции: пополнение или списание.</param>
/// <param name="Description">Описание транзакции.</param>
public record RegisterTransactionRequest(
    decimal Amount,
    string Currency,
    TransactionType Type,
    string Description
);