using AccountService.Features.Accounts;
using AccountService.Features.Accounts.Abstractions;
using AccountService.Features.Accounts.GetAccountById;
using AutoMapper;
using Microsoft.Extensions.Logging;
using Moq;

namespace AccountService.Tests.UnitTests.Handlers;

public class GetAccountByIdHandlerTests
{
    private readonly IMapper _mapper;
    private readonly Mock<IAccountRepository> _repoMock = new();

    public GetAccountByIdHandlerTests()
    {
        var config = new MapperConfiguration(cfg =>
        {
            cfg.AddProfile<MappingProfile>();
        }, new LoggerFactory());

        _mapper = config.CreateMapper();
    }

    private GetAccountByIdHandler CreateHandler()
        => new (_repoMock.Object, _mapper);

    [Fact]
    public async Task Handle_ShouldReturnAccountDto_WhenAccountExists()
    {
        // Arrange
        var accountId = Guid.NewGuid();
        var account = new Account
        {
            Id = accountId,
            OwnerId = Guid.NewGuid(),
            Type = AccountType.Checking,
            Currency = "USD",
            Balance = 100m,
            InterestRate = 1.5m,
            OpenedAt = DateTime.UtcNow.AddDays(-10)
        };

        _repoMock.Setup(r => r.GetByIdAsync(accountId, null)).ReturnsAsync(account);

        var handler = CreateHandler();

        var query = new GetAccountByIdQuery(accountId);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.NotNull(result.Data);

        Assert.Equal(account.Id, result.Data.Id);
        Assert.Equal(account.OwnerId, result.Data.OwnerId);
        Assert.Equal(account.Type.ToString(), result.Data.Type);
        Assert.Equal(account.Currency, result.Data.Currency);
        Assert.Equal(account.Balance, result.Data.Balance);
        Assert.Equal(account.InterestRate, result.Data.InterestRate);
        Assert.Equal(account.OpenedAt, result.Data.OpenedAt);

        _repoMock.Verify(r => r.GetByIdAsync(accountId, null), Times.Once);
    }

    [Fact]
    public async Task Handle_ShouldThrowKeyNotFoundException_WhenAccountDoesNotExist()
    {
        // Arrange
        var accountId = Guid.NewGuid();

        _repoMock.Setup(r => r.GetByIdAsync(accountId, null)).ReturnsAsync((Account?)null);

        var handler = CreateHandler();

        var query = new GetAccountByIdQuery(accountId);

        // Act & Assert
        await Assert.ThrowsAsync<KeyNotFoundException>(() => handler.Handle(query, CancellationToken.None));
        _repoMock.Verify(r => r.GetByIdAsync(accountId, null), Times.Once);
    }
}