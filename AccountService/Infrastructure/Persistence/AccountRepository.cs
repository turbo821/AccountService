using AccountService.Features.Accounts;
using AccountService.Features.Accounts.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace AccountService.Infrastructure.Persistence;

public class AccountRepository(AppDbContext db) : IAccountRepository
{
    public async Task<Account?> GetByIdAsync(Guid id)
    {
        return await db.Accounts
            .Include(a => a.Transactions)
            .FirstOrDefaultAsync(a => a.Id == id);
    }

    public async Task<List<Account>> GetAllAsync()
    {
        return await db.Accounts
            .Include(a => a.Transactions)
            .ToListAsync();
    }

    public async Task<List<Account>> GetByOwnerIdAsync(Guid ownerId)
    {
        return await db.Accounts
            .Where(a => a.OwnerId == ownerId && a.ClosedAt == null)
            .Include(a => a.Transactions)
            .ToListAsync();
    }

    public async Task AddAsync(Account account)
    {
        db.Accounts.Add(account);
        await db.SaveChangesAsync();
    }

    public async Task UpdateAsync(Account account)
    {
        db.Accounts.Update(account);
        await db.SaveChangesAsync();
    }

    public async Task<Guid?> SoftDeleteAsync(Account account)
    {
        account.ClosedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();
        return account.Id;
    }

    public async Task<bool> ExistsAsync(Guid id)
    {
        return await db.Accounts.AnyAsync(a => a.Id == id);
    }
}