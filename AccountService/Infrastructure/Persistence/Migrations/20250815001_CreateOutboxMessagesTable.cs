using FluentMigrator;
using Microsoft.AspNetCore.Http.HttpResults;

namespace AccountService.Infrastructure.Persistence.Migrations;

[Migration(20250815001)]
public class CreateOutboxMessagesTable : Migration
{
    public override void Up()
    {
        Create.Table("outbox_messages")
            .WithColumn("id").AsGuid().PrimaryKey()
            .WithColumn("type").AsString(200).NotNullable()
            .WithColumn("payload").AsCustom("JSONB").NotNullable()
            .WithColumn("occurred_at").AsDateTime().NotNullable()
            .WithColumn("processed_at").AsDateTime().Nullable();
    }

    public override void Down()
    {
        Delete.Table("outbox_messages");
    }
}