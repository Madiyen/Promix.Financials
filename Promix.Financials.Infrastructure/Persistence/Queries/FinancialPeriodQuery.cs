using Microsoft.EntityFrameworkCore;
using Promix.Financials.Application.Features.FinancialYears.Queries;
using Promix.Financials.Domain.Accounting;

namespace Promix.Financials.Infrastructure.Persistence.Queries;

internal sealed class FinancialPeriodQuery : IFinancialPeriodQuery
{
    private readonly IDbContextFactory<PromixDbContext> _dbFactory;

    public FinancialPeriodQuery(IDbContextFactory<PromixDbContext> dbFactory)
    {
        _dbFactory = dbFactory;
    }

    public async Task<IReadOnlyList<FinancialPeriodListItemDto>> GetFinancialPeriodsAsync(Guid companyId, Guid financialYearId, CancellationToken ct = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);

        return await db.Set<FinancialPeriod>()
            .AsNoTracking()
            .Where(x => x.CompanyId == companyId && x.FinancialYearId == financialYearId)
            .OrderBy(x => x.StartDate)
            .Select(x => new FinancialPeriodListItemDto(
                x.Id,
                x.Code,
                x.Name,
                x.StartDate,
                x.EndDate,
                x.Status,
                x.IsAdjustmentPeriod,
                db.Set<Promix.Financials.Domain.Aggregates.Journals.JournalEntry>().Count(e =>
                    e.CompanyId == companyId
                    && !e.IsDeleted
                    && e.FinancialPeriodId == x.Id)))
            .ToListAsync(ct);
    }
}
