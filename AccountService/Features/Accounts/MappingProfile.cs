using AccountService.Features.Accounts.CreateAccount;
using AccountService.Features.Accounts.GetAccountStatement;
using AccountService.Features.Accounts.RegisterTransaction;
using AccountService.Features.Accounts.UpdateAccount;
using AutoMapper;

namespace AccountService.Features.Accounts;

public class MappingProfile : Profile
{
    public MappingProfile()
    {
        CreateMap<CreateAccountCommand, Account>()
            .ForMember(dest => dest.Currency,
               opt => opt.MapFrom(src => src.Currency.ToUpperInvariant()));

        CreateMap<UpdateAccountCommand, Account>()
            .ForMember(dest => dest.Id,
                opt => opt.MapFrom(src => src.AccountId))
            .ForMember(dest => dest.Currency,
                opt => opt.MapFrom(src => src.Currency.ToUpperInvariant()));

        CreateMap<Account, AccountDto>();

        CreateMap<RegisterTransactionCommand, Transaction>()
            .ForMember(dest => dest.Currency,
                opt => opt.MapFrom(src => src.Currency.ToUpperInvariant()));

        CreateMap<Transaction, TransactionDto>()
            .ForMember(dest => dest.TransactionId,
                opt => opt.MapFrom(src => src.Id));

        CreateMap<Account, AccountStatementDto>()
            .ForMember(dest => dest.AccountId, 
                opt => opt.MapFrom(src => src.Id))
            .ForMember(dest => dest.Transactions, 
                opt => opt.MapFrom(src => src.Transactions));

        CreateMap<Account, AccountIdDto>()
            .ForMember(dest => dest.AccountId, 
                opt => opt.MapFrom(src => src.Id));

        CreateMap<Transaction, TransactionIdDto>()
            .ForMember(dest => dest.TransactionId, 
                opt => opt.MapFrom(src => src.Id));
    }
}

