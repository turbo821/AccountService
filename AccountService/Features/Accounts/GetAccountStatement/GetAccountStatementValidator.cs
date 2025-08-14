using FluentValidation;
using JetBrains.Annotations;

namespace AccountService.Features.Accounts.GetAccountStatement;

[UsedImplicitly]
public class GetAccountStatementValidator : AbstractValidator<GetAccountStatementQuery>
{
    public GetAccountStatementValidator()
    {
        RuleFor(x => x.AccountId)
            .NotEmpty().WithMessage("Account ID is required");  

        RuleFor(x => x.From)
            .LessThanOrEqualTo(x => x.To)
            .When(x => x.From.HasValue && x.To.HasValue)
            .WithMessage("From date must be before or equal to To date");

        RuleFor(x => x.To)
            .GreaterThanOrEqualTo(x => x.From)
            .When(x => x.From.HasValue && x.To.HasValue)
            .WithMessage("To date must be after or equal to From date");

    }
}