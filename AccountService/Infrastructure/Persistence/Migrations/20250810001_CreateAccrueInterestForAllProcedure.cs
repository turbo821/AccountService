using FluentMigrator;

namespace AccountService.Infrastructure.Persistence.Migrations;

[Migration(20250810001, "Create accrue_interest_all stored procedure")]
public class CreateAccrueInterestForAllProcedure : Migration
{
    public override void Up()
    {
        Execute.Sql(@"
            CREATE OR REPLACE FUNCTION accrue_interest_all()
            RETURNS VOID AS $$
            DECLARE
                acc RECORD;
            BEGIN
                FOR acc IN
                    SELECT id
                    FROM accounts
                    WHERE type = 1 -- AccountType.Deposit
                      AND closed_at IS NULL
                    FOR UPDATE
                LOOP
                    PERFORM accrue_interest(acc.id);
                END LOOP;
            END;
            $$ LANGUAGE plpgsql;
        ");
    }

    public override void Down()
    {
        Execute.Sql("DROP FUNCTION IF EXISTS accrue_interest_all()");
    }
}