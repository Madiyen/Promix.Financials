using Promix.Financials.Domain.Accounting;

namespace Promix.Financials.Application.Abstractions;

public interface IFinancialPeriodRepository
{
    Task<FinancialPeriod?> GetByIdAsync(Guid companyId, Guid financialPeriodId, CancellationToken ct = default);
    Task<IReadOnlyList<FinancialPeriod>> GetByFinancialYearAsync(Guid companyId, Guid financialYearId, CancellationToken ct = default);
    Task<FinancialPeriod?> GetByDateAsync(Guid companyId, Guid financialYearId, DateOnly date, CancellationToken ct = default);
    Task<bool> HasOverlapAsync(Guid companyId, Guid financialYearId, DateOnly startDate, DateOnly endDate, Guid? excludeFinancialPeriodId = null, CancellationToken ct = default);
    Task AddRangeAsync(IReadOnlyList<FinancialPeriod> periods, CancellationToken ct = default);
    void RemoveRange(IEnumerable<FinancialPeriod> periods);
    Task SaveChangesAsync(CancellationToken ct = default);
}
