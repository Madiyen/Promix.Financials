using Microsoft.EntityFrameworkCore;
using Promix.Financials.Application.Abstractions;
using Promix.Financials.Domain.Aggregates.Parties;
using Promix.Financials.Domain.Enums;

namespace Promix.Financials.Infrastructure.Persistence.Repositories;

public sealed class EfPartySettlementRepository : IPartySettlementRepository
{
    private readonly PromixDbContext _db;

    public EfPartySettlementRepository(PromixDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<PartySettlement>> GetByPairAsync(Guid companyId, Guid partyId, Guid accountId, CancellationToken ct = default)
        => await _db.PartySettlements
            .Where(x => x.CompanyId == companyId && x.PartyId == partyId && x.AccountId == accountId)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<PartySettlementLedgerLine>> GetPostedLedgerLinesAsync(Guid companyId, Guid partyId, Guid accountId, CancellationToken ct = default)
        => await _db.JournalLines
            .AsNoTracking()
            .Where(x => x.PartyId == partyId
                && x.AccountId == accountId
                && x.JournalEntry.CompanyId == companyId
                && !x.JournalEntry.IsDeleted
                && x.JournalEntry.Status == JournalEntryStatus.Posted)
            .OrderBy(x => x.JournalEntry.EntryDate)
            .ThenBy(x => x.JournalEntry.EntryNumber)
            .ThenBy(x => x.LineNumber)
            .Select(x => new PartySettlementLedgerLine(
                x.Id,
                x.PartyId!.Value,
                x.AccountId,
                x.JournalEntry.EntryDate,
                x.JournalEntry.EntryNumber,
                x.LineNumber,
                x.Debit,
                x.Credit))
            .ToListAsync(ct);

    public Task AddRangeAsync(IReadOnlyList<PartySettlement> settlements, CancellationToken ct = default)
    {
        _db.PartySettlements.AddRange(settlements);
        return Task.CompletedTask;
    }

    public void RemoveRange(IEnumerable<PartySettlement> settlements)
        => _db.PartySettlements.RemoveRange(settlements);

    public Task SaveChangesAsync(CancellationToken ct = default)
        => _db.SaveChangesAsync(ct);
}
