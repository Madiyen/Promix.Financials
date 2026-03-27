using Promix.Financials.Domain.Security;

namespace Promix.Financials.Application.Abstractions;

public interface ICompanyJournalLockRepository
{
    Task<Company?> GetByIdAsync(Guid companyId, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
}
