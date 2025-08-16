using AccountService.Application.Abstractions;
using AccountService.Features.Accounts;
using AccountService.Features.Accounts.Abstractions;
using AccountService.Features.Accounts.UpdateAccount;
using AutoMapper;
using Microsoft.Extensions.Logging;
using Moq;
using System.Data;
using System.Data.Common;

namespace AccountService.Tests.UnitTests.Handlers;

public class UpdateAccountHandlerTests
{
    private readonly IMapper _mapper;
    private readonly Mock<IAccountRepository> _repoMock = new();
    private readonly Mock<IOutboxRepository> _outboxRepoMock = new();
    private readonly Mock<ICurrencyValidator> _currencyValidatorMock = new();
    private readonly Mock<IOwnerVerificator> _ownerVerificatorMock = new();
    private readonly Mock<DbTransaction> _mockDbTransaction = new();

    public UpdateAccountHandlerTests()
    {
        var config = new MapperConfiguration(cfg => {
            cfg.AddProfile<MappingProfile>();
        }, new LoggerFactory());

        _mapper = config.CreateMapper();
    }

    private UpdateAccountHandler CreateHandler() =>
        new(_mapper, _repoMock.Object, _outboxRepoMock.Object, _currencyValidatorMock.Object, _ownerVerificatorMock.Object);

    [Fact]
    public async Task Handle_ShouldUpdateAccount_WhenDataIsValid()
    {
        // Arrange
        var accountId = Guid.NewGuid();
        var ownerId = Guid.NewGuid();

        var existingAccount = new Account
        {
            Id = accountId,
            OwnerId = ownerId,
            Type = AccountType.Checking,
            Currency = "USD",
            Balance = 100,
            Version = 1,
            OpenedAt = DateTime.UtcNow.AddDays(-10)
        };

        var command = new UpdateAccountCommand(
            accountId: accountId,
            ownerId: ownerId,
            type: AccountType.Checking,
            currency: "USD",
            balance: 150,
            interestRate: 15,
            openedAt: null
        );

        _mockDbTransaction.Setup(t => t.CommitAsync(CancellationToken.None));
        _mockDbTransaction.Setup(t => t.RollbackAsync(CancellationToken.None));

        _repoMock.Setup(r => r.BeginTransactionAsync(IsolationLevel.Unspecified)).ReturnsAsync(_mockDbTransaction.Object);
        _repoMock.Setup(r => r.GetByIdAsync(accountId, _mockDbTransaction.Object)).ReturnsAsync(existingAccount);
        _ownerVerificatorMock.Setup(v => v.IsExists(ownerId)).Returns(true);
        _currencyValidatorMock.Setup(v => v.IsExists(command.Currency)).ReturnsAsync(true);
        _repoMock.Setup(r => r.UpdateAsync(It.IsAny<Account>(), _mockDbTransaction.Object)).ReturnsAsync(1);

        var handler = CreateHandler();

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        _repoMock.Verify(r => r.UpdateAsync(It.Is<Account>(a =>
            a.Id == accountId &&
            a.OwnerId == ownerId &&
            a.Type == AccountType.Checking &&
            a.Currency == "USD" &&
            a.Balance == 150 &&
            a.OpenedAt == existingAccount.OpenedAt &&
            a.Version == existingAccount.Version
        ), _mockDbTransaction.Object), Times.Once);

        Assert.NotNull(result);
        Assert.True(result.Success);
        _mockDbTransaction.Verify(t => t.CommitAsync(CancellationToken.None), Times.Once);
        _mockDbTransaction.Verify(t => t.RollbackAsync(CancellationToken.None), Times.Never);
    }

    [Fact]
    public async Task Handle_ShouldThrowKeyNotFoundException_WhenAccountNotFound()
    {
        // Arrange
        var accountId = Guid.NewGuid();

        _mockDbTransaction.Setup(t => t.CommitAsync(CancellationToken.None));
        _mockDbTransaction.Setup(t => t.RollbackAsync(CancellationToken.None));

        _currencyValidatorMock.Setup(v => v.IsExists(It.IsAny<string>())).ReturnsAsync(true);
        _repoMock.Setup(r => r.BeginTransactionAsync(IsolationLevel.Unspecified)).ReturnsAsync(_mockDbTransaction.Object);
        _repoMock.Setup(r => r.GetByIdAsync(accountId, _mockDbTransaction.Object)).ReturnsAsync((Account?)null);

        var command = new UpdateAccountCommand(
            accountId,
            Guid.NewGuid(),
            AccountType.Checking,
            "USD",
            100,
            null,
            null);

        var handler = CreateHandler();

        // Act & Assert
        var ex = await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            handler.Handle(command, CancellationToken.None));

