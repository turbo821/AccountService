namespace AccountService.Features.Accounts.GetAccountTransactions;

public class AccountTransactionsDto
{
    public Guid AccountId { get; set; }
    public Guid OwnerId { get; set; }
    public AccountType Type { get; set; }
    public decimal Balance { get; set; }
    public List<TransactionDto> Transactions { get; set; } = [];
}