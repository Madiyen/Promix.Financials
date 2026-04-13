using Promix.Financials.Domain.Aggregates.Journals;
using Promix.Financials.Domain.Enums;

namespace Promix.Financials.Application.Abstractions;

public interface IJournalEntryRepository
{
    Task AddAsync(JournalEntry entry, CancellationToken ct = default);
    Task<JournalEntry?> GetByIdAsync(Guid companyId, Guid entryId, CancellationToken ct = default);
    Task<string> GenerateNextNumberAsync(Guid companyId, Guid financialYearId, JournalEntryType type, CancellationToken ct = default);
    Task<JournalDailyMovementSummary> GetDailyMovementSummaryAsync(Guid companyId, Guid accountId, DateOnly entryDate, CancellationToken ct = default);
    Task<bool> HasDailyCashClosingAsync(Guid companyId, Guid accountId, DateOnly entryDate, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
}

public sealed record JournalDailyMovementSummary(
    decimal TotalDebit,
    decimal TotalCredit)
{
    public decimal NetMovement => TotalDebit - TotalCredit;
}
