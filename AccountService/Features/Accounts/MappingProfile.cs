using AccountService.Features.Accounts.CreateAccount;
using AutoMapper;

namespace AccountService.Features.Accounts;

public class MappingProfile : Profile
{
    public MappingProfile()
    {
        CreateMap<CreateAccountCommand, Account>();
    }
}

