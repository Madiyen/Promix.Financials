using Microsoft.EntityFrameworkCore;
using Promix.Financials.Application.Abstractions;
using Promix.Financials.Domain.Accounting;

namespace Promix.Financials.Infrastructure.Persistence.Repositories;

public sealed class EfFinancialYearRepository : IFinancialYearRepository
{
    private readonly PromixDbContext _db;

    public EfFinancialYearRepository(PromixDbContext db)
    {
        _db = db;
    }

    public Task<FinancialYear?> GetByIdAsync(Guid companyId, Guid financialYearId, CancellationToken ct = default)
        => _db.Set<FinancialYear>().FirstOrDefaultAsync(x => x.CompanyId == companyId && x.Id == financialYearId, ct);

    public async Task<IReadOnlyList<FinancialYear>> GetByCompanyAsync(Guid companyId, CancellationToken ct = default)
        => await _db.Set<FinancialYear>()
            .Where(x => x.CompanyId == companyId)
            .OrderByDescending(x => x.StartDate)
            .ToListAsync(ct);

    public Task<FinancialYear?> GetActiveAsync(Guid companyId, CancellationToken ct = default)
        => _db.Set<FinancialYear>()
            .FirstOrDefaultAsync(x => x.CompanyId == companyId && x.IsActive, ct);

    public Task<bool> CodeExistsAsync(Guid companyId, string code, Guid? excludeFinancialYearId = null, CancellationToken ct = default)
        => _db.Set<FinancialYear>()
            .AnyAsync(x => x.CompanyId == companyId
                && x.Code == code
                && (!excludeFinancialYearId.HasValue || x.Id != excludeFinancialYearId.Value), ct);

    public Task<bool> HasOverlapAsync(Guid companyId, DateOnly startDate, DateOnly endDate, Guid? excludeFinancialYearId = null, CancellationToken ct = default)
        => _db.Set<FinancialYear>()
            .AnyAsync(x => x.CompanyId == companyId
                && (!excludeFinancialYearId.HasValue || x.Id != excludeFinancialYearId.Value)
                && x.StartDate <= endDate
                && x.EndDate >= startDate, ct);

    public Task AddAsync(FinancialYear financialYear, CancellationToken ct = default)
    {
        _db.Set<FinancialYear>().Add(financialYear);
        return Task.CompletedTask;
    }

    public Task SaveChangesAsync(CancellationToken ct = default)
        => _db.SaveChangesAsync(ct);
}
