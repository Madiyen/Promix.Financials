using Microsoft.EntityFrameworkCore;
using Promix.Financials.Application.Abstractions;
using Promix.Financials.Domain.Accounting;

namespace Promix.Financials.Infrastructure.Persistence.Repositories;

public sealed class EfFinancialPeriodRepository : IFinancialPeriodRepository
{
    private readonly PromixDbContext _db;

    public EfFinancialPeriodRepository(PromixDbContext db)
    {
        _db = db;
    }

    public Task<FinancialPeriod?> GetByIdAsync(Guid companyId, Guid financialPeriodId, CancellationToken ct = default)
        => _db.Set<FinancialPeriod>()
            .FirstOrDefaultAsync(x => x.CompanyId == companyId && x.Id == financialPeriodId, ct);

    public async Task<IReadOnlyList<FinancialPeriod>> GetByFinancialYearAsync(Guid companyId, Guid financialYearId, CancellationToken ct = default)
        => await _db.Set<FinancialPeriod>()
            .Where(x => x.CompanyId == companyId && x.FinancialYearId == financialYearId)
            .OrderBy(x => x.StartDate)
            .ToListAsync(ct);

    public Task<FinancialPeriod?> GetByDateAsync(Guid companyId, Guid financialYearId, DateOnly date, CancellationToken ct = default)
        => _db.Set<FinancialPeriod>()
            .FirstOrDefaultAsync(x => x.CompanyId == companyId
                && x.FinancialYearId == financialYearId
                && x.StartDate <= date
                && x.EndDate >= date, ct);

    public Task<bool> HasOverlapAsync(Guid companyId, Guid financialYearId, DateOnly startDate, DateOnly endDate, Guid? excludeFinancialPeriodId = null, CancellationToken ct = default)
        => _db.Set<FinancialPeriod>()
            .AnyAsync(x => x.CompanyId == companyId
                && x.FinancialYearId == financialYearId
                && (!excludeFinancialPeriodId.HasValue || x.Id != excludeFinancialPeriodId.Value)
                && x.StartDate <= endDate
                && x.EndDate >= startDate, ct);

    public Task AddRangeAsync(IReadOnlyList<FinancialPeriod> periods, CancellationToken ct = default)
    {
        _db.Set<FinancialPeriod>().AddRange(periods);
        return Task.CompletedTask;
    }

    public void RemoveRange(IEnumerable<FinancialPeriod> periods)
        => _db.Set<FinancialPeriod>().RemoveRange(periods);

    public Task SaveChangesAsync(CancellationToken ct = default)
        => _db.SaveChangesAsync(ct);
}
