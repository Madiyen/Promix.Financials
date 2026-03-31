using Promix.Financials.Domain.Aggregates.Parties;

namespace Promix.Financials.Application.Abstractions;

public interface IPartyRepository
{
    Task<bool> CodeExistsAsync(Guid companyId, string code, Guid? excludePartyId = null, CancellationToken ct = default);
    Task<Party?> GetByIdAsync(Guid companyId, Guid partyId, CancellationToken ct = default);
    Task<IReadOnlyList<Party>> GetActiveAsync(Guid companyId, CancellationToken ct = default);
    Task<IReadOnlyList<Party>> GetByLinkedAccountAsync(Guid companyId, Guid accountId, Guid? excludePartyId = null, CancellationToken ct = default);
    Task AddAsync(Party party, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
}
