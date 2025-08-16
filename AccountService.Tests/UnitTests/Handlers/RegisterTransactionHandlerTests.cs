using AccountService.Features.Accounts;
using AccountService.Features.Accounts.Abstractions;
using AccountService.Features.Accounts.RegisterTransaction;
using AutoMapper;
using Microsoft.Extensions.Logging;
using Moq;
using System.Data;
using System.Data.Common;
using AccountService.Application.Abstractions;

namespace AccountService.Tests.UnitTests.Handlers;

public class RegisterTransactionHandlerTests
{
    private readonly IMapper _mapper;
    private readonly Mock<IAccountRepository> _repoMock = new();
    private readonly Mock<IOutboxRepository> _outboxRepoMock = new();
    private readonly Mock<ICurrencyValidator> _currencyValidatorMock = new();
    private readonly Mock<DbTransaction> _mockDbTransaction = new();

    public RegisterTransactionHandlerTests()
    {
        var config = new MapperConfiguration(cfg =>
        {
            cfg.AddProfile<MappingProfile>();
        }, new LoggerFactory());

        _mapper = config.CreateMapper();
    }

    private RegisterTransactionHandler CreateHandler()
        => new(_repoMock.Object, _outboxRepoMock.Object, _mapper, _currencyValidatorMock.Object);

    [Fact]
    public async Task Handle_ShouldRegisterTransaction_WhenDataIsValid()
    {
        // Arrange
        var command = new RegisterTransactionCommand(
            AccountId: Guid.NewGuid(),
            Amount: 50m,
            Currency: "USD",
            Type: TransactionType.Credit,
            Description: "Test transaction"
        );

        var account = new Account
        {
            Id = command.AccountId,
            Currency = "USD",
            Balance = 100m,
            Transactions = []
        };

        _mockDbTransaction.Setup(t => t.CommitAsync(CancellationToken.None));
        _mockDbTransaction.Setup(t => t.RollbackAsync(CancellationToken.None));

        _currencyValidatorMock.Setup(v => v.IsExists(command.Currency)).ReturnsAsync(true);
        _repoMock.Setup(r => r.BeginTransactionAsync(IsolationLevel.Serializable)).ReturnsAsync(_mockDbTransaction.Object);
        _repoMock.Setup(r => r.GetByIdForUpdateAsync(command.AccountId, _mockDbTransaction.Object))
            .ReturnsAsync(account);
        _repoMock.Setup(r => r.UpdateBalanceAsync(account, _mockDbTransaction.Object))
            .ReturnsAsync(1);
        _repoMock.Setup(r => r.AddTransactionAsync(It.IsAny<Transaction>(), _mockDbTransaction.Object))
            .Returns(Task.CompletedTask);

        var handler = CreateHandler();

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.NotNull(result.Data);
        Assert.NotEqual(Guid.Empty, result.Data.TransactionId);

        _repoMock.Verify(r => r.BeginTransactionAsync(IsolationLevel.Serializable), Times.Once);
        _repoMock.Verify(r => r.GetByIdForUpdateAsync(command.AccountId, _mockDbTransaction.Object), Times.Once);
        _repoMock.Verify(r => r.UpdateBalanceAsync(account, _mockDbTransaction.Object), Times.Once);
        _repoMock.Verify(r => r.AddTransactionAsync(It.Is<Transaction>(t =>
            t.AccountId == command.AccountId &&
            t.Amount == command.Amount &&
            t.Currency == command.Currency &&
            t.Type == command.Type &&
            t.Description == command.Description), _mockDbTransaction.Object), Times.Once);
        _mockDbTransaction.Verify(t => t.CommitAsync(CancellationToken.None), Times.Once);
        _mockDbTransaction.Verify(t => t.RollbackAsync(CancellationToken.None), Times.Never);
    }

    [Fact]
    public async Task Handle_ShouldThrowArgumentException_WhenCurrencyIsUnsupported()
    {
        // Arrange
        var command = new RegisterTransactionCommand(
            AccountId: Guid.NewGuid(),
            Amount: 50m,
            Currency: "FAKE",
            Type: TransactionType.Credit,
            Description: "Test"
        );

        _currencyValidatorMock.Setup(v => v.IsExists(command.Currency)).ReturnsAsync(false);

        var handler = CreateHandler();

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => handler.Handle(command, CancellationToken.None));

        _repoMock.Verify(r => r.BeginTransactionAsync(IsolationLevel.Serializable), Times.Never);
    }

    [Fact]
    public async Task Handle_ShouldThrowKeyNotFoundException_WhenAccountNotFound()
    {
        // Arrange
        var command = new RegisterTransactionCommand(
            AccountId: Guid.NewGuid(),
            Amount: 50m,
            Currency: "USD",
            Type: TransactionType.Credit,
            Description: "Test"
        );

        _mockDbTransaction.Setup(t => t.CommitAsync(CancellationToken.None));
        _mockDbTransaction.Setup(t => t.RollbackAsync(CancellationToken.None));

        _currencyValidatorMock.Setup(v => v.IsExists(command.Currency)).ReturnsAsync(true);
        _repoMock.Setup(r => r.BeginTransactionAsync(IsolationLevel.Serializable)).ReturnsAsync(_mockDbTransaction.Object);
        _repoMock.Setup(r => r.GetByIdForUpdateAsync(command.AccountId, _mockDbTransaction.Object))
            .ReturnsAsync((Account?)null);

        var handler = CreateHandler();

        // Act & Assert
        await Assert.ThrowsAsync<KeyNotFoundException>(() => handler.Handle(command, CancellationToken.None));

        _mockDbTransaction.Verify(t => t.RollbackAsync(CancellationToken.None), Times.Once);
    }

    [Fact]
    public async Task Handle_ShouldThrowDBConcurrencyException_WhenUpdateBalanceReturnsZero()
    {
        // Arrange
        var command = new RegisterTransactionCommand(
            AccountId: Guid.NewGuid(),
            Amount: 50m,
            Currency: "USD",
            Type: TransactionType.Credit,
            Description: "Test"
        );

        var account = new Account
        {
            Id = command.AccountId,
            Currency = "USD",
            Balance = 100m,
            Transactions = []
        };

        _mockDbTransaction.Setup(t => t.CommitAsync(CancellationToken.None));
        _mockDbTransaction.Setup(t => t.RollbackAsync(CancellationToken.None));

        _currencyValidatorMock.Setup(v => v.IsExists(command.Currency)).ReturnsAsync(true);
        _repoMock.Setup(r => r.BeginTransactionAsync(IsolationLevel.Serializable)).ReturnsAsync(_mockDbTransaction.Object);
        _repoMock.Setup(r => r.GetByIdForUpdateAsync(command.AccountId, _mockDbTransaction.Object))
            .ReturnsAsync(account);
        _repoMock.Setup(r => r.UpdateBalanceAsync(account, _mockDbTransaction.Object))
            .ReturnsAsync(0);

        var handler = CreateHandler();

        // Act & Assert
        await Assert.ThrowsAsync<DBConcurrencyException>(() => handler.Handle(command, CancellationToken.None));

        _mockDbTransaction.Verify(t => t.CommitAsync(It.IsAny<CancellationToken>()), Times.Never);
        _mockDbTransaction.Verify(t => t.RollbackAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}