        Assert.Contains(accountId.ToString(), ex.Message);
        _mockDbTransaction.Verify(t => t.CommitAsync(CancellationToken.None), Times.Never);
        _mockDbTransaction.Verify(t => t.RollbackAsync(CancellationToken.None), Times.Once);
    }

    [Fact]
    public async Task Handle_ShouldThrowArgumentException_WhenOwnerNotExists()
    {
        // Arrange
        var accountId = Guid.NewGuid();
        var newOwnerId = Guid.NewGuid();
        var existingAccount = new Account { Id = accountId, OwnerId = Guid.NewGuid(), Currency = "USD", Version = 1 };

        _mockDbTransaction.Setup(t => t.CommitAsync(CancellationToken.None));
        _mockDbTransaction.Setup(t => t.RollbackAsync(CancellationToken.None));

        _repoMock.Setup(r => r.BeginTransactionAsync(IsolationLevel.Unspecified)).ReturnsAsync(_mockDbTransaction.Object);
        _currencyValidatorMock.Setup(v => v.IsExists(It.IsAny<string>())).ReturnsAsync(true);
        _repoMock.Setup(r => r.GetByIdAsync(accountId, _mockDbTransaction.Object)).ReturnsAsync(existingAccount);
        _ownerVerificatorMock.Setup(v => v.IsExists(newOwnerId)).Returns(false);

        var command = new UpdateAccountCommand(
            accountId,
            newOwnerId,
            AccountType.Checking,
            "USD",
            100,
            null,
            null);

        var handler = CreateHandler();

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() =>
            handler.Handle(command, CancellationToken.None));

        _mockDbTransaction.Verify(t => t.CommitAsync(CancellationToken.None), Times.Never);
        _mockDbTransaction.Verify(t => t.RollbackAsync(CancellationToken.None), Times.Once);
    }

    [Fact]
    public async Task Handle_ShouldThrowArgumentException_WhenCurrencyUnsupported()
    {
        // Arrange
        var accountId = Guid.NewGuid();
        var ownerId = Guid.NewGuid();
        var existingAccount = new Account { Id = accountId, OwnerId = ownerId, Currency = "USD", Version = 1 };

        var command = new UpdateAccountCommand(
            accountId,
            ownerId,
            AccountType.Checking,
            "QWE",
            100,
            null,
            null);

        _repoMock.Setup(r => r.GetByIdAsync(accountId, It.IsAny<IDbTransaction>())).ReturnsAsync(existingAccount);
        _ownerVerificatorMock.Setup(v => v.IsExists(ownerId)).Returns(true);
        _currencyValidatorMock.Setup(v => v.IsExists(command.Currency)).ReturnsAsync(false);

        var handler = CreateHandler();

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() =>
            handler.Handle(command, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_ShouldThrowDbConcurrencyException_WhenUpdateReturnsZero()
    {
        // Arrange
        var accountId = Guid.NewGuid();
        var ownerId = Guid.NewGuid();
        var existingAccount = new Account { Id = accountId, OwnerId = ownerId, Currency = "USD", Version = 1 };

        var command = new UpdateAccountCommand(
            accountId,
            ownerId,
            AccountType.Checking,
            "USD",
            100,
            null,
            null);

        _mockDbTransaction.Setup(t => t.CommitAsync(CancellationToken.None));
        _mockDbTransaction.Setup(t => t.RollbackAsync(CancellationToken.None));

        _repoMock.Setup(r => r.BeginTransactionAsync(IsolationLevel.Unspecified)).ReturnsAsync(_mockDbTransaction.Object);
        _repoMock.Setup(r => r.GetByIdAsync(accountId, _mockDbTransaction.Object)).ReturnsAsync(existingAccount);
        _ownerVerificatorMock.Setup(v => v.IsExists(ownerId)).Returns(true);
        _currencyValidatorMock.Setup(v => v.IsExists(command.Currency)).ReturnsAsync(true);
        _repoMock.Setup(r => r.UpdateAsync(It.IsAny<Account>(), null)).ReturnsAsync(0);

        var handler = CreateHandler();

        // Act & Assert
        var ex = await Assert.ThrowsAsync<DBConcurrencyException>(() =>
            handler.Handle(command, CancellationToken.None));

        Assert.Contains("modified by another process", ex.Message);
        _mockDbTransaction.Verify(t => t.CommitAsync(CancellationToken.None), Times.Never);
        _mockDbTransaction.Verify(t => t.RollbackAsync(CancellationToken.None), Times.Once);
    }
}