using AccountService.Features.Accounts;
using AccountService.Features.Accounts.Abstractions;
using AccountService.Features.Accounts.TransferBetweenAccounts;
using AutoMapper;
using Microsoft.Extensions.Logging;
using Moq;
using System.Data;
using System.Data.Common;

namespace AccountService.Tests.UnitTests.Handlers;

public class TransferBetweenAccountsHandlerTests
{
    private readonly IMapper _mapper;
    private readonly Mock<IAccountRepository> _repoMock = new();
    private readonly Mock<ICurrencyValidator> _currencyValidatorMock = new();
    private readonly Mock<DbTransaction> _mockDbTransaction = new();

    public TransferBetweenAccountsHandlerTests()
    {
        var config = new MapperConfiguration(cfg =>
        {
            cfg.AddProfile<MappingProfile>();
        }, new LoggerFactory());

        _mapper = config.CreateMapper();
    }

    private TransferBetweenAccountsHandler CreateHandler()
        => new(_repoMock.Object, _mapper, _currencyValidatorMock.Object);

    [Fact]
    public async Task Handle_ShouldTransferFunds_WhenDataIsValid()
    {
        // Arrange
        var fromAccountId = Guid.NewGuid();
        var toAccountId = Guid.NewGuid();
        const string currency = "USD";
        const decimal amount = 50m;

        var fromAccount = new Account
        {
            Id = fromAccountId,
            Currency = currency,
            Balance = 200m,
            Transactions = []
        };

        var toAccount = new Account
        {
            Id = toAccountId,
            Currency = currency,
            Balance = 100m,
            Transactions = []
        };

        _mockDbTransaction.Setup(t => t.CommitAsync(CancellationToken.None));
        _mockDbTransaction.Setup(t => t.RollbackAsync(CancellationToken.None));

        var command = new TransferBetweenAccountsCommand(
            FromAccountId: fromAccountId,
            ToAccountId: toAccountId,
            Amount: amount,
            Currency: currency,
            Description: "Test transfer"
        );

        _currencyValidatorMock.Setup(v => v.IsExists(currency)).ReturnsAsync(true);
        _repoMock.Setup(r => r.BeginTransactionAsync(IsolationLevel.Serializable)).ReturnsAsync(_mockDbTransaction.Object);
        _repoMock.Setup(r => r.GetByIdForUpdateAsync(fromAccountId, _mockDbTransaction.Object))
            .ReturnsAsync(fromAccount);
        _repoMock.Setup(r => r.GetByIdForUpdateAsync(toAccountId, _mockDbTransaction.Object))
            .ReturnsAsync(toAccount);
        _repoMock.Setup(r => r.AddTransactionAsync(It.IsAny<Transaction>(), _mockDbTransaction.Object))
            .Returns(Task.CompletedTask);
        _repoMock.Setup(r => r.UpdateBalanceAsync(fromAccount, _mockDbTransaction.Object))
            .ReturnsAsync(1);
        _repoMock.Setup(r => r.UpdateBalanceAsync(toAccount, _mockDbTransaction.Object))
            .ReturnsAsync(1);

        var handler = CreateHandler();

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.NotNull(result.Data);
        Assert.Equal(2, result.Data.Count);

        Assert.Equal(150m, fromAccount.Balance);
        Assert.Equal(150m, toAccount.Balance);

        _repoMock.Verify(r => r.BeginTransactionAsync(IsolationLevel.Serializable), Times.Once);
        _repoMock.Verify(r => r.GetByIdForUpdateAsync(fromAccountId, _mockDbTransaction.Object), Times.Once);
        _repoMock.Verify(r => r.GetByIdForUpdateAsync(toAccountId, _mockDbTransaction.Object), Times.Once);
        _repoMock.Verify(r => r.AddTransactionAsync(It.IsAny<Transaction>(), _mockDbTransaction.Object), Times.Exactly(2));
        _repoMock.Verify(r => r.UpdateBalanceAsync(fromAccount, _mockDbTransaction.Object), Times.Once);
        _repoMock.Verify(r => r.UpdateBalanceAsync(toAccount, _mockDbTransaction.Object), Times.Once);
        _mockDbTransaction.Verify(t => t.CommitAsync(CancellationToken.None), Times.Once);
        _mockDbTransaction.Verify(t => t.RollbackAsync(CancellationToken.None), Times.Never);
    }

