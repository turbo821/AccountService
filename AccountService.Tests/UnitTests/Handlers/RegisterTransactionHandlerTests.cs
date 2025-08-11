using AccountService.Features.Accounts;
using AccountService.Features.Accounts.Abstractions;
using AccountService.Features.Accounts.RegisterTransaction;
using AutoMapper;
using Moq;
using System.Data;
using Microsoft.Extensions.Logging;

namespace AccountService.Tests.UnitTests.Handlers;

public class RegisterTransactionHandlerTests
{
    private readonly IMapper _mapper;
    private readonly Mock<IAccountRepository> _repoMock = new();
    private readonly Mock<ICurrencyValidator> _currencyValidatorMock = new();

    public RegisterTransactionHandlerTests()
    {
        var config = new MapperConfiguration(cfg =>
        {
            cfg.AddProfile<MappingProfile>();
        }, new LoggerFactory());

        _mapper = config.CreateMapper();
    }

    private RegisterTransactionHandler CreateHandler()
        => new(_repoMock.Object, _mapper, _currencyValidatorMock.Object);

    [Fact]
    public async Task Handle_ShouldRegisterTransaction_WhenDataIsValid()
    {
        // Arrange
        var command = new RegisterTransactionCommand(
            AccountId: Guid.NewGuid(),
            Amount: 50m,
            Currency: "USD",
            Type: TransactionType.Debit,
            Description: "Test transaction"
        );

        var account = new Account
        {
            Id = command.AccountId,
            Currency = "USD",
            Balance = 100m,
            Transactions = []
        };

        var mockDbTransaction = new Mock<IDbTransaction>();
        mockDbTransaction.Setup(t => t.Commit());
        mockDbTransaction.Setup(t => t.Rollback());

        _currencyValidatorMock.Setup(v => v.IsExists(command.Currency)).ReturnsAsync(true);
        _repoMock.Setup(r => r.BeginTransaction()).ReturnsAsync(mockDbTransaction.Object);
        _repoMock.Setup(r => r.GetByIdForUpdateAsync(command.AccountId, mockDbTransaction.Object))
            .ReturnsAsync(account);
        _repoMock.Setup(r => r.UpdateBalanceAsync(account, mockDbTransaction.Object))
            .ReturnsAsync(1);
        _repoMock.Setup(r => r.AddTransactionAsync(It.IsAny<Transaction>(), mockDbTransaction.Object))
            .Returns(Task.CompletedTask);

        var handler = CreateHandler();

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.NotNull(result.Data);
        Assert.NotEqual(Guid.Empty, result.Data.TransactionId);

        _repoMock.Verify(r => r.BeginTransaction(), Times.Once);
        _repoMock.Verify(r => r.GetByIdForUpdateAsync(command.AccountId, mockDbTransaction.Object), Times.Once);
        _repoMock.Verify(r => r.UpdateBalanceAsync(account, mockDbTransaction.Object), Times.Once);
        _repoMock.Verify(r => r.AddTransactionAsync(It.Is<Transaction>(t =>
            t.AccountId == command.AccountId &&
            t.Amount == command.Amount &&
            t.Currency == command.Currency &&
            t.Type == command.Type &&
            t.Description == command.Description), mockDbTransaction.Object), Times.Once);
        mockDbTransaction.Verify(t => t.Commit(), Times.Once);
        mockDbTransaction.Verify(t => t.Rollback(), Times.Never);
    }

    [Fact]
    public async Task Handle_ShouldThrowArgumentException_WhenCurrencyIsUnsupported()
    {
        // Arrange
        var command = new RegisterTransactionCommand(
            AccountId: Guid.NewGuid(),
            Amount: 50m,
            Currency: "FAKE",
            Type: TransactionType.Debit,
            Description: "Test"
        );

        _currencyValidatorMock.Setup(v => v.IsExists(command.Currency)).ReturnsAsync(false);

        var handler = CreateHandler();

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => handler.Handle(command, CancellationToken.None));

        _repoMock.Verify(r => r.BeginTransaction(), Times.Never);
    }

    [Fact]
    public async Task Handle_ShouldThrowKeyNotFoundException_WhenAccountNotFound()
    {
        // Arrange
        var command = new RegisterTransactionCommand(
            AccountId: Guid.NewGuid(),
            Amount: 50m,
            Currency: "USD",
            Type: TransactionType.Debit,
            Description: "Test"
        );

        var mockDbTransaction = new Mock<IDbTransaction>();
        mockDbTransaction.Setup(t => t.Commit());
        mockDbTransaction.Setup(t => t.Rollback());

        _currencyValidatorMock.Setup(v => v.IsExists(command.Currency)).ReturnsAsync(true);
        _repoMock.Setup(r => r.BeginTransaction()).ReturnsAsync(mockDbTransaction.Object);
        _repoMock.Setup(r => r.GetByIdForUpdateAsync(command.AccountId, mockDbTransaction.Object))
            .ReturnsAsync((Account?)null);

        var handler = CreateHandler();

        // Act & Assert
        await Assert.ThrowsAsync<KeyNotFoundException>(() => handler.Handle(command, CancellationToken.None));

        mockDbTransaction.Verify(t => t.Rollback(), Times.Once);
    }

    [Fact]
    public async Task Handle_ShouldThrowDBConcurrencyException_WhenUpdateBalanceReturnsZero()
    {
        // Arrange
        var command = new RegisterTransactionCommand(
            AccountId: Guid.NewGuid(),
            Amount: 50m,
            Currency: "USD",
            Type: TransactionType.Debit,
            Description: "Test"
        );

        var account = new Account
        {
            Id = command.AccountId,
            Currency = "USD",
            Balance = 100m,
            Transactions = []
        };

        var mockDbTransaction = new Mock<IDbTransaction>();
        mockDbTransaction.Setup(t => t.Commit());
        mockDbTransaction.Setup(t => t.Rollback());

        _currencyValidatorMock.Setup(v => v.IsExists(command.Currency)).ReturnsAsync(true);
        _repoMock.Setup(r => r.BeginTransaction()).ReturnsAsync(mockDbTransaction.Object);
        _repoMock.Setup(r => r.GetByIdForUpdateAsync(command.AccountId, mockDbTransaction.Object))
            .ReturnsAsync(account);
        _repoMock.Setup(r => r.UpdateBalanceAsync(account, mockDbTransaction.Object))
            .ReturnsAsync(0);

        var handler = CreateHandler();

        // Act & Assert
        await Assert.ThrowsAsync<DBConcurrencyException>(() => handler.Handle(command, CancellationToken.None));

        mockDbTransaction.Verify(t => t.Rollback(), Times.Once);
        mockDbTransaction.Verify(t => t.Commit(), Times.Never);
    }
}