using Microsoft.EntityFrameworkCore;
using Promix.Financials.Application.Abstractions;
using Promix.Financials.Domain.Aggregates.Accounts;
using Promix.Financials.Infrastructure.Persistence;

namespace Promix.Financials.Infrastructure.Persistence.Repositories;

public sealed class EfAccountRepository : IAccountRepository
{
    private readonly PromixDbContext _db;

    public EfAccountRepository(PromixDbContext db) => _db = db;

    public Task<bool> CodeExistsAsync(Guid companyId, string code, CancellationToken ct = default)
        => _db.Accounts.AnyAsync(a => a.CompanyId == companyId && a.Code == code, ct);

    public Task<bool> SystemRoleExistsAsync(Guid companyId, string systemRole, CancellationToken ct = default)
        => _db.Accounts.AnyAsync(a => a.CompanyId == companyId && a.SystemRole == systemRole, ct);

    public Task<Account?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => _db.Accounts.FirstOrDefaultAsync(a => a.Id == id, ct);

    public Task AddAsync(Account account, CancellationToken ct = default)
    {
        _db.Accounts.Add(account);
        return Task.CompletedTask;
    }

    public Task SaveChangesAsync(CancellationToken ct = default)
        => _db.SaveChangesAsync(ct);

    public Task<Account?> GetByIdAsync(Guid id, Guid companyId, CancellationToken ct = default)
        => _db.Accounts.FirstOrDefaultAsync(a => a.Id == id && a.CompanyId == companyId, ct);

    public Task<Account?> GetByCodeAsync(Guid companyId, string code, CancellationToken ct = default)
        => _db.Accounts.FirstOrDefaultAsync(a => a.CompanyId == companyId && a.Code == code, ct);

    public Task<Account?> GetBySystemRoleAsync(Guid companyId, string systemRole, CancellationToken ct = default)
        => _db.Accounts.FirstOrDefaultAsync(a => a.CompanyId == companyId && a.SystemRole == systemRole, ct);

    public async Task<IReadOnlyList<Account>> GetAllAsync(Guid companyId, CancellationToken ct = default)
        => await _db.Accounts
            .Where(a => a.CompanyId == companyId)
            .OrderBy(a => a.Code)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<Account>> GetChildrenAsync(Guid companyId, Guid parentId, CancellationToken ct = default)
        => await _db.Accounts
            .Where(a => a.CompanyId == companyId && a.ParentId == parentId)
            .OrderBy(a => a.Code)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<Account>> GetPostingAccountsAsync(Guid companyId, CancellationToken ct = default)
        => await _db.Accounts
            .AsNoTracking()
            .Where(a => a.CompanyId == companyId && a.IsPosting)
            .OrderBy(a => a.Code)
            .ToListAsync(ct);

    public Task<bool> HasChildrenAsync(Guid accountId, Guid companyId, CancellationToken ct = default)
        => _db.Accounts.AnyAsync(a => a.ParentId == accountId && a.CompanyId == companyId, ct);

    public Task<bool> HasMovementsAsync(Guid accountId, Guid companyId, CancellationToken ct = default)
        => _db.JournalLines
            .AnyAsync(x => x.AccountId == accountId && x.JournalEntry.CompanyId == companyId, ct);

    public void Remove(Account account)
        => _db.Accounts.Remove(account);
}
