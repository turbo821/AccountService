using AccountService.Features.Accounts;
using AccountService.Features.Accounts.Abstractions;
using AccountService.Features.Accounts.CreateAccount;
using AutoMapper;
using Microsoft.Extensions.Logging;
using Moq;

namespace AccountService.Tests.UnitTests.Handlers;

public class CreateAccountHandlerWithMapperTests
{
    private readonly IMapper _realMapper;
    private readonly Mock<IAccountRepository> _repoMock = new();
    private readonly Mock<ICurrencyValidator> _currencyValidatorMock = new();
    private readonly Mock<IOwnerVerificator> _ownerVerificatorMock = new();
    public CreateAccountHandlerWithMapperTests()
    {
        var config = new MapperConfiguration(cfg =>
        {
            cfg.AddProfile<MappingProfile>();
        }, new LoggerFactory());

        _realMapper = config.CreateMapper();
    }

    private CreateAccountHandler CreateHandler()
    {
        return new CreateAccountHandler(
            _realMapper,
            _repoMock.Object,
            _currencyValidatorMock.Object,
            _ownerVerificatorMock.Object
        );
    }

    [Fact]
    public async Task Handle_ShouldCreateAccount_WhenDataIsValid()
    {
        // Arrange
        var command = new CreateAccountCommand(
            OwnerId: Guid.NewGuid(),
            Type: AccountType.Deposit,
            Currency: "USD",
            InterestRate: 15
        );

        _ownerVerificatorMock.Setup(v => v.IsExists(command.OwnerId)).Returns(true);
        _currencyValidatorMock.Setup(v => v.IsExists(command.Currency)).ReturnsAsync(true);

        var handler = CreateHandler();

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        _repoMock.Verify(r => r.AddAsync(It.Is<Account>(a =>
            a.OwnerId == command.OwnerId &&
            a.Type == command.Type &&
            a.Currency == command.Currency &&
            a.Balance == 0m &&
            a.InterestRate == command.InterestRate
        )), Times.Once);

        Assert.NotNull(result.Data);
        Assert.NotEqual(Guid.Empty, result.Data.AccountId);
    }

    [Fact]
    public async Task Handle_ShouldThrow_WhenOwnerDoesNotExist()
    {
        // Arrange
        var command = new CreateAccountCommand(
            OwnerId: Guid.NewGuid(),
            Type: AccountType.Checking,
            Currency: "USD",
            InterestRate: null
        );

        _ownerVerificatorMock.Setup(v => v.IsExists(command.OwnerId)).Returns(false);
        var handler = CreateHandler();

        // Act & Assert
        var ex = await Assert.ThrowsAsync<ArgumentException>(() => handler.Handle(command, CancellationToken.None));
        Assert.Equal("Client with this ID not found", ex.Message);
        _repoMock.Verify(r => r.AddAsync(It.IsAny<Account>()), Times.Never);
    }

    [Fact]
    public async Task Handle_ShouldThrow_WhenCurrencyNotSupported()
    {
        // Arrange
        var command = new CreateAccountCommand(
            OwnerId: Guid.NewGuid(),
            Type: AccountType.Checking,
            Currency: "EWQ",
            InterestRate: null
        );

        _ownerVerificatorMock.Setup(v => v.IsExists(command.OwnerId)).Returns(true);
        _currencyValidatorMock.Setup(v => v.IsExists(command.Currency)).ReturnsAsync(false);

        var handler = CreateHandler();

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => handler.Handle(command, CancellationToken.None));
        _repoMock.Verify(r => r.AddAsync(It.IsAny<Account>()), Times.Never);
    }
}