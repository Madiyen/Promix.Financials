using Promix.Financials.Application.Abstractions;
using Promix.Financials.Domain.Accounting;
using Promix.Financials.Domain.Exceptions;

namespace Promix.Financials.Application.Features.Journals.Services;

public sealed class FinancialPeriodGuard
{
    private readonly IFinancialYearRepository _financialYears;
    private readonly IFinancialPeriodRepository _financialPeriods;
    private readonly ICompanyJournalLockRepository _companyLocks;

    public FinancialPeriodGuard(
        IFinancialYearRepository financialYears,
        IFinancialPeriodRepository financialPeriods,
        ICompanyJournalLockRepository companyLocks)
    {
        _financialYears = financialYears;
        _financialPeriods = financialPeriods;
        _companyLocks = companyLocks;
    }

    public async Task<ResolvedFinancialPeriodContext> ResolveOpenPeriodAsync(Guid companyId, DateOnly entryDate, CancellationToken ct = default)
    {
        if (companyId == Guid.Empty)
            throw new BusinessRuleException("CompanyId is required.");

        var activeYear = await _financialYears.GetActiveAsync(companyId, ct);
        if (activeYear is null || !activeYear.Contains(entryDate))
            throw new BusinessRuleException("لا توجد سنة مالية نشطة تغطي تاريخ القيد.");

        var period = await _financialPeriods.GetByDateAsync(companyId, activeYear.Id, entryDate, ct);
        if (period is null)
            throw new BusinessRuleException("تعذر العثور على فترة مالية تغطي تاريخ القيد.");

        if (period.Status == Domain.Enums.FinancialPeriodStatus.Closed)
            throw new BusinessRuleException("الفترة المالية التي يقع ضمنها تاريخ القيد مقفلة.");

        var company = await _companyLocks.GetByIdAsync(companyId, ct)
            ?? throw new BusinessRuleException("Company was not found.");

        company.EnsureJournalDateIsOpen(entryDate);

        return new ResolvedFinancialPeriodContext(activeYear, period);
    }
}

public sealed record ResolvedFinancialPeriodContext(
    FinancialYear FinancialYear,
    FinancialPeriod FinancialPeriod);
