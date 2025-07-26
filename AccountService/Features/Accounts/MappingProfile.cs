using AccountService.Features.Accounts.CreateAccount;
using AccountService.Features.Accounts.GetAccountTransactions;
using AccountService.Features.Accounts.RegisterTransaction;
using AccountService.Features.Accounts.TransferBetweenAccounts;
using AutoMapper;

namespace AccountService.Features.Accounts;

public class MappingProfile : Profile
{
    public MappingProfile()
    {
        CreateMap<CreateAccountCommand, Account>()
            .ForMember(dest => dest.Currency,
               opt => opt.MapFrom(src => src.Currency.ToUpperInvariant()));

        CreateMap<Account, AccountDto>();

        CreateMap<RegisterTransactionCommand, Transaction>()
            .ForMember(dest => dest.Currency,
                opt => opt.MapFrom(src => src.Currency.ToUpperInvariant()));

        CreateMap<TransferBetweenAccountsCommand, List<Transaction>>()
            .ConvertUsing<TransferToTransactionsMappingAction>();

        CreateMap<Transaction, TransactionDto>()
            .ForMember(dest => dest.TransactionId,
                opt => opt.MapFrom(src => src.Id));

        CreateMap<Account, AccountTransactionsDto>()
            .ForMember(dest => dest.AccountId, 
                opt => opt.MapFrom(src => src.Id))
            .ForMember(dest => dest.Transactions, 
                opt => opt.MapFrom(src => src.Transactions));
    }

    protected class TransferToTransactionsMappingAction : ITypeConverter<TransferBetweenAccountsCommand, List<Transaction>>
    {
        public List<Transaction> Convert(TransferBetweenAccountsCommand source, List<Transaction> destination, ResolutionContext context)
        {
            destination.Add(new Transaction
            {
                AccountId = source.FromAccountId,
                CounterpartyAccountId = source.ToAccountId,
                Amount = source.Amount,
                Currency = source.Currency,
                Type = TransactionType.Credit,
                Description = source.Description
            });

            destination.Add(new Transaction
            {
                AccountId = source.ToAccountId,
                CounterpartyAccountId = source.FromAccountId,
                Amount = source.Amount,
                Currency = source.Currency,
                Type = TransactionType.Debit,
                Description = source.Description
            });

            return destination;
        }
    }
}

