using AccountService.Application.Models;
using MediatR;

namespace AccountService.Features.Accounts.GetAccountStatement;

/// <summary>
/// Запрос на получение выписки по счету за определенный период.
/// </summary>
/// <param name="AccountId">ID счёта.</param>
/// <param name="From">Дата начала периода (опционально).</param>
/// <param name="To">Дата окончания периода (опционально).</param>
public record GetAccountStatementQuery(
    Guid AccountId, 
    DateTime? From, 
    DateTime? To
    ) : IRequest<MbResult<AccountStatementDto>>;