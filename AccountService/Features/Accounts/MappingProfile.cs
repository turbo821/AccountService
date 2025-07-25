using AccountService.Features.Accounts.CreateAccount;
using AccountService.Features.Accounts.GetAccountList;
using AutoMapper;

namespace AccountService.Features.Accounts;

public class MappingProfile : Profile
{
    public MappingProfile()
    {
        CreateMap<CreateAccountCommand, Account>()
            .ForMember(dest => dest.Currency,
                opt 
                    => opt.MapFrom(src => src.Currency.ToUpperInvariant()));

        CreateMap<Account, AccountDto>();
    }
}

