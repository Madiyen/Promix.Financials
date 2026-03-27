using Microsoft.EntityFrameworkCore;
using Promix.Financials.Application.Abstractions;
using Promix.Financials.Domain.Security;
using Promix.Financials.Infrastructure.Persistence;

namespace Promix.Financials.Infrastructure.Security;

public sealed class EfCompanyJournalLockRepository : ICompanyJournalLockRepository
{
    private readonly PromixDbContext _db;

    public EfCompanyJournalLockRepository(PromixDbContext db)
    {
        _db = db;
    }

    public Task<Company?> GetByIdAsync(Guid companyId, CancellationToken ct = default)
        => _db.Companies.FirstOrDefaultAsync(x => x.Id == companyId, ct);

    public Task SaveChangesAsync(CancellationToken ct = default)
        => _db.SaveChangesAsync(ct);
}
