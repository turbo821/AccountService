using FluentMigrator;
using System.Data;

namespace AccountService.Infrastructure.Persistence.Migrations;

[Migration(20250809001, "Initial schema for Accounts and Transactions")]
public class InitialMigration : Migration
{
    public override void Up()
    {
        Execute.Sql("CREATE EXTENSION IF NOT EXISTS btree_gist");

        Create.Table("accounts")
            .WithColumn("id").AsGuid().PrimaryKey()
            .WithColumn("owner_id").AsGuid().NotNullable()
            .WithColumn("type").AsInt32().NotNullable()
            .WithColumn("currency").AsString(3).NotNullable()
            .WithColumn("balance").AsDecimal().NotNullable()
            .WithColumn("interest_rate").AsDecimal().Nullable()
            .WithColumn("opened_at").AsDateTime().NotNullable().WithDefaultValue(SystemMethods.CurrentUTCDateTime)
            .WithColumn("closed_at").AsDateTime().Nullable();

        Create.Table("transactions")
            .WithColumn("id").AsGuid().PrimaryKey()
            .WithColumn("account_id").AsGuid().NotNullable()
            .WithColumn("counterparty_account_id").AsGuid().Nullable()
            .WithColumn("amount").AsDecimal().NotNullable()
            .WithColumn("currency").AsString(3).NotNullable()
            .WithColumn("type").AsInt32().NotNullable()
            .WithColumn("description").AsString(255).NotNullable()
            .WithColumn("timestamp").AsDateTime().NotNullable().WithDefaultValue(SystemMethods.CurrentUTCDateTime);

        Create.ForeignKey("fk_transactions_accounts")
            .FromTable("transactions").ForeignColumn("account_id")
            .ToTable("accounts").PrimaryColumn("id")
            .OnDelete(Rule.Cascade);

        Create.ForeignKey("fk_transactions_counterparty")
            .FromTable("transactions").ForeignColumn("counterparty_account_id")
            .ToTable("accounts").PrimaryColumn("id");

        Execute.Sql("CREATE INDEX idx_accounts_owner_id_hash ON accounts USING hash (owner_id);");

        Execute.Sql("CREATE INDEX idx_transactions_account_id_timestamp ON transactions (account_id, timestamp);");
        Execute.Sql("CREATE INDEX idx_transactions_timestamp_gist ON transactions USING gist (timestamp);");
    }

    public override void Down()
    {
        Delete.Index("idx_accounts_owner_id_hash").OnTable("accounts");
        Delete.Index("idx_transactions_account_id_timestamp").OnTable("transactions");
        Delete.Index("idx_transactions_timestamp_gist").OnTable("transactions");
        Delete.Index("ix_accounts_owner_id_btree").OnTable("accounts");

        Delete.ForeignKey("fk_transactions_counterparty").OnTable("transactions");
        Delete.ForeignKey("fk_transactions_accounts").OnTable("transactions");

        Delete.Table("transactions");
        Delete.Table("accounts");
    }
}