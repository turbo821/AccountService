using AccountService.Features.Accounts.Contracts;
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

        CreateMap<Account, AccountDto>()
            .ForMember(dest => dest.Type,
                opt => opt.MapFrom(src => src.Type.ToString()));

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

        CreateMap<Account, AccountOpened>()
            .ForCtorParam("EventId", opt => opt.MapFrom(_ => Guid.NewGuid()))
            .ForCtorParam("OccurredAt", opt => opt.MapFrom(_ => DateTime.UtcNow))
            .ForCtorParam("AccountId", opt => opt.MapFrom(src => src.Id));

        CreateMap<Transaction, MoneyCredited>()
            .ForCtorParam("EventId", opt => opt.MapFrom(_ => Guid.NewGuid()))
            .ForCtorParam("OccurredAt", opt => opt.MapFrom(_ => DateTime.UtcNow))
            .ForCtorParam("OperationId", opt => opt.MapFrom(src => src.Id))
            .ForCtorParam("Reason", opt => opt.MapFrom(src => src.Description));

        CreateMap<Transaction, MoneyDebited>()
            .ForCtorParam("EventId", opt => opt.MapFrom(_ => Guid.NewGuid()))
            .ForCtorParam("OccurredAt", opt => opt.MapFrom(_ => DateTime.UtcNow))
            .ForCtorParam("OperationId", opt => opt.MapFrom(src => src.Id))
            .ForCtorParam("Reason", opt => opt.MapFrom(src => src.Description));
    }
}

