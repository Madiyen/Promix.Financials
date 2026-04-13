using Promix.Financials.Domain.Accounting;

namespace Promix.Financials.Application.Abstractions;

public interface IFinancialYearRepository
{
    Task<FinancialYear?> GetByIdAsync(Guid companyId, Guid financialYearId, CancellationToken ct = default);
    Task<IReadOnlyList<FinancialYear>> GetByCompanyAsync(Guid companyId, CancellationToken ct = default);
    Task<FinancialYear?> GetActiveAsync(Guid companyId, CancellationToken ct = default);
    Task<bool> CodeExistsAsync(Guid companyId, string code, Guid? excludeFinancialYearId = null, CancellationToken ct = default);
    Task<bool> HasOverlapAsync(Guid companyId, DateOnly startDate, DateOnly endDate, Guid? excludeFinancialYearId = null, CancellationToken ct = default);
    Task<bool> HasEntriesAsync(Guid companyId, Guid financialYearId, CancellationToken ct = default);
    Task AddAsync(FinancialYear financialYear, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
}
