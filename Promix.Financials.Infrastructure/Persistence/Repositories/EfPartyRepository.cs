using Microsoft.EntityFrameworkCore;
using Promix.Financials.Application.Abstractions;
using Promix.Financials.Domain.Aggregates.Parties;

namespace Promix.Financials.Infrastructure.Persistence.Repositories;

public sealed class EfPartyRepository : IPartyRepository
{
    private readonly PromixDbContext _db;

    public EfPartyRepository(PromixDbContext db)
    {
        _db = db;
    }

    public Task<bool> CodeExistsAsync(Guid companyId, string code, Guid? excludePartyId = null, CancellationToken ct = default)
        => _db.Parties.AnyAsync(
            x => x.CompanyId == companyId
                && x.Code == code
                && (!excludePartyId.HasValue || x.Id != excludePartyId.Value),
            ct);

    public Task<Party?> GetByIdAsync(Guid companyId, Guid partyId, CancellationToken ct = default)
        => _db.Parties.FirstOrDefaultAsync(x => x.CompanyId == companyId && x.Id == partyId, ct);

    public async Task<IReadOnlyList<Party>> GetActiveAsync(Guid companyId, CancellationToken ct = default)
        => await _db.Parties
            .AsNoTracking()
            .Where(x => x.CompanyId == companyId && x.IsActive)
            .OrderBy(x => x.Code)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<Party>> GetByLinkedAccountAsync(Guid companyId, Guid accountId, Guid? excludePartyId = null, CancellationToken ct = default)
        => await _db.Parties
            .Where(x => x.CompanyId == companyId
                && (x.ReceivableAccountId == accountId || x.PayableAccountId == accountId)
                && (!excludePartyId.HasValue || x.Id != excludePartyId.Value))
            .OrderBy(x => x.Code)
            .ToListAsync(ct);

    public Task AddAsync(Party party, CancellationToken ct = default)
    {
        _db.Parties.Add(party);
        return Task.CompletedTask;
    }

    public Task SaveChangesAsync(CancellationToken ct = default)
        => _db.SaveChangesAsync(ct);
}
