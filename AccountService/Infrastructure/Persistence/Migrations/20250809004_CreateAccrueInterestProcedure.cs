using FluentMigrator;

namespace AccountService.Infrastructure.Persistence.Migrations;

[Migration(20250809004, "Create accrue_interest stored procedure")]
public class CreateAccrueInterestProcedure : Migration
{
    public override void Up()
    {
        Execute.Sql(@"
            CREATE OR REPLACE FUNCTION accrue_interest(account_id UUID)
            RETURNS DECIMAL AS $$
            DECLARE
                current_balance DECIMAL;
                interest_rate DECIMAL;
                interest_amount DECIMAL;
                deposit_account RECORD;
            BEGIN
                SELECT * INTO deposit_account 
                FROM accounts 
                WHERE id = account_id AND type = 1 -- AccountType.Deposit
                AND closed_at IS NULL
                FOR UPDATE;
                
                IF deposit_account IS NULL THEN
                    RAISE EXCEPTION 'Deposit account not found or not active';
                END IF;
                
                current_balance := deposit_account.balance;
                interest_rate := deposit_account.interest_rate;
                
                interest_amount := current_balance * interest_rate / 365;
                
                UPDATE accounts 
                SET balance = balance + interest_amount 
                WHERE id = account_id;
                
                INSERT INTO transactions 
                    (id, account_id, amount, currency, type, description, timestamp)
                VALUES 
                    (gen_random_uuid(), 
                     account_id, 
                     interest_amount, 
                     deposit_account.currency, 
                     0, -- TransactionType.Debit
                     'Daily interest accrual', 
                     now());
                
                RETURN interest_amount;
            END;
            $$ LANGUAGE plpgsql;
        ");
    }

    public override void Down()
    {
        Execute.Sql("DROP FUNCTION IF EXISTS accrue_interest(UUID)");
    }
}