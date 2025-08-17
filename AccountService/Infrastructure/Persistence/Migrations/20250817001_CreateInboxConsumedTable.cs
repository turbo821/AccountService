using FluentMigrator;
using Microsoft.AspNetCore.Http.HttpResults;

namespace AccountService.Infrastructure.Persistence.Migrations;

[Migration(20250817001)]
public class CreateInboxConsumedTable : Migration
{
    public override void Up()
    {
        Create.Table("inbox_consumed")
            .WithColumn("message_id").AsGuid().PrimaryKey()
            .WithColumn("processed_at").AsDateTime().NotNullable()
            .WithColumn("handler").AsString(200).NotNullable();
    }

    public override void Down()
    {
        Delete.Table("inbox_consumed");
    }
}