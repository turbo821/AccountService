using AccountService.Features.Accounts.Abstractions;
using Dapper;
using System.Data;

namespace AccountService.Infrastructure.Services;

public class CurrencyValidator(IDbConnection dbConnection, ILogger logger) : ICurrencyValidator
{
    private readonly IDbConnection _dbConnection = dbConnection ?? throw new ArgumentNullException(nameof(dbConnection));

    public async Task<bool> IsExists(string currency)
    {
        if (string.IsNullOrWhiteSpace(currency))
            return false;

        var normalizedCurrency = currency.Trim().ToUpperInvariant();

        const string sql = "SELECT 1 FROM currencies WHERE code = @Currency AND is_active = true";

        try
        {
            return await _dbConnection.QueryFirstOrDefaultAsync<int?>(
                new CommandDefinition(
                    sql,
                    new { Currency = normalizedCurrency })) != null;
        }
        catch (Exception ex)
        {
            logger.LogError("Error validating currency: {ExMessage}", ex.Message);
            return false;
        }
    }
}