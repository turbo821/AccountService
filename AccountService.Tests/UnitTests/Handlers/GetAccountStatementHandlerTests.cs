using AccountService.Features.Accounts;
using AccountService.Features.Accounts.Abstractions;
using AccountService.Features.Accounts.GetAccountStatement;
using AutoMapper;
using Microsoft.Extensions.Logging;
using Moq;

namespace AccountService.Tests.UnitTests.Handlers;

public class GetAccountStatementHandlerTests
{
    private readonly IMapper _mapper;
    private readonly Mock<IAccountRepository> _repoMock = new();

    public GetAccountStatementHandlerTests()
    {
        var config = new MapperConfiguration(cfg =>
        {
            cfg.AddProfile<MappingProfile>(); // Предполагается, что у тебя есть mapping Account → AccountStatementDto и Transaction → TransactionDto
        }, new LoggerFactory());

        _mapper = config.CreateMapper();
    }

    private GetAccountStatementHandler CreateHandler()
        => new(_repoMock.Object, _mapper);

    [Fact]
    public async Task Handle_ShouldReturnAccountStatement_WhenAccountExists()
    {
        // Arrange
        var accountId = Guid.NewGuid();
        var from = DateTime.UtcNow.AddDays(-10);
        var to = DateTime.UtcNow;

        var transactions = new List<Transaction>
        {
            new()
            {
                Id = Guid.NewGuid(),
                AccountId = accountId,
                CounterpartyAccountId = Guid.NewGuid(),
                Amount = 100m,
                Currency = "USD",
                Type = TransactionType.Debit,
                Description = "Deposit",
                Timestamp = DateTime.UtcNow.AddDays(-5)
            },
            new()
            {
                Id = Guid.NewGuid(),
                AccountId = accountId,
                CounterpartyAccountId = Guid.NewGuid(),
                Amount = 50m,
                Currency = "USD",
                Type = TransactionType.Credit,
                Description = "Withdrawal",
                Timestamp = DateTime.UtcNow.AddDays(-2)
            }
        };

        var account = new Account
        {
            Id = accountId,
            OwnerId = Guid.NewGuid(),
            Currency = "RUB",
            Type = AccountType.Checking,
            Balance = 150m,
            Transactions = transactions
        };

        _repoMock.Setup(r => r.GetByIdWithTransactionsForPeriodAsync(accountId, from, to))
            .ReturnsAsync(account);

        var handler = CreateHandler();

        var query = new GetAccountStatementQuery(accountId, from, to);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.NotNull(result.Data);

        var dto = result.Data;
        Assert.Equal(account.Id, dto.AccountId);
        Assert.Equal(account.OwnerId, dto.OwnerId);
        Assert.Equal(account.Type, dto.Type);
        Assert.Equal(account.Balance, dto.Balance);
        Assert.NotNull(dto.Transactions);
        Assert.Equal(transactions.Count, dto.Transactions.Count);

        for (var i = 0; i < transactions.Count; i++)
        {
            var expected = transactions[i];
            var actual = dto.Transactions[i];

            Assert.Equal(expected.Id, actual.TransactionId);
            Assert.Equal(expected.CounterpartyAccountId, actual.CounterpartyAccountId);
            Assert.Equal(expected.Amount, actual.Amount);
            Assert.Equal(expected.Currency, actual.Currency);
            Assert.Equal(expected.Type, actual.Type);
            Assert.Equal(expected.Description, actual.Description);
            Assert.Equal(expected.Timestamp, actual.Timestamp);
        }

        _repoMock.Verify(r => r.GetByIdWithTransactionsForPeriodAsync(accountId, from, to), Times.Once);
    }

    [Fact]
    public async Task Handle_ShouldThrowKeyNotFoundException_WhenAccountNotFound()
    {
        // Arrange
        var accountId = Guid.NewGuid();
        var from = DateTime.UtcNow.AddDays(-10);
        var to = DateTime.UtcNow;

        _repoMock.Setup(r => r.GetByIdWithTransactionsForPeriodAsync(accountId, from, to))
            .ReturnsAsync((Account?)null);

        var handler = CreateHandler();

        var query = new GetAccountStatementQuery(accountId, from, to);

        // Act & Assert
        await Assert.ThrowsAsync<KeyNotFoundException>(() => handler.Handle(query, CancellationToken.None));

        _repoMock.Verify(r => r.GetByIdWithTransactionsForPeriodAsync(accountId, from, to), Times.Once);
    }
}