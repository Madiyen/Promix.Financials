using Microsoft.EntityFrameworkCore;
using Promix.Financials.Application.Abstractions;
using Promix.Financials.Domain.Aggregates.Journals;
using Promix.Financials.Domain.Enums;

namespace Promix.Financials.Infrastructure.Persistence.Repositories;

public sealed class EfJournalEntryRepository : IJournalEntryRepository
{
    private readonly PromixDbContext _db;

    public EfJournalEntryRepository(PromixDbContext db)
    {
        _db = db;
    }

    public Task AddAsync(JournalEntry entry, CancellationToken ct = default)
    {
        _db.JournalEntries.Add(entry);
        return Task.CompletedTask;
    }

    public Task<JournalEntry?> GetByIdAsync(Guid companyId, Guid entryId, CancellationToken ct = default)
        => _db.JournalEntries
            .Include(x => x.Lines)
            .FirstOrDefaultAsync(x => x.CompanyId == companyId && x.Id == entryId && !x.IsDeleted, ct);

    public async Task<string> GenerateNextNumberAsync(Guid companyId, JournalEntryType type, CancellationToken ct = default)
    {
        var prefix = type switch
        {
            JournalEntryType.ReceiptVoucher => "RV",
            JournalEntryType.PaymentVoucher => "PV",
            JournalEntryType.OpeningEntry => "OPN",
            JournalEntryType.TransferVoucher => "TRF",
            JournalEntryType.DailyCashClosing => "CCL",
            JournalEntryType.Adjustment => "ADJ",
            _ => "JV"
        };

        var lastNumber = await _db.JournalEntries
            .AsNoTracking()
            .Where(x => x.CompanyId == companyId && x.Type == type && x.EntryNumber.StartsWith(prefix))
            .OrderByDescending(x => x.EntryNumber)
            .Select(x => x.EntryNumber)
            .FirstOrDefaultAsync(ct);

        if (string.IsNullOrWhiteSpace(lastNumber))
            return $"{prefix}-000001";

        var numericPart = lastNumber[(prefix.Length + 1)..];
        if (!int.TryParse(numericPart, out var current))
            return $"{prefix}-000001";

        return $"{prefix}-{current + 1:000000}";
    }

    public async Task<JournalDailyMovementSummary> GetDailyMovementSummaryAsync(Guid companyId, Guid accountId, DateOnly entryDate, CancellationToken ct = default)
    {
        var summary = await _db.JournalEntries
            .AsNoTracking()
            .Where(x => x.CompanyId == companyId
                && !x.IsDeleted
                && x.EntryDate == entryDate
                && x.Status == JournalEntryStatus.Posted
                && x.Type != JournalEntryType.DailyCashClosing)
            .SelectMany(x => x.Lines.Where(line => line.AccountId == accountId))
            .GroupBy(_ => 1)
            .Select(group => new JournalDailyMovementSummary(
                group.Sum(line => line.Debit),
                group.Sum(line => line.Credit)))
            .FirstOrDefaultAsync(ct);

        return summary ?? new JournalDailyMovementSummary(0m, 0m);
    }

    public Task<bool> HasDailyCashClosingAsync(Guid companyId, Guid accountId, DateOnly entryDate, CancellationToken ct = default)
        => _db.JournalEntries
            .AsNoTracking()
            .Where(x => x.CompanyId == companyId
                && !x.IsDeleted
                && x.EntryDate == entryDate
                && x.Type == JournalEntryType.DailyCashClosing)
            .AnyAsync(x => x.Lines.Any(line => line.AccountId == accountId), ct);

    public Task SaveChangesAsync(CancellationToken ct = default)
        => _db.SaveChangesAsync(ct);
}
