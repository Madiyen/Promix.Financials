using Microsoft.EntityFrameworkCore;
using Promix.Financials.Application.Features.Journals.Queries;
using Promix.Financials.Domain.Accounting;
using Promix.Financials.Domain.Aggregates.Accounts;
using Promix.Financials.Domain.Aggregates.Journals;
using Promix.Financials.Domain.Enums;

namespace Promix.Financials.Infrastructure.Persistence.Queries;

internal sealed class JournalEntriesQuery : IJournalEntriesQuery
{
    private readonly PromixDbContext _db;

    public JournalEntriesQuery(PromixDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<JournalEntrySummaryDto>> GetEntriesAsync(Guid companyId, CancellationToken ct = default)
    {
        return await _db.Set<JournalEntry>()
            .AsNoTracking()
            .Where(x => x.CompanyId == companyId)
            .OrderByDescending(x => x.EntryDate)
            .ThenByDescending(x => x.CreatedAtUtc)
            .Select(x => new JournalEntrySummaryDto(
                x.Id,
                x.EntryNumber,
                x.EntryDate,
                (int)x.Type,
                (int)x.Status,
                x.ReferenceNo,
                x.Description,
                x.Lines.Sum(l => l.Debit),
                x.Lines.Sum(l => l.Credit),
                x.Lines.Count,
                x.CreatedAtUtc))
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyList<JournalPostingAccountDto>> GetPostingAccountsAsync(Guid companyId, CancellationToken ct = default)
    {
        return await _db.Set<Account>()
            .AsNoTracking()
            .Where(x => x.CompanyId == companyId && x.IsPosting && x.IsActive)
            .OrderBy(x => x.Code)
            .Select(x => new JournalPostingAccountDto(x.Id, x.Code, x.NameAr, x.Nature, x.SystemRole))
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyList<JournalCurrencyOptionDto>> GetActiveCurrenciesAsync(Guid companyId, CancellationToken ct = default)
    {
        return await _db.Set<CompanyCurrency>()
            .AsNoTracking()
            .Where(x => x.CompanyId == companyId && x.IsActive)
            .OrderByDescending(x => x.IsBaseCurrency)
            .ThenBy(x => x.CurrencyCode)
            .Select(x => new JournalCurrencyOptionDto(
                x.CurrencyCode,
                x.NameAr,
                x.NameEn,
                x.Symbol,
                x.DecimalPlaces,
                x.ExchangeRate,
                x.IsBaseCurrency))
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyList<JournalAccountBalanceDto>> GetPostedAccountBalancesAsync(
        Guid companyId,
        IReadOnlyList<Guid> accountIds,
        DateOnly throughDate,
        CancellationToken ct = default)
    {
        if (accountIds.Count == 0)
            return Array.Empty<JournalAccountBalanceDto>();

        var ids = accountIds
            .Where(x => x != Guid.Empty)
            .Distinct()
            .ToList();

        var balances = await _db.Set<Account>()
            .AsNoTracking()
            .Where(x => x.CompanyId == companyId && ids.Contains(x.Id))
            .OrderBy(x => x.Code)
            .Select(x => new JournalAccountBalanceDto(
                x.Id,
                x.Code,
                x.NameAr,
                x.Nature,
                _db.Set<JournalLine>()
                    .Where(line => line.AccountId == x.Id
                        && line.JournalEntry.CompanyId == companyId
                        && line.JournalEntry.Status == JournalEntryStatus.Posted
                        && line.JournalEntry.EntryDate <= throughDate)
                    .Sum(line => (decimal?)line.Debit) ?? 0m,
                _db.Set<JournalLine>()
                    .Where(line => line.AccountId == x.Id
                        && line.JournalEntry.CompanyId == companyId
                        && line.JournalEntry.Status == JournalEntryStatus.Posted
                        && line.JournalEntry.EntryDate <= throughDate)
                    .Sum(line => (decimal?)line.Credit) ?? 0m))
            .ToListAsync(ct);

        return balances
            .OrderBy(x => ids.IndexOf(x.AccountId))
            .ToList();
    }

    public async Task<IReadOnlyList<JournalCashMovementDto>> GetCashMovementSeriesAsync(
        Guid companyId,
        DateOnly fromDate,
        DateOnly toDate,
        CancellationToken ct = default)
    {
        var cashLines = await _db.Set<JournalLine>()
            .AsNoTracking()
            .Where(line => line.JournalEntry.CompanyId == companyId
                && line.JournalEntry.Status == JournalEntryStatus.Posted
                && line.JournalEntry.EntryDate >= fromDate
                && line.JournalEntry.EntryDate <= toDate
                && (line.Account.Code.StartsWith("13")
                    || (line.Account.SystemRole != null
                        && (EF.Functions.Like(line.Account.SystemRole, "%cash%")
                            || EF.Functions.Like(line.Account.SystemRole, "%bank%")
                            || EF.Functions.Like(line.Account.SystemRole, "%treasury%")))
                    || EF.Functions.Like(line.Account.NameAr, "%صندوق%")
                    || EF.Functions.Like(line.Account.NameAr, "%خزنة%")
                    || EF.Functions.Like(line.Account.NameAr, "%خزينة%")
                    || EF.Functions.Like(line.Account.NameAr, "%الأموال الجاهزة%")
                    || EF.Functions.Like(line.Account.NameAr, "%مصرف%")
                    || EF.Functions.Like(line.Account.NameAr, "%بنك%")))
            .Select(line => new
            {
                line.JournalEntry.EntryDate,
                line.Debit,
                line.Credit,
                line.Account.Nature
            })
            .ToListAsync(ct);

        return cashLines
            .GroupBy(line => line.EntryDate)
            .Select(group => new JournalCashMovementDto(
                group.Key,
                group.Sum(line => line.Nature == AccountNature.Debit
                    ? line.Debit - line.Credit
                    : line.Credit - line.Debit)))
            .OrderBy(x => x.EntryDate)
            .ToList();
    }
}