    [Fact]
    public async Task Handle_ShouldThrowArgumentException_WhenCurrencyUnsupported()
    {
        // Arrange
        var command = new TransferBetweenAccountsCommand(
            FromAccountId: Guid.NewGuid(),
            ToAccountId: Guid.NewGuid(),
            Amount: 10m,
            Currency: "FAKE",
            Description: "Test"
        );

        _currencyValidatorMock.Setup(v => v.IsExists(command.Currency)).ReturnsAsync(false);
        var handler = CreateHandler();

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => handler.Handle(command, CancellationToken.None));
        _repoMock.Verify(r => r.BeginTransactionAsync(IsolationLevel.Serializable), Times.Never);
    }

    [Fact]
    public async Task Handle_ShouldThrowKeyNotFoundException_WhenFromAccountNotFound()
    {
        // Arrange
        var command = new TransferBetweenAccountsCommand(
            FromAccountId: Guid.NewGuid(),
            ToAccountId: Guid.NewGuid(),
            Amount: 10m,
            Currency: "USD",
            Description: "Test"
        );

        _mockDbTransaction.Setup(t => t.CommitAsync(CancellationToken.None));
        _mockDbTransaction.Setup(t => t.RollbackAsync(CancellationToken.None));

        _currencyValidatorMock.Setup(v => v.IsExists(command.Currency)).ReturnsAsync(true);
        _repoMock.Setup(r => r.BeginTransactionAsync(IsolationLevel.Serializable)).ReturnsAsync(_mockDbTransaction.Object);
        _repoMock.Setup(r => r.GetByIdForUpdateAsync(command.FromAccountId, _mockDbTransaction.Object)).ReturnsAsync((Account?)null);
        _repoMock.Setup(r => r.GetByIdForUpdateAsync(command.ToAccountId, _mockDbTransaction.Object)).ReturnsAsync(new Account
        {
            Id = command.ToAccountId,
            Currency = "USD",
            Balance = 100m
        });

        var handler = CreateHandler();

        // Act & Assert
        await Assert.ThrowsAsync<KeyNotFoundException>(() => handler.Handle(command, CancellationToken.None));
        _mockDbTransaction.Verify(t => t.RollbackAsync(CancellationToken.None), Times.Once);
    }

    [Fact]
    public async Task Handle_ShouldThrowKeyNotFoundException_WhenToAccountNotFound()
    {
        // Arrange
        var command = new TransferBetweenAccountsCommand(
            FromAccountId: Guid.NewGuid(),
            ToAccountId: Guid.NewGuid(),
            Amount: 10m,
            Currency: "USD",
            Description: "Test"
        );

        _mockDbTransaction.Setup(t => t.CommitAsync(CancellationToken.None));
        _mockDbTransaction.Setup(t => t.RollbackAsync(CancellationToken.None));

        _currencyValidatorMock.Setup(v => v.IsExists(command.Currency)).ReturnsAsync(true);
        _repoMock.Setup(r => r.BeginTransactionAsync(IsolationLevel.Serializable)).ReturnsAsync(_mockDbTransaction.Object);
        _repoMock.Setup(r => r.GetByIdForUpdateAsync(command.FromAccountId, _mockDbTransaction.Object)).ReturnsAsync(new Account
        {
            Id = command.FromAccountId,
            Currency = "USD",
            Balance = 100m
        });
        _repoMock.Setup(r => r.GetByIdForUpdateAsync(command.ToAccountId, _mockDbTransaction.Object)).ReturnsAsync((Account?)null);

        var handler = CreateHandler();

        // Act & Assert
        await Assert.ThrowsAsync<KeyNotFoundException>(() => handler.Handle(command, CancellationToken.None));
        _mockDbTransaction.Verify(t => t.RollbackAsync(CancellationToken.None), Times.Once);
    }

    [Fact]
    public async Task Handle_ShouldThrowArgumentException_WhenCurrenciesDiffer()
    {
        // Arrange
        var command = new TransferBetweenAccountsCommand(
            FromAccountId: Guid.NewGuid(),
            ToAccountId: Guid.NewGuid(),
            Amount: 10m,
            Currency: "USD",
            Description: "Test"
        );

        _mockDbTransaction.Setup(t => t.CommitAsync(CancellationToken.None));
        _mockDbTransaction.Setup(t => t.RollbackAsync(CancellationToken.None));

        _currencyValidatorMock.Setup(v => v.IsExists(command.Currency)).ReturnsAsync(true);
        _repoMock.Setup(r => r.BeginTransactionAsync(IsolationLevel.Serializable)).ReturnsAsync(_mockDbTransaction.Object);

        _repoMock.Setup(r => r.GetByIdForUpdateAsync(command.FromAccountId, _mockDbTransaction.Object)).ReturnsAsync(new Account
        {
            Id = command.FromAccountId,
            Currency = "USD",
            Balance = 100m
        });

        _repoMock.Setup(r => r.GetByIdForUpdateAsync(command.ToAccountId, _mockDbTransaction.Object)).ReturnsAsync(new Account
        {
            Id = command.ToAccountId,
            Currency = "EUR",
            Balance = 100m
        });

        var handler = CreateHandler();

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => handler.Handle(command, CancellationToken.None));
        _mockDbTransaction.Verify(t => t.RollbackAsync(CancellationToken.None), Times.Once);
    }

    [Fact]
    public async Task Handle_ShouldThrowDBConcurrencyException_WhenUpdateBalanceReturnsZero()
    {
        // Arrange
        var fromAccountId = Guid.NewGuid();
        var toAccountId = Guid.NewGuid();
        const string currency = "USD";
        const decimal amount = 50m;

        var fromAccount = new Account
        {
            Id = fromAccountId,
            Currency = currency,
            Balance = 200m,
            Transactions = []
        };

        var toAccount = new Account
        {
            Id = toAccountId,
            Currency = currency,
            Balance = 100m,
            Transactions = []
        };

        _mockDbTransaction.Setup(t => t.CommitAsync(CancellationToken.None));
        _mockDbTransaction.Setup(t => t.RollbackAsync(CancellationToken.None));

        var command = new TransferBetweenAccountsCommand(
            FromAccountId: fromAccountId,
            ToAccountId: toAccountId,
            Amount: amount,
            Currency: currency,
            Description: "Test transfer"
        );

        _currencyValidatorMock.Setup(v => v.IsExists(currency)).ReturnsAsync(true);
        _repoMock.Setup(r => r.BeginTransactionAsync(IsolationLevel.Serializable)).ReturnsAsync(_mockDbTransaction.Object);
        _repoMock.Setup(r => r.GetByIdForUpdateAsync(fromAccountId, _mockDbTransaction.Object)).ReturnsAsync(fromAccount);
        _repoMock.Setup(r => r.GetByIdForUpdateAsync(toAccountId, _mockDbTransaction.Object)).ReturnsAsync(toAccount);
        _repoMock.Setup(r => r.AddTransactionAsync(It.IsAny<Transaction>(), _mockDbTransaction.Object)).Returns(Task.CompletedTask);
        _repoMock.Setup(r => r.UpdateBalanceAsync(fromAccount, _mockDbTransaction.Object)).ReturnsAsync(0);
        _repoMock.Setup(r => r.UpdateBalanceAsync(toAccount, _mockDbTransaction.Object)).ReturnsAsync(1);

        var handler = CreateHandler();

        // Act & Assert
        await Assert.ThrowsAsync<DBConcurrencyException>(() => handler.Handle(command, CancellationToken.None));
        _mockDbTransaction.Verify(t => t.RollbackAsync(CancellationToken.None), Times.Once);
        _mockDbTransaction.Verify(t => t.CommitAsync(CancellationToken.None), Times.Never);
    }

    [Fact]
    public async Task Handle_ShouldThrowInvalidOperationException_WhenFinalBalancesMismatch()
    {
        // Arrange
        var fromAccountId = Guid.NewGuid();
        var toAccountId = Guid.NewGuid();
        const string currency = "USD";
        const decimal amount = 50m;

        var fromAccount = new Account
        {
            Id = fromAccountId,
            Currency = currency,
            Balance = 200m,
            Transactions = []
        };

        var toAccount = new Account
        {
            Id = toAccountId,
            Currency = currency,
            Balance = 100m,
            Transactions = []
        };

        _mockDbTransaction.Setup(t => t.CommitAsync(CancellationToken.None));
        _mockDbTransaction.Setup(t => t.RollbackAsync(CancellationToken.None));

        var command = new TransferBetweenAccountsCommand(
            FromAccountId: fromAccountId,
            ToAccountId: toAccountId,
            Amount: amount,
            Currency: currency,
            Description: "Test transfer"
        );

        _currencyValidatorMock.Setup(v => v.IsExists(currency)).ReturnsAsync(true);
        _repoMock.Setup(r => r.BeginTransactionAsync(IsolationLevel.Serializable)).ReturnsAsync(_mockDbTransaction.Object);
        _repoMock.Setup(r => r.GetByIdForUpdateAsync(fromAccountId, _mockDbTransaction.Object)).ReturnsAsync(fromAccount);
        _repoMock.Setup(r => r.GetByIdForUpdateAsync(toAccountId, _mockDbTransaction.Object)).ReturnsAsync(toAccount);
        _repoMock.Setup(r => r.AddTransactionAsync(It.IsAny<Transaction>(), _mockDbTransaction.Object)).Returns(Task.CompletedTask);
        
        // changing balances that total balance don't end up the same
        _repoMock.Setup(r => r.UpdateBalanceAsync(fromAccount, _mockDbTransaction.Object))
            .Callback(() => fromAccount.Balance += 10)
            .ReturnsAsync(1);
        _repoMock.Setup(r => r.UpdateBalanceAsync(toAccount, _mockDbTransaction.Object))
            .Callback(() => toAccount.Balance += 20)
            .ReturnsAsync(1);

        var handler = CreateHandler();

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => handler.Handle(command, CancellationToken.None));
        _mockDbTransaction.Verify(t => t.RollbackAsync(CancellationToken.None), Times.Once);
        _mockDbTransaction.Verify(t => t.CommitAsync(CancellationToken.None), Times.Never);
    }
}