using FluentValidation;
using JetBrains.Annotations;

namespace AccountService.Features.Accounts.UpdateInterestRate;

[UsedImplicitly]
public class UpdateInterestRateValidator : AbstractValidator<UpdateInterestRateCommand>
{
    public UpdateInterestRateValidator()
    {
        RuleFor(x => x.AccountId)
            .NotEmpty().WithMessage("Account ID is required");

        RuleFor(x => x.InterestRate)
            .NotEmpty().WithMessage("Update interest rate is required");

        RuleFor(x => x.InterestRate)
            .GreaterThanOrEqualTo(0)
            .WithMessage("Interest rate must be non-negative");
    }
}