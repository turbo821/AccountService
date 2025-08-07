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
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AccountService.Controllers;

/// <summary>
/// Контроллер для управления банковскими счетами.
/// </summary>
[Authorize]
[ApiController]
[Route("/accounts")]
public class AccountsController(IMediator mediator) : ControllerBase
{
    /// <summary>
    /// Создаёт новый банковский счёт с нулевым балансом.
    /// </summary>
    /// <param name="command">Команда с данными для создания счёта.</param>
    /// <remarks>
    ///     <p>Для создания счёта нужно отправить необходимые данные. Счёт создастся при прохождении всех проверок с нулевым балансом.</p>
    ///     <p>ID владельца проверяется сервисом верификации владельца (заглушка позволяет ввести любой в формате GUID).</p>
    ///     <p>Тип счёта указывается из enum.</p>
    ///     <p>Валюта проверяется сервисом проверки валюты (placeholder позволяет задать ["USD", "EUR", "RUB", "KZT"]).</p>
    ///     <p>Процентная ставка для Checking должна быть null, для остальных - это положительное число.</p>
    /// </remarks>
    /// <returns>ID созданного счёта.</returns>
    /// <response code="200">Новый счёт создан.</response>
    /// <response code="400">Неверный запрос.</response>
    /// <response code="401">Нет доступа к запрашиваемому ресурсу.</response>
    [HttpPost]
    [ProducesResponseType(typeof(MbResult<AccountIdDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(MbResult<Unit>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(MbResult<Unit>), StatusCodes.Status401Unauthorized)]
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
    /// <response code="200">Получен список счетов.</response>
    /// <response code="401">Нет доступа к запрашиваемому ресурсу.</response>
    [HttpGet]
    [ProducesResponseType(typeof(MbResult<IReadOnlyList<AccountDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(MbResult<Unit>), StatusCodes.Status401Unauthorized)]
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
    /// <response code="200">Получены данные счета.</response>
    /// <response code="400">Неверный запрос.</response>
    /// <response code="401">Нет доступа к запрашиваемому ресурсу.</response>
    /// <response code="404">Счёт с таким ID не найден.</response>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(MbResult<AccountDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(MbResult<Unit>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(MbResult<Unit>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(MbResult<Unit>), StatusCodes.Status404NotFound)]
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
    /// <returns>Статус выполнения.</returns>
    /// <response code="200">Данные счёта успешно изменены.</response>
    /// <response code="400">Неверный запрос.</response>
    /// <response code="401">Нет доступа к запрашиваемому ресурсу.</response>
    /// <response code="404">Счёт с таким ID не найден.</response>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(MbResult<Unit>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(MbResult<Unit>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(MbResult<Unit>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(MbResult<Unit>), StatusCodes.Status404NotFound)]
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
    /// <response code="200">Процентная ставка счёта обновлена.</response>
    /// <response code="400">Неверный запрос.</response>
    /// <response code="401">Нет доступа к запрашиваемому ресурсу.</response>
    /// <response code="404">Счёт с таким ID не найден.</response>
    [HttpPatch("{id:guid}/interest-rate")]
    [ProducesResponseType(typeof(MbResult<Unit>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(MbResult<Unit>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(MbResult<Unit>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(MbResult<Unit>), StatusCodes.Status404NotFound)]
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
    /// <response code="200">Счёт успешно закрыт.</response>
    /// <response code="400">Неверный запрос.</response>
    /// <response code="401">Нет доступа к запрашиваемому ресурсу.</response>
    /// <response code="404">Счёт с таким ID не найден.</response>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(typeof(MbResult<AccountIdDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(MbResult<Unit>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(MbResult<Unit>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(MbResult<Unit>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteAccount(Guid id)
    {
        var command = new DeleteAccountCommand(id);
        var response = await mediator.Send(command);
        return Ok(response);
    }

    /// <summary>
    /// Регистрирует транзакцию по счёту (пополнение или списание).
    /// </summary>
    /// <remarks>
    ///     <p>Проводится и создается одна транзакция для существующего счёта по ID после всех проверок.</p>
    ///     <p>При создании для транзакций устанавливается CounterpartyAccountId=null</p>
    ///     <p>Сумма транзакции должна быть положительным числом.</p>
    ///     <p>Валюта проверяется сервисом проверки валюты (placeholder позволяет задать ["USD", "EUR", "RUB", "KZT"]).</p>
    ///     <p>Тип транзакции из enum: Debit - пополнение, Credit - списание.</p>
    ///     <p>Описание - это простой текст, подробности о транзакции (максимум 255 символов).</p>
    /// </remarks>
    /// <param name="accountId">ID счёта.</param>
    /// <param name="request">Данные о транзакции.</param>
    /// <returns>ID созданной транзакции.</returns>
    /// <response code="200">Транзакция успешно выполнена.</response>
    /// <response code="400">Неверный запрос.</response>
    /// <response code="401">Нет доступа к запрашиваемому ресурсу.</response>
    /// <response code="404">Счёт с таким ID не найден.</response>
    [HttpPost("{accountId:guid}/transactions")]
    [ProducesResponseType(typeof(MbResult<TransactionIdDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(MbResult<Unit>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(MbResult<Unit>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(MbResult<Unit>), StatusCodes.Status404NotFound)]
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
    /// <remarks>
    ///     <p>Проводятся и создаются две транзакция для двух существующих счетов.</p>
    ///     <p>Для fromAccount создается Credit транзакция (списание).</p>
    ///     <p>Для toAccount создается Debit транзакция (пополнение).</p>
    ///     <p>При создании для транзакций CounterpartyAccountId устанавливается ID второго счёта, к которому эта транзакция не относится.</p>
    ///     <p>Сумма перевода не должна превышать баланс fromAccount, быть положительным числом.</p>
    ///     <p>Валюта двух счетов должна быть одинаковой, иначе перевод не может быть осуществлен.</p>
    ///     <p>Валюта проверяется сервисом проверки валюты (placeholder позволяет задать ["USD", "EUR", "RUB", "KZT"]).</p>
    ///     <p>Описание - это простой текст, подробности о транзакциях (максимум 255 символов). Относится для обеих транзакций.</p>
    /// </remarks>
    /// <param name="command">Команда перевода между счетами.</param>
    /// <returns>ID созданных транзакций.</returns>
    /// <response code="200">Перевод успешно выполнен.</response>
    /// <response code="400">Неверный запрос.</response>
    /// <response code="401">Нет доступа к запрашиваемому ресурсу.</response>
    /// <response code="404">Счёт с таким ID не найден.</response>
    [HttpPost("transfer")]
    [ProducesResponseType(typeof(MbResult<IReadOnlyList<TransactionIdDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(MbResult<Unit>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(MbResult<Unit>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(MbResult<Unit>), StatusCodes.Status404NotFound)]
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
    /// <returns>Выписка по счёту за определенный период.</returns>
    /// <response code="200">Выписка получена.</response>
    /// <response code="400">Неверный запрос.</response>
    /// <response code="401">Нет доступа к запрашиваемому ресурсу.</response>
    /// <response code="404">Счёт с таким ID не найден.</response>
    [HttpGet("{accountId:guid}/transactions")]
    [ProducesResponseType(typeof(MbResult<AccountStatementDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(MbResult<Unit>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(MbResult<Unit>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(MbResult<Unit>), StatusCodes.Status404NotFound)]
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
    /// <response code="200">Осуществлена проверка наличия счетов у владельца.</response>
    /// <response code="400">Неверный запрос.</response>
    /// <response code="401">Нет доступа к запрашиваемому ресурсу.</response>
    /// <response code="404">Владелец с таким ID не найден.</response>
    [HttpGet("check-owner/{ownerId:guid}")]
    [ProducesResponseType(typeof(MbResult<CheckOwnerAccountsDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(MbResult<Unit>),StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(MbResult<Unit>),StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(MbResult<Unit>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CheckOwnerAccounts(Guid ownerId)
    {
        var query = new CheckOwnerAccountsQuery(ownerId);
        var response = await mediator.Send(query);
        return Ok(response);
    }
}
