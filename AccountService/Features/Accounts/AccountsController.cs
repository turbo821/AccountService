using AccountService.Features.Accounts.CheckOwnerAccounts;
using AccountService.Features.Accounts.CreateAccount;
using AccountService.Features.Accounts.DeleteAccount;
using AccountService.Features.Accounts.GetAccountById;
using AccountService.Features.Accounts.GetAccountList;
using AccountService.Features.Accounts.GetAccountTransactions;
using AccountService.Features.Accounts.RegisterTransaction;
using AccountService.Features.Accounts.TransferBetweenAccounts;
using AccountService.Features.Accounts.UpdateAccount;
using AccountService.Middlewares;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace AccountService.Features.Accounts;

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
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ErrorResponse))]
    public async Task<IActionResult> CreateAccount([FromBody] CreateAccountCommand command)
    {
        var accountId = await mediator.Send(command);
        return Ok(new { AccountId = accountId });
    }

    /// <summary>
    /// Возвращает список всех открытых счетов.
    /// </summary>
    /// <returns>Список счетов.</returns>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(IEnumerable<AccountDto>))]
    public async Task<IActionResult> GetAccountList()
    {
        var query = new GetAccountListQuery();
        var accounts = await mediator.Send(query);
        return Ok(accounts);
    }

    /// <summary>
    /// Возвращает информацию о конкретном счёте по его ID.
    /// </summary>
    /// <param name="id">ID счёта.</param>
    /// <returns>Информация о счёте.</returns>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(AccountDto))]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ErrorResponse))]
    [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ErrorResponse))]
    public async Task<IActionResult> GetAccountById(Guid id)
    {
        var query = new GetAccountByIdQuery(id);
        var account = await mediator.Send(query);
        return Ok(account);
    }

    /// <summary>
    /// Обновляет процентную ставку по счёту.
    /// </summary>
    /// <param name="id">ID счёта.</param>
    /// <param name="request">Запрос с новой процентной ставкой.</param>
    /// <returns>Статус выполнения.</returns>
    [HttpPatch("{id:guid}/interest-rate")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ErrorResponse))]
    [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ErrorResponse))]
    public async Task<IActionResult> UpdateAccountInterestRate(Guid id, [FromBody] UpdateInterestRateRequest request)
    {
        var command = new UpdateAccountCommand(id, request.InterestRate);
        await mediator.Send(command);
        return Ok();
    }

    /// <summary>
    /// Закрывает счёт по его ID - счет не отображается публично. Альтернатива мягкого удаления.
    /// </summary>
    /// <param name="id">ID счёта.</param>
    /// <returns>ID закрытого счёта.</returns>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ErrorResponse))]
    [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ErrorResponse))]
    public async Task<IActionResult> DeleteAccount(Guid id)
    {
        var command = new DeleteAccountCommand(id);
        var accountId = await mediator.Send(command);
        return Ok(new { AccountId = accountId });
    }

    /// <summary>
    /// Регистрирует транзакцию по счёту (пополнение или списание).
    /// </summary>
    /// <param name="accountId">ID счёта.</param>
    /// <param name="request">Данные о транзакции.</param>
    /// <returns>ID созданной транзакции.</returns>
    [HttpPost("{accountId:guid}/transactions")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ErrorResponse))]
    [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ErrorResponse))]
    public async Task<IActionResult> RegisterTransaction(Guid accountId, [FromBody] RegisterTransactionRequest request)
    {
        var command = new RegisterTransactionCommand(
            accountId,
            request.Amount,
            request.Currency,
            request.Type,
            request.Description
        );
        var transactionId = await mediator.Send(command);
        return Ok(new { TransactionId = transactionId });
    }

    /// <summary>
    /// Переводит средства между двумя счетами. Регистрация транзакций противоположных типов (пополнение и списание) для обоих счетов.
    /// </summary>
    /// <param name="command">Команда перевода между счетами.</param>
    /// <returns>Статус выполнения.</returns>
    [HttpPost("transfer")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ErrorResponse))]
    [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ErrorResponse))]
    public async Task<IActionResult> TransferBetweenAccounts([FromBody] TransferBetweenAccountsCommand command)
    {
        await mediator.Send(command);
        return Ok();
    }

    /// <summary>
    /// Возвращает список транзакций по счёту.
    /// </summary>
    /// <param name="accountId">Идентификатор счёта.</param>
    /// <returns>Выписка по счёту.</returns>
    [HttpGet("{accountId:guid}/transactions")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(AccountTransactionsDto))]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ErrorResponse))]
    [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ErrorResponse))]
    public async Task<IActionResult> GetStatement(Guid accountId)
    {
        var query = new GetAccountTransactionsQuery(accountId);
        var accountTransactionsDto = await mediator.Send(query);
        return Ok(accountTransactionsDto);
    }

    /// <summary>
    /// Проверяет наличие счетов у владельца.
    /// </summary>
    /// <param name="ownerId">Идентификатор владельца.</param>
    /// <returns>Информация о счетах владельца.</returns>
    [HttpGet("owner/{ownerId:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(CheckOwnerAccountsDto))]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ErrorResponse))]
    [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ErrorResponse))]
    public async Task<IActionResult> CheckOwnerAccounts(Guid ownerId)
    {
        var query = new CheckOwnerAccountsQuery(ownerId);
        var checkOwnerAccountsDto = await mediator.Send(query);
        return Ok(checkOwnerAccountsDto);
    }
}
