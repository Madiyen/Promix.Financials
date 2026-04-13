using Microsoft.EntityFrameworkCore;
using Promix.Financials.Application.Features.Journals.Queries;
using Promix.Financials.Domain.Accounting;
using Promix.Financials.Domain.Aggregates.Accounts;
using Promix.Financials.Domain.Aggregates.Journals;
using Promix.Financials.Domain.Enums;
using Promix.Financials.Domain.Security;

namespace Promix.Financials.Infrastructure.Persistence.Queries;

internal sealed class JournalEntriesQuery : IJournalEntriesQuery
{
    private readonly IDbContextFactory<PromixDbContext> _dbFactory;

    public JournalEntriesQuery(IDbContextFactory<PromixDbContext> dbFactory)
    {
        _dbFactory = dbFactory;
    }

    public async Task<IReadOnlyList<JournalEntrySummaryDto>> GetEntriesAsync(Guid companyId, CancellationToken ct = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);

        return await db.Set<JournalEntry>()
            .AsNoTracking()
            .Where(x => x.CompanyId == companyId && !x.IsDeleted)
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
                x.CurrencyCode,
                x.ExchangeRate,
                x.CurrencyAmount,
                x.Lines.Sum(l => l.Debit),
                x.Lines.Sum(l => l.Credit),
                x.Lines.Count,
                x.CreatedAtUtc,
                x.PostedAtUtc,
                x.ModifiedAtUtc))
            .ToListAsync(ct);
    }

    public async Task<JournalEntryDetailDto?> GetEntryDetailAsync(Guid companyId, Guid entryId, CancellationToken ct = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);

        return await db.Set<JournalEntry>()
            .AsNoTracking()
            .Where(x => x.CompanyId == companyId && x.Id == entryId && !x.IsDeleted)
            .Select(x => new JournalEntryDetailDto(
                x.Id,
                x.EntryNumber,
                x.EntryDate,
                (int)x.Type,
                (int)x.Status,
                x.ReferenceNo,
                x.Description,
                x.CurrencyCode,
                x.ExchangeRate,
                x.CurrencyAmount,
                x.CreatedAtUtc,
                x.PostedAtUtc,
                x.ModifiedByUserId,
                x.ModifiedAtUtc,
                x.Lines
                    .OrderBy(line => line.LineNumber)
                    .Select(line => new JournalEntryDetailLineDto(
                        line.AccountId,
                        line.Debit,
                        line.Credit,
                        line.PartyName,
                        line.Description,
                        line.PartyId))
                    .ToList(),
                x.TransferSettlementMode,
                (int?)x.SourceDocumentType,
                x.SourceDocumentId,
                x.SourceDocumentNumber,
                x.SourceLineId))
            .SingleOrDefaultAsync(ct);
    }

    public async Task<IReadOnlyList<JournalPostingAccountDto>> GetPostingAccountsAsync(Guid companyId, CancellationToken ct = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);

        return await db.Set<Account>()
            .AsNoTracking()
            .Where(x => x.CompanyId == companyId && x.IsPosting && x.IsActive)
            .OrderBy(x => x.Code)
            .Select(x => new JournalPostingAccountDto(
                x.Id,
                x.Code,
                x.NameAr,
                x.Nature,
                x.SystemRole,
                db.Parties.Any(p => p.CompanyId == companyId
                    && p.LedgerMode == PartyLedgerMode.LegacyLinkedAccounts
                    && (p.ReceivableAccountId == x.Id || p.PayableAccountId == x.Id))))
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyList<JournalCurrencyOptionDto>> GetActiveCurrenciesAsync(Guid companyId, CancellationToken ct = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);

        return await db.Set<CompanyCurrency>()
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

    public async Task<IReadOnlyList<int>> GetAvailableFiscalYearsAsync(Guid companyId, CancellationToken ct = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);

        var years = await db.Set<JournalEntry>()
            .AsNoTracking()
            .Where(x => x.CompanyId == companyId && !x.IsDeleted)
            .Select(x => x.EntryDate.Year)
            .Distinct()
            .OrderByDescending(x => x)
            .ToListAsync(ct);

        if (years.Count == 0)
            years.Add(DateTime.Today.Year);

        return years;
    }

    public async Task<JournalPeriodLockDto> GetJournalPeriodLockAsync(Guid companyId, CancellationToken ct = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);

        var company = await db.Set<Company>()
            .AsNoTracking()
            .Where(x => x.Id == companyId)
            .Select(x => new JournalPeriodLockDto(
                x.JournalLockedThroughDate,
                x.JournalLockedAtUtc,
                x.JournalLockedByUserId))
            .SingleOrDefaultAsync(ct);

        return company ?? new JournalPeriodLockDto(null, null, null);
    }

    public async Task<AccountStatementDto?> GetAccountStatementAsync(
        Guid companyId,
        Guid accountId,
        DateOnly fromDate,
        DateOnly toDate,
        CancellationToken ct = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);

        var account = await db.Set<Account>()
            .AsNoTracking()
            .Where(x => x.CompanyId == companyId && x.Id == accountId)
            .Select(x => new
            {
                x.Id,
                x.Code,
                x.NameAr,
                x.Nature
            })
            .SingleOrDefaultAsync(ct);

        if (account is null)
            return null;

        var opening = await db.Set<JournalLine>()
            .AsNoTracking()
            .Where(line => line.AccountId == accountId
                && line.JournalEntry.CompanyId == companyId
                && !line.JournalEntry.IsDeleted
                && line.JournalEntry.Status == JournalEntryStatus.Posted
                && line.JournalEntry.EntryDate < fromDate)
            .GroupBy(_ => 1)
            .Select(group => new
            {
                Debit = group.Sum(line => (decimal?)line.Debit) ?? 0m,
                Credit = group.Sum(line => (decimal?)line.Credit) ?? 0m
            })
            .FirstOrDefaultAsync(ct);

        var movements = await db.Set<JournalLine>()
            .AsNoTracking()
            .Where(line => line.AccountId == accountId
                && line.JournalEntry.CompanyId == companyId
                && !line.JournalEntry.IsDeleted
                && line.JournalEntry.Status == JournalEntryStatus.Posted
                && line.JournalEntry.EntryDate >= fromDate
                && line.JournalEntry.EntryDate <= toDate)
            .GroupBy(line => new
            {
                line.JournalEntry.Id,
                line.JournalEntry.EntryNumber,
                line.JournalEntry.EntryDate,
                line.JournalEntry.Type,
                line.JournalEntry.ReferenceNo,
                line.JournalEntry.Description
            })
            .OrderBy(group => group.Key.EntryDate)
            .ThenBy(group => group.Key.EntryNumber)
            .Select(group => new AccountStatementMovementDto(
                group.Key.Id,
                group.Key.EntryNumber,
                group.Key.EntryDate,
                (int)group.Key.Type,
                group.Key.ReferenceNo,
                group.Key.Description,
                group.Sum(line => line.Debit),
                group.Sum(line => line.Credit)))
            .ToListAsync(ct);

        return new AccountStatementDto(
            account.Id,
            account.Code,
            account.NameAr,
            account.Nature,
            fromDate,
            toDate,
            opening?.Debit ?? 0m,
            opening?.Credit ?? 0m,
            movements);
    }

    public async Task<IReadOnlyList<TrialBalanceRowDto>> GetTrialBalanceAsync(
        Guid companyId,
        DateOnly fromDate,
        DateOnly toDate,
        bool includeZeroBalance,
        CancellationToken ct = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);

        var accounts = await db.Set<Account>()
            .AsNoTracking()
            .Where(x => x.CompanyId == companyId && x.IsPosting)
            .Select(x => new
            {
                x.Id,
                x.Code,
                x.NameAr,
                x.Nature
            })
            .ToListAsync(ct);

        if (accounts.Count == 0)
            return Array.Empty<TrialBalanceRowDto>();

        var openingSums = await db.Set<JournalLine>()
            .AsNoTracking()
            .Where(line => line.JournalEntry.CompanyId == companyId
                && !line.JournalEntry.IsDeleted
                && line.JournalEntry.Status == JournalEntryStatus.Posted
                && line.JournalEntry.EntryDate < fromDate)
            .GroupBy(line => line.AccountId)
            .Select(group => new
            {
                AccountId = group.Key,
                Debit = group.Sum(line => line.Debit),
                Credit = group.Sum(line => line.Credit)
            })
            .ToListAsync(ct);

        var periodSums = await db.Set<JournalLine>()
            .AsNoTracking()
            .Where(line => line.JournalEntry.CompanyId == companyId
                && !line.JournalEntry.IsDeleted
                && line.JournalEntry.Status == JournalEntryStatus.Posted
                && line.JournalEntry.EntryDate >= fromDate
                && line.JournalEntry.EntryDate <= toDate)
            .GroupBy(line => line.AccountId)
            .Select(group => new
            {
                AccountId = group.Key,
                Debit = group.Sum(line => line.Debit),
                Credit = group.Sum(line => line.Credit)
            })
            .ToListAsync(ct);

        var openingMap = openingSums.ToDictionary(x => x.AccountId);
        var periodMap = periodSums.ToDictionary(x => x.AccountId);
        var rows = new List<TrialBalanceRowDto>(accounts.Count);

        foreach (var account in accounts.OrderBy(x => x.Code))
        {
            var opening = openingMap.GetValueOrDefault(account.Id);
            var period = periodMap.GetValueOrDefault(account.Id);

            var openingDebit = opening?.Debit ?? 0m;
            var openingCredit = opening?.Credit ?? 0m;
            var periodDebit = period?.Debit ?? 0m;
            var periodCredit = period?.Credit ?? 0m;

            var (openingBalanceDebit, openingBalanceCredit) = ToBalanceColumns(openingDebit, openingCredit, account.Nature);
            var (closingBalanceDebit, closingBalanceCredit) = ToBalanceColumns(
                openingDebit + periodDebit,
                openingCredit + periodCredit,
                account.Nature);

            if (!includeZeroBalance
                && openingBalanceDebit == 0m
                && openingBalanceCredit == 0m
                && periodDebit == 0m
                && periodCredit == 0m
                && closingBalanceDebit == 0m
                && closingBalanceCredit == 0m)
            {
                continue;
            }

            rows.Add(new TrialBalanceRowDto(
                account.Id,
                account.Code,
                account.NameAr,
                account.Nature,
                openingBalanceDebit,
                openingBalanceCredit,
                periodDebit,
                periodCredit,
                closingBalanceDebit,
                closingBalanceCredit));
        }

        return rows;
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

        if (ids.Count == 0)
            return Array.Empty<JournalAccountBalanceDto>();

        await using var db = await _dbFactory.CreateDbContextAsync(ct);

        var accounts = await db.Set<Account>()
            .AsNoTracking()
            .Where(x => x.CompanyId == companyId && ids.Contains(x.Id))
            .Select(x => new
            {
                x.Id,
                x.Code,
                x.NameAr,
                x.Nature
            })
            .ToListAsync(ct);

        var movementSums = await db.Set<JournalLine>()
            .AsNoTracking()
            .Where(line => ids.Contains(line.AccountId)
                && line.JournalEntry.CompanyId == companyId
                && !line.JournalEntry.IsDeleted
                && line.JournalEntry.Status == JournalEntryStatus.Posted
                && line.JournalEntry.EntryDate <= throughDate)
            .GroupBy(line => line.AccountId)
            .Select(group => new
            {
                AccountId = group.Key,
                TotalDebit = group.Sum(line => line.Debit),
                TotalCredit = group.Sum(line => line.Credit)
            })
            .ToListAsync(ct);

        var accountMap = accounts.ToDictionary(x => x.Id);
        var movementMap = movementSums.ToDictionary(x => x.AccountId);

        return ids
            .Where(accountMap.ContainsKey)
            .Select(id =>
            {
                var account = accountMap[id];
                var movement = movementMap.GetValueOrDefault(id);

                return new JournalAccountBalanceDto(
                    account.Id,
                    account.Code,
                    account.NameAr,
                    account.Nature,
                    movement?.TotalDebit ?? 0m,
                    movement?.TotalCredit ?? 0m);
            })
            .ToList();
    }

    public async Task<IReadOnlyList<JournalCashMovementDto>> GetCashMovementSeriesAsync(
        Guid companyId,
        DateOnly fromDate,
        DateOnly toDate,
        CancellationToken ct = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);

        var cashLines = await db.Set<JournalLine>()
            .AsNoTracking()
            .Where(line => line.JournalEntry.CompanyId == companyId
                && !line.JournalEntry.IsDeleted
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

    private static (decimal Debit, decimal Credit) ToBalanceColumns(decimal totalDebit, decimal totalCredit, AccountNature nature)
    {
        var signedBalance = nature == AccountNature.Debit
            ? totalDebit - totalCredit
            : totalCredit - totalDebit;

        if (signedBalance == 0m)
            return (0m, 0m);

        if (nature == AccountNature.Debit)
            return signedBalance > 0m ? (signedBalance, 0m) : (0m, Math.Abs(signedBalance));

        return signedBalance > 0m ? (0m, signedBalance) : (Math.Abs(signedBalance), 0m);
    }
}
