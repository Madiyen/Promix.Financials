using Microsoft.EntityFrameworkCore;
using Promix.Financials.Application.Features.FinancialYears.Queries;
using Promix.Financials.Domain.Accounting;
using Promix.Financials.Domain.Aggregates.Journals;
using Promix.Financials.Domain.Security;

namespace Promix.Financials.Infrastructure.Persistence.Queries;

internal sealed class FinancialYearQuery : IFinancialYearQuery
{
    private readonly IDbContextFactory<PromixDbContext> _dbFactory;

    public FinancialYearQuery(IDbContextFactory<PromixDbContext> dbFactory)
    {
        _dbFactory = dbFactory;
    }

    public async Task<IReadOnlyList<FinancialYearListItemDto>> GetFinancialYearsAsync(Guid companyId, CancellationToken ct = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);

        return await db.Set<FinancialYear>()
            .AsNoTracking()
            .Where(x => x.CompanyId == companyId)
            .OrderByDescending(x => x.StartDate)
            .Select(x => new FinancialYearListItemDto(
                x.Id,
                x.Code,
                x.Name,
                x.StartDate,
                x.EndDate,
                x.IsActive))
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyList<FinancialYearOptionDto>> GetSelectableYearsAsync(Guid companyId, CancellationToken ct = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);

        var explicitYears = await db.Set<FinancialYear>()
            .AsNoTracking()
            .Where(x => x.CompanyId == companyId)
            .OrderByDescending(x => x.StartDate)
            .Select(x => new FinancialYearOptionDto(
                x.Id,
                $"{x.Code} - {x.Name}",
                x.StartDate,
                x.EndDate,
                x.IsActive,
                false))
            .ToListAsync(ct);

        if (explicitYears.Count > 0)
            return explicitYears;

        var derivedYears = await db.Set<JournalEntry>()
            .AsNoTracking()
            .Where(x => x.CompanyId == companyId && !x.IsDeleted)
            .Select(x => x.EntryDate.Year)
            .Distinct()
            .OrderByDescending(x => x)
            .ToListAsync(ct);

        if (derivedYears.Count == 0)
        {
            var companyStartYear = await db.Set<Company>()
                .AsNoTracking()
                .Where(x => x.Id == companyId)
                .Select(x => x.AccountingStartDate.Year)
                .SingleOrDefaultAsync(ct);

            derivedYears.Add(companyStartYear == 0 ? DateTime.Today.Year : companyStartYear);
        }

        return derivedYears
            .Distinct()
            .OrderByDescending(x => x)
            .Select(year => new FinancialYearOptionDto(
                null,
                $"سنة مشتقة {year}",
                new DateOnly(year, 1, 1),
                new DateOnly(year, 12, 31),
                false,
                true))
            .ToList();
    }
}
