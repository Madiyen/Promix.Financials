using Promix.Financials.Domain.Accounting;

namespace Promix.Financials.Application.Abstractions;

public interface ICompanyCurrencyRepository
{
    Task<bool> ExistsAsync(Guid companyId, string currencyCode, CancellationToken ct = default);
    Task<CompanyCurrency?> GetByIdAsync(Guid id, Guid companyId, CancellationToken ct = default);
    Task<CompanyCurrency?> GetBaseCurrencyAsync(Guid companyId, CancellationToken ct = default);
    Task<IReadOnlyList<CompanyCurrency>> GetAllAsync(Guid companyId, CancellationToken ct = default);
    Task AddAsync(CompanyCurrency currency, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
}