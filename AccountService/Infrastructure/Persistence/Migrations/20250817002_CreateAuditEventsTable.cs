using FluentMigrator;
using Microsoft.AspNetCore.Http.HttpResults;

namespace AccountService.Infrastructure.Persistence.Migrations;

[Migration(20250817002)]
public class CreateAuditEventsTable : Migration
{
    public override void Up()
    {
        Create.Table("audit_events")
            .WithColumn("id").AsGuid().PrimaryKey()
            .WithColumn("type").AsString(200).NotNullable()
            .WithColumn("payload").AsCustom("JSONB").NotNullable()
            .WithColumn("occurred_at").AsDateTime().NotNullable();
    }

    public override void Down()
    {
        Delete.Table("audit_events");
    }
}