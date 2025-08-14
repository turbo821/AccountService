namespace AccountService.Features.Accounts.Abstractions;

public interface IInterestAccrualService
{
    Task AccrueDailyInterestAsync();
}