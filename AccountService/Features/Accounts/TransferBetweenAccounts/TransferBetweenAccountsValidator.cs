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
            .NotEmpty();

        RuleFor(x => x.Description)
            .MaximumLength(255);

        RuleFor(x => x)
            .Must(x => x.FromAccountId != x.ToAccountId)
            .WithMessage("You cannot transfer funds to the same account");
    }
}