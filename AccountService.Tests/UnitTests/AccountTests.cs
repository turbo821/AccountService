using AccountService.Features.Accounts;

namespace AccountService.Tests.UnitTests;

public class AccountTests
{
    [Fact]
    public void ConductTransactionCredit_WithSufficientFunds_ShouldDecreaseBalance()
    {
        // Arrange
        var account = new Account
        {
            Balance = 100m,
            Currency = "USD",
            OwnerId = Guid.NewGuid(),
            Type = AccountType.Checking
        };

        var transaction = new Transaction
        {
            Type = TransactionType.Credit,
            Amount = 50m,
            Currency = "USD",
            Description = "1"
        };

        // Act
        account.ConductTransaction(transaction);
        
        // Assert
        Assert.Equal(50m, account.Balance);
        Assert.Single(account.Transactions);
        Assert.Equal(account.Id, transaction.AccountId);
    }

    [Fact]
    public void ConductTransaction_Credit_WithInsufficientFunds_ShouldThrow()
    {
        // Arrange
        var account = new Account
        {
            Balance = 10m,
            Currency = "USD",
            OwnerId = Guid.NewGuid(),
            Type = AccountType.Checking
        };

        var transaction = new Transaction
        {
            Type = TransactionType.Credit,
            Amount = 50m,
            Currency = "USD",
            Description = "1"
        };

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => account.ConductTransaction(transaction));
        Assert.Equal(10m, account.Balance);
        Assert.Empty(account.Transactions);
    }

    [Fact]
    public void ConductTransaction_Debit_ShouldIncreaseBalance()
    {
        // Arrange
        var account = new Account
        {
            Balance = 100m,
            Currency = "USD",
            OwnerId = Guid.NewGuid(),
            Type = AccountType.Checking
        };

        var transaction = new Transaction
        {
            Type = TransactionType.Debit,
            Amount = 20m,
            Currency = "USD",
            Description = "1"
        };

        // Act
        account.ConductTransaction(transaction);

        // Assert
        Assert.Equal(120m, account.Balance);
        Assert.Single(account.Transactions);
        Assert.Equal(account.Id, transaction.AccountId);
    }

    [Fact]
    public void ConductTransaction_InvalidTransactionType_ShouldThrow()
    {
        // Arrange
        var account = new Account
        {
            Balance = 100m,
            Currency = "USD",
            OwnerId = Guid.NewGuid(),
            Type = AccountType.Checking
        };

        var transaction = new Transaction
        {
            Type = (TransactionType)999,
            Amount = 10m,
            Currency = "USD",
            Description = "1"
        };

        // Act & Assert
        Assert.Throws<ArgumentException>(() => account.ConductTransaction(transaction));
        Assert.Equal(100m, account.Balance);
        Assert.Empty(account.Transactions);
    }

    [Fact]
    public void ConductTransaction_DifferentCurrency_ShouldThrow()
    {
        // Arrange
        var account = new Account
        {
            Balance = 100m,
            Currency = "USD",
            OwnerId = Guid.NewGuid(),
            Type = AccountType.Checking
        };

        var transaction = new Transaction
        {
            Type = TransactionType.Debit,
            Amount = 10m,
            Currency = "EUR",
            Description = "1"
        };

        // Act & Assert
        Assert.Throws<ArgumentException>(() => account.ConductTransaction(transaction));
        Assert.Equal(100m, account.Balance);
        Assert.Empty(account.Transactions);
    }
}