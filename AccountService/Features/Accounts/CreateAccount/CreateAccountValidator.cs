using FluentValidation;
using JetBrains.Annotations;

namespace AccountService.Features.Accounts.CreateAccount;

[UsedImplicitly]
public class CreateAccountValidator : AbstractValidator<CreateAccountCommand>
{
    public CreateAccountValidator()
    {
        RuleFor(x => x.OwnerId)
            .NotEmpty().WithMessage("Owner ID is required");

        RuleFor(x => x.Type)
            .IsInEnum().WithMessage("Account type is required");

        RuleFor(x => x.Currency)
            .NotEmpty().WithMessage("Currency is required")
            .Length(3).WithMessage("Currency must be specified in ISO format (3 characters)");

        RuleFor(x => x.InterestRate)
            .NotNull()
            .When(x => x.Type is AccountType.Deposit or AccountType.Credit)
            .WithMessage("Interest rate must be provided for Deposit and Credit accounts");

        RuleFor(x => x.InterestRate)
            .Must(rate => rate >= 0)
            .When(x => x.InterestRate.HasValue && x.Type is AccountType.Deposit or AccountType.Credit)
            .WithMessage("Interest rate must be non-negative");

        RuleFor(x => x.InterestRate)
            .Must(rate => rate is null)
            .When(x => x.Type == AccountType.Checking)
            .WithMessage("Interest rate must not be set for Checking accounts");
    }
}