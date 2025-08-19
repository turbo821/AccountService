using FluentMigrator;
using Microsoft.AspNetCore.Http.HttpResults;

namespace AccountService.Infrastructure.Persistence.Migrations;

[Migration(20250818001)]
public class CreateInboxDeadLettersTable : Migration
{
    public override void Up()
    {
        Create.Table("inbox_dead_letters")
            .WithColumn("id").AsInt64().PrimaryKey().Identity()
            .WithColumn("message_id").AsGuid()
            .WithColumn("received_at").AsDateTime().NotNullable()
            .WithColumn("type").AsString(200).NotNullable()
            .WithColumn("payload").AsCustom("JSONB").NotNullable()
            .WithColumn("error").AsString(int.MaxValue).NotNullable();
    }

    public override void Down()
    {
        Delete.Table("inbox_dead_letters");
    }
}