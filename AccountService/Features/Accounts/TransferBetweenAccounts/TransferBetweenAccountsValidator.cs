using FluentValidation;

namespace AccountService.Features.Accounts.TransferBetweenAccounts;

public class TransferBetweenAccountsValidator : AbstractValidator<TransferBetweenAccountsCommand>
{
    public TransferBetweenAccountsValidator()
    {
        RuleFor(x => x.FromAccountId)
            .NotEmpty()
            .WithMessage("Sender account ID is required");

        RuleFor(x => x.ToAccountId)
            .NotEmpty()
            .WithMessage("Recipient account ID is required");

        RuleFor(x => x.Amount)
            .GreaterThan(0)
            .WithMessage("The transfer amount must be greater than zero");

        RuleFor(x => x.Currency)
            .NotEmpty().WithMessage("Currency is required")
            .Length(3).WithMessage("Currency must be specified in ISO format (3 characters)");

        RuleFor(x => x.Description)
            .NotEmpty().WithMessage("Description is required")
            .MaximumLength(255).WithMessage("Description must not exceed 255 characters");

        RuleFor(x => x)
            .Must(x => x.FromAccountId != x.ToAccountId)
            .WithMessage("You cannot transfer funds to the same account");
    }
}