namespace AccountService.Features.Accounts;

/// <summary>
/// Информация о банковском счёте.
/// </summary>
/// <param name="Id">ID счёта.</param>
/// <param name="OwnerId">ID владельца счёта.</param>
/// <param name="Type">Тип счёта (например, накопительный, расчётный).</param>
/// <param name="Currency">Валюта счёта.</param>
/// <param name="Balance">Текущий баланс счёта.</param>
/// <param name="InterestRate">Процентная ставка (если применимо).</param>
/// <param name="OpenedAt">Дата и время открытия счёта.</param>
public record AccountDto(
    Guid Id,
    Guid OwnerId,
    string Type,
    string Currency,
    decimal Balance,
    decimal? InterestRate,
    DateTime OpenedAt);