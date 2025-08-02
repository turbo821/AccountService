using AccountService.Application.Models;
using AccountService.Features.Accounts;
using AccountService.Features.Accounts.CheckOwnerAccounts;
using AccountService.Features.Accounts.CreateAccount;
using AccountService.Features.Accounts.DeleteAccount;
using AccountService.Features.Accounts.GetAccountById;
using AccountService.Features.Accounts.GetAccountList;
using AccountService.Features.Accounts.GetAccountStatement;
using AccountService.Features.Accounts.RegisterTransaction;
using AccountService.Features.Accounts.TransferBetweenAccounts;
using AccountService.Features.Accounts.UpdateAccount;
using AccountService.Features.Accounts.UpdateInterestRate;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace AccountService.Controllers;

/// <summary>
/// Контроллер для управления банковскими счетами.
/// </summary>
[ApiController]
[Route("/accounts")]
public class AccountsController(IMediator mediator) : ControllerBase
{
    /// <summary>
    /// Создаёт новый банковский счёт с нулевым балансом.
    /// </summary>
    /// <param name="command">Команда с данными для создания счёта.</param>
    /// <returns>ID созданного счёта.</returns>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(MbResult<AccountIdDto>))]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(MbResult<Unit>))]
    public async Task<IActionResult> CreateAccount([FromBody] CreateAccountCommand command)
    {
        var response = await mediator.Send(command);
        return Ok(response);
    }

    /// <summary>
    /// Возвращает список всех открытых счетов (опционально для конкретного владельца).
    /// </summary>
    /// <param name="ownerId">ID владельца счетов (опционально).</param>
    /// <returns>Список счетов.</returns>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(MbResult<IReadOnlyList<AccountDto>>))]
    public async Task<IActionResult> GetAccountList([FromQuery] Guid? ownerId)
    {
        var query = new GetAccountListQuery(ownerId);
        var response = await mediator.Send(query);
        return Ok(response);
    }

    /// <summary>
    /// Возвращает информацию о конкретном счёте по его ID.
    /// </summary>
    /// <param name="id">ID счёта.</param>
    /// <returns>Информация о счёте.</returns>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(MbResult<AccountDto>))]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(MbResult<Unit>))]
    [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(MbResult<Unit>))]
    public async Task<IActionResult> GetAccountById(Guid id)
    {
        var query = new GetAccountByIdQuery(id);
        var response = await mediator.Send(query);
        return Ok(response);
    }

    /// <summary>
    /// Изменяет данные счёта по его ID.
    /// </summary>
    /// <param name="id">ID изменяемого счета.</param>
    /// <param name="request">Запрос с данными на изменение счета.</param>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(MbResult<Unit>))]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(MbResult<Unit>))]
    [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(MbResult<Unit>))]
    public async Task<IActionResult> UpdateAccount(Guid id, [FromBody] UpdateAccountRequest request)
    {
        var command = new UpdateAccountCommand(
            id,
            request.OwnerId,
            request.Type,
            request.Currency,
            request.Balance,
            request.InterestRate,
            request.OpenedAt
        );

        var response = await mediator.Send(command);
        return Ok(response);
    }

    /// <summary>
    /// Обновляет процентную ставку по счёту.
    /// </summary>
    /// <param name="id">ID счёта.</param>
    /// <param name="request">Запрос с новой процентной ставкой.</param>
    /// <returns>Статус выполнения.</returns>
    [HttpPatch("{id:guid}/interest-rate")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(MbResult<Unit>))]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(MbResult<Unit>))]
    [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(MbResult<Unit>))]
    public async Task<IActionResult> UpdateAccountInterestRate(Guid id, [FromBody] UpdateInterestRateRequest request)
    {
        var command = new UpdateInterestRateCommand(id, request.InterestRate);
        var response = await mediator.Send(command);
        return Ok(response);
    }

    /// <summary>
    /// Закрывает счёт по его ID - счет не отображается публично. Альтернатива мягкого удаления.
    /// </summary>
    /// <param name="id">ID счёта.</param>
    /// <returns>ID закрытого счёта.</returns>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(MbResult<AccountIdDto>))]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(MbResult<Unit>))]
    [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(MbResult<Unit>))]
    public async Task<IActionResult> DeleteAccount(Guid id)
    {
        var command = new DeleteAccountCommand(id);
        var response = await mediator.Send(command);
        return Ok(response);
    }

    /// <summary>
    /// Регистрирует транзакцию по счёту (пополнение или списание).
    /// </summary>
    /// <param name="accountId">ID счёта.</param>
    /// <param name="request">Данные о транзакции.</param>
    /// <returns>ID созданной транзакции.</returns>
    [HttpPost("{accountId:guid}/transactions")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(MbResult<TransactionIdDto>))]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(MbResult<Unit>))]
    [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(MbResult<Unit>))]
    public async Task<IActionResult> RegisterTransaction(Guid accountId, [FromBody] RegisterTransactionRequest request)
    {
        var command = new RegisterTransactionCommand(
            accountId,
            request.Amount,
            request.Currency,
            request.Type,
            request.Description
        );
        var response = await mediator.Send(command);
        return Ok(response);
    }

    /// <summary>
    /// Переводит средства между двумя счетами. Регистрация транзакций противоположных типов (пополнение и списание) для обоих счетов.
    /// </summary>
    /// <param name="command">Команда перевода между счетами.</param>
    /// <returns>Статус выполнения.</returns>
    [HttpPost("transfer")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(MbResult<IReadOnlyList<TransactionIdDto>>))]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(MbResult<Unit>))]
    [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(MbResult<Unit>))]
    public async Task<IActionResult> TransferBetweenAccounts([FromBody] TransferBetweenAccountsCommand command)
    {
        var response = await mediator.Send(command);
        return Ok(response);
    }

    /// <summary>
    /// Возвращает выписку по счету за определенный период.
    /// </summary>
    /// <param name="accountId">Идентификатор счёта.</param>
    /// <param name="fromDate">Дата начала периода (опционально).</param>
    /// <param name="toDate">Дата окончания периода (опционально).</param>
    /// <returns>Выписка по счёту.</returns>
    [HttpGet("{accountId:guid}/transactions")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(MbResult<AccountStatementDto>))]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(MbResult<Unit>))]
    [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(MbResult<Unit>))]
    public async Task<IActionResult> GetAccountStatement(Guid accountId, [FromQuery] DateTime? fromDate, [FromQuery] DateTime? toDate)
    {
        var query = new GetAccountStatementQuery(accountId, fromDate, toDate);
        var response = await mediator.Send(query);
        return Ok(response);
    }

    /// <summary>
    /// Проверяет наличие счетов у владельца.
    /// </summary>
    /// <param name="ownerId">Идентификатор владельца.</param>
    /// <returns>Информация о счетах владельца.</returns>
    [HttpGet("check-owner/{ownerId:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(MbResult<CheckOwnerAccountsDto>))]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(MbResult<Unit>))]
    [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(MbResult<Unit>))]
    public async Task<IActionResult> CheckOwnerAccounts(Guid ownerId)
    {
        var query = new CheckOwnerAccountsQuery(ownerId);
        var response = await mediator.Send(query);
        return Ok(response);
    }
}
