using AccountService.Features.Accounts.Abstractions;
using FluentValidation;

namespace AccountService.Features.Accounts.CreateAccount;

public class CreateAccountCommandValidator : AbstractValidator<CreateAccountCommand>
{
    public CreateAccountCommandValidator(
        ICurrencyValidator currencyValidator)
    {
        RuleFor(x => x.OwnerId)
            .NotEmpty();

        RuleFor(x => x.Currency)
            .NotEmpty();

        RuleFor(x => x.Type)
            .IsInEnum();

        RuleFor(x => x.InterestRate)
            .Cascade(CascadeMode.Stop)
            .NotNull()
            .When(x => x.Type is AccountType.Deposit or AccountType.Credit)
            .WithMessage("Interest rate must be provided for Deposit and Credit accounts.")
            .Must(rate => rate >= 0)
            .When(x => x.InterestRate.HasValue)
            .WithMessage("Interest rate must be non-negative.");

        RuleFor(x => x.InterestRate)
            .Must(rate => rate is null)
            .When(x => x.Type == AccountType.Checking)
            .WithMessage("Interest rate must not be set for Checking accounts.");
    }
}