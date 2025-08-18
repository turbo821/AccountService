using FluentMigrator;
using System.Diagnostics.Metrics;

namespace AccountService.Infrastructure.Persistence.Migrations;

[Migration(20250818002)]
public class AddIsDeadLetterToOutboxMessages : Migration
{
    public override void Up()
    {
        Alter.Table("outbox_messages")
            .AddColumn("is_dead_letter").AsBoolean().NotNullable().WithDefaultValue(false);
    }

    public override void Down()
    {
        Delete.Column("is_dead_letter").FromTable("outbox_messages");
    }
}