using AccountService.Features.Accounts;
using FluentMigrator;
using System.Collections.Generic;

namespace AccountService.Infrastructure.Persistence.Migrations;

[Migration(20250809003, "Seed initial data")]
public class SeedInitialDataMigration : Migration
{
    public override void Up()
    {
        var accounts = new List<Account>
        {
            new()
            {
                Id = Guid.Parse("11111111-1111-1111-1111-111111111111"),
                OwnerId = Guid.Parse("22222222-2222-2222-2222-222222222222"),
                Type = AccountType.Checking,
                Currency = "USD",
                Balance = 1000m,
                OpenedAt = new DateTime(2025, 2, 20)
            },
            new()
            {
                Id = Guid.Parse("33333333-3333-3333-3333-333333333333"),
                OwnerId = Guid.Parse("44444444-4444-4444-4444-444444444444"),
                Type = AccountType.Deposit,
                Currency = "RUB",
                Balance = 10000m,
                InterestRate = 14,
                OpenedAt = DateTime.UtcNow
            },
            new()
            {
                Id = Guid.Parse("55555555-5555-5555-5555-555555555555"),
                OwnerId = Guid.Parse("66666666-6666-6666-6666-666666666666"),
                Type = AccountType.Credit,
                Currency = "RUB",
                Balance = 10000m,
                InterestRate = 16,
                OpenedAt = new DateTime(2021, 10, 5)
            }
        };

        foreach (var account in accounts)
        {
            Insert.IntoTable("accounts")
                .Row(new
                {
                    id = account.Id,          // было Id
                    owner_id = account.OwnerId, // было OwnerId
                    type = (int)account.Type,
                    currency = account.Currency,
                    balance = account.Balance,
                    interest_rate = account.InterestRate,
                    opened_at = account.OpenedAt,
                    closed_at = (DateTime?)null
                });
        }

        var transactions = new List<Transaction>
        {
            new()
            {
                Id = Guid.Parse("77777777-7777-7777-7777-777777777777"),
                AccountId = Guid.Parse("11111111-1111-1111-1111-111111111111"),
                Amount = 100m,
                Currency = "USD",
                Type = TransactionType.Debit,
                Description = "Initial deposit",
                Timestamp = new DateTime(2025, 4, 22)
            },
            new()
            {
                Id = Guid.Parse("88888888-8888-8888-8888-888888888888"),
                AccountId = Guid.Parse("33333333-3333-3333-3333-333333333333"),
                Amount = 50m,
                Currency = "RUB",
                Type = TransactionType.Credit,
                Description = "Initial credit",
                Timestamp = DateTime.UtcNow
            },
            new()
            {
                Id = Guid.Parse("99999999-9999-9999-9999-999999999999"),
                AccountId = Guid.Parse("55555555-5555-5555-5555-555555555555"),
                Amount = 500m,
                Currency = "RUB",
                Type = TransactionType.Debit,
                Description = "Money transaction",
                Timestamp = DateTime.UtcNow.AddHours(-2)
            }
        };

        foreach (var transaction in transactions)
        {
            Insert.IntoTable("transactions")
                .Row(new
                {
                    id = transaction.Id,
                    account_id = transaction.AccountId,
                    counterparty_account_id = transaction.CounterpartyAccountId,
                    amount = transaction.Amount,
                    currency = transaction.Currency,
                    type = (int)transaction.Type,
                    description = transaction.Description,
                    timestamp = transaction.Timestamp
                });

            if (transaction.Type == TransactionType.Debit)
            {
                Execute.Sql($@"
                    UPDATE accounts 
                    SET balance = balance + {transaction.Amount} 
                    WHERE id = '{transaction.AccountId}'");
            }
            else
            {
                Execute.Sql($@"
                    UPDATE accounts 
                    SET balance = balance - {transaction.Amount} 
                    WHERE id = '{transaction.AccountId}'");
            }
        }
    }

    public override void Down()
    {
        Delete.FromTable("transactions")
            .Row(new { id = Guid.Parse("77777777-7777-7777-7777-777777777777") })
            .Row(new { id = Guid.Parse("88888888-8888-8888-8888-888888888888") })
            .Row(new { id = Guid.Parse("99999999-9999-9999-9999-999999999999") });

        Delete.FromTable("accounts")
            .Row(new { id = Guid.Parse("11111111-1111-1111-1111-111111111111") })
            .Row(new { id = Guid.Parse("33333333-3333-3333-3333-333333333333") })
            .Row(new { id = Guid.Parse("55555555-5555-5555-5555-555555555555") });

    }
}