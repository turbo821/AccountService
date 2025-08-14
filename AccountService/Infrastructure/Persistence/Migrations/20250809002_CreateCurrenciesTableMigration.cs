using FluentMigrator;

namespace AccountService.Infrastructure.Persistence.Migrations;

[Migration(20250809002, "Create currencies table")]
public class CreateCurrenciesTableMigration : Migration
{
    public override void Up()
    {
        Create.Table("currencies")
            .WithColumn("code").AsString(3).PrimaryKey() // ISO code (USD, EUR, etc)
            .WithColumn("name").AsString(50).NotNullable()
            .WithColumn("symbol").AsString(5).Nullable()
            .WithColumn("is_active").AsBoolean().NotNullable().WithDefaultValue(true);

        Execute.Sql(@"
            INSERT INTO currencies (code, name, symbol) VALUES
            ('USD', 'US Dollar', '$'),
            ('EUR', 'Euro', '€'),
            ('RUB', 'Russian Ruble', '₽'),
            ('KZT', 'Kazakhstani Tenge', '₸')");
    }

    public override void Down()
    {
        Delete.Table("currencies");
    }
}