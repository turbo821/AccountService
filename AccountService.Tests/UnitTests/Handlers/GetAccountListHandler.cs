using AccountService.Features.Accounts;
using AccountService.Features.Accounts.Abstractions;
using AccountService.Features.Accounts.GetAccountList;
using AutoMapper;
using Microsoft.Extensions.Logging;
using Moq;

namespace AccountService.Tests.UnitTests.Handlers;

public class GetAccountListHandlerTests
{
    private readonly IMapper _mapper;
    private readonly Mock<IAccountRepository> _repoMock = new();

    public GetAccountListHandlerTests()
    {
        var config = new MapperConfiguration(cfg =>
        {
            cfg.AddProfile<MappingProfile>();
        }, new LoggerFactory());

        _mapper = config.CreateMapper();
    }

    private GetAccountListHandler CreateHandler()
        => new (_mapper, _repoMock.Object);

    [Fact]
    public async Task Handle_ShouldReturnListOfAccountsDto()
    {
        // Arrange
        var ownerId = Guid.NewGuid();

        var accounts = new List<Account>
        {
            new()
            {
                Id = Guid.NewGuid(),
                OwnerId = ownerId,
                Type = AccountType.Checking,
                Currency = "USD",
                Balance = 100m,
                InterestRate = 1.5m,
                OpenedAt = DateTime.UtcNow.AddDays(-10)
            },
            new()
            {
                Id = Guid.NewGuid(),
                OwnerId = ownerId,
                Type = AccountType.Credit,
                Currency = "EUR",
                Balance = 200m,
                InterestRate = 2m,
                OpenedAt = DateTime.UtcNow.AddDays(-20)
            }
        };

        _repoMock.Setup(r => r.GetAllAsync(ownerId)).ReturnsAsync(accounts);

        var handler = CreateHandler();

        var query = new GetAccountListQuery(ownerId);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.NotNull(result.Data);
        Assert.Equal(accounts.Count, result.Data.Count);

        for (var i = 0; i < accounts.Count; i++)
        {
            var account = accounts[i];
            var dto = result.Data[i];

            Assert.Equal(account.Id, dto.Id);
            Assert.Equal(account.OwnerId, dto.OwnerId);
            Assert.Equal(account.Type, dto.Type);
            Assert.Equal(account.Currency, dto.Currency);
            Assert.Equal(account.Balance, dto.Balance);
            Assert.Equal(account.InterestRate, dto.InterestRate);
            Assert.Equal(account.OpenedAt, dto.OpenedAt);
        }

        _repoMock.Verify(r => r.GetAllAsync(ownerId), Times.Once);
    }
}