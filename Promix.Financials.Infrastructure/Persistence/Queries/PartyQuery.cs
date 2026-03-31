using Microsoft.EntityFrameworkCore;
using Promix.Financials.Application.Features.Parties.Queries;
using Promix.Financials.Domain.Aggregates.Accounts;
using Promix.Financials.Domain.Aggregates.Journals;
using Promix.Financials.Domain.Enums;

namespace Promix.Financials.Infrastructure.Persistence.Queries;

internal sealed class PartyQuery : IPartyQuery
{
    private readonly IDbContextFactory<PromixDbContext> _dbFactory;

    public PartyQuery(IDbContextFactory<PromixDbContext> dbFactory)
    {
        _dbFactory = dbFactory;
    }

    public async Task<IReadOnlyList<PartyLookupDto>> GetActivePartiesAsync(Guid companyId, CancellationToken ct = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);

        return await db.Parties
            .AsNoTracking()
            .Where(x => x.CompanyId == companyId && x.IsActive)
            .OrderBy(x => x.Code)
            .Select(x => new PartyLookupDto(
                x.Id,
                x.Code,
                x.NameAr,
                x.TypeFlags,
                x.LedgerMode,
                x.ReceivableAccountId,
                x.PayableAccountId,
                x.IsActive))
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyList<PartyListItemDto>> GetPartiesAsync(Guid companyId, CancellationToken ct = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);

        var controls = await GetControlAccountIdsAsync(db, companyId, ct);
        var parties = await db.Parties
            .AsNoTracking()
            .Where(x => x.CompanyId == companyId)
            .OrderBy(x => x.Code)
            .Select(x => new PartyProjection(
                x.Id,
                x.Code,
                x.NameAr,
                x.NameEn,
                x.TypeFlags,
                x.LedgerMode,
                x.Phone,
                x.Mobile,
                x.Email,
                x.TaxNo,
                x.Address,
                x.Notes,
                x.IsActive,
                x.ReceivableAccountId,
                x.PayableAccountId))
            .ToListAsync(ct);

        if (parties.Count == 0)
            return Array.Empty<PartyListItemDto>();

        var openByLine = await GetOpenLineMapAsync(
            db,
            companyId,
            parties.Select(x => x.Id).ToArray(),
            null,
            ct);

        return parties
            .Select(party =>
            {
                var receivableAccounts = GetReceivableAccountIds(party, controls);
                var payableAccounts = GetPayableAccountIds(party, controls);

                var receivableOpen = openByLine.Values
                    .Where(x => x.PartyId == party.Id && receivableAccounts.Contains(x.AccountId) && x.Debit > 0)
                    .Sum(x => x.OpenAmount);

                var payableOpen = openByLine.Values
                    .Where(x => x.PartyId == party.Id && payableAccounts.Contains(x.AccountId) && x.Credit > 0)
                    .Sum(x => x.OpenAmount);

                return new PartyListItemDto(
                    party.Id,
                    party.Code,
                    party.NameAr,
                    party.NameEn,
                    party.TypeFlags,
                    party.Phone,
                    party.Mobile,
                    party.Email,
                    party.TaxNo,
                    party.Address,
                    party.Notes,
                    party.IsActive,
                    party.LedgerMode,
                    party.ReceivableAccountId,
                    party.PayableAccountId,
                    receivableOpen,
                    payableOpen);
            })
            .ToList();
    }

    public async Task<PartyStatementDto?> GetStatementAsync(
        Guid companyId,
        Guid partyId,
        DateOnly fromDate,
        DateOnly toDate,
        CancellationToken ct = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);

        var controls = await GetControlAccountIdsAsync(db, companyId, ct);
        var party = await db.Parties
            .AsNoTracking()
            .Where(x => x.CompanyId == companyId && x.Id == partyId)
            .Select(x => new PartyProjection(
                x.Id,
                x.Code,
                x.NameAr,
                null,
                x.TypeFlags,
                x.LedgerMode,
                null,
                null,
                null,
                null,
                null,
                null,
                x.IsActive,
                x.ReceivableAccountId,
                x.PayableAccountId))
            .SingleOrDefaultAsync(ct);

        if (party is null)
            return null;

        var accountIds = GetStatementAccountIds(party, controls);
        if (accountIds.Length == 0)
        {
            return new PartyStatementDto(
                party.Id,
                party.Code,
                party.NameAr,
                party.TypeFlags,
                party.LedgerMode,
                party.ReceivableAccountId,
                party.PayableAccountId,
                fromDate,
                toDate,
                0m,
                0m,
                Array.Empty<PartyStatementMovementDto>(),
                Array.Empty<PartyOpenItemDto>(),
                Array.Empty<PartySettlementDto>(),
                BuildAgingBuckets(Array.Empty<PartyOpenItemDto>()));
        }

        var ledgerLines = await db.JournalLines
            .AsNoTracking()
            .Where(line => line.PartyId == partyId
                && accountIds.Contains(line.AccountId)
                && line.JournalEntry.CompanyId == companyId
                && !line.JournalEntry.IsDeleted
                && line.JournalEntry.Status == JournalEntryStatus.Posted
                && line.JournalEntry.EntryDate <= toDate)
            .OrderBy(line => line.JournalEntry.EntryDate)
            .ThenBy(line => line.JournalEntry.EntryNumber)
            .ThenBy(line => line.Account.Code)
            .Select(line => new PartyLedgerLineRow(
                line.Id,
                line.JournalEntryId,
                line.JournalEntry.EntryNumber,
                line.JournalEntry.EntryDate,
                line.AccountId,
                line.Account.Code,
                line.Account.NameAr,
                line.Debit,
                line.Credit,
                line.JournalEntry.ReferenceNo,
                line.Description ?? line.JournalEntry.Description))
            .ToListAsync(ct);

        var openMap = await GetOpenLineMapAsync(db, companyId, new[] { partyId }, accountIds, ct);
        var openingBalance = ledgerLines
            .Where(x => x.EntryDate < fromDate)
            .Sum(x => x.Debit - x.Credit);

        var runningBalance = openingBalance;
        var movements = new List<PartyStatementMovementDto>();
        foreach (var row in ledgerLines.Where(x => x.EntryDate >= fromDate && x.EntryDate <= toDate))
        {
            runningBalance += row.Debit - row.Credit;
            movements.Add(new PartyStatementMovementDto(
                row.LineId,
                row.EntryId,
                row.EntryNumber,
                row.EntryDate,
                row.AccountCode,
                row.AccountNameAr,
                row.Debit,
                row.Credit,
                runningBalance,
                row.ReferenceNo,
                row.Description));
        }

        var openItems = ledgerLines
            .Select(row =>
            {
                if (!openMap.TryGetValue(row.LineId, out var open) || open.OpenAmount <= 0)
                    return null;

                return new PartyOpenItemDto(
                    row.LineId,
                    row.EntryId,
                    row.EntryNumber,
                    row.EntryDate,
                    row.AccountId,
                    row.AccountCode,
                    row.AccountNameAr,
                    row.Debit,
                    row.Credit,
                    open.SettledAmount,
                    open.OpenAmount,
                    Math.Max(0, toDate.DayNumber - row.EntryDate.DayNumber),
                    row.ReferenceNo,
                    row.Description);
            })
            .Where(x => x is not null)
            .Cast<PartyOpenItemDto>()
            .OrderBy(x => x.EntryDate)
            .ThenBy(x => x.EntryNumber)
            .ToList();

        var settlements = await db.PartySettlements
            .AsNoTracking()
            .Where(x => x.CompanyId == companyId
                && x.PartyId == partyId
                && accountIds.Contains(x.AccountId)
                && x.SettledOn <= toDate)
            .Join(
                db.JournalLines.AsNoTracking(),
                settlement => settlement.DebitLineId,
                line => line.Id,
                (settlement, debitLine) => new { settlement, debitLine })
            .Join(
                db.JournalLines.AsNoTracking(),
                pair => pair.settlement.CreditLineId,
                line => line.Id,
                (pair, creditLine) => new
                {
                    pair.settlement.Id,
                    pair.settlement.DebitLineId,
                    pair.settlement.CreditLineId,
                    pair.settlement.Amount,
                    pair.settlement.SettledOn,
                    DebitEntryNumber = pair.debitLine.JournalEntry.EntryNumber,
                    CreditEntryNumber = creditLine.JournalEntry.EntryNumber
                })
            .OrderByDescending(x => x.SettledOn)
            .ThenByDescending(x => x.DebitEntryNumber)
            .Select(x => new PartySettlementDto(
                x.Id,
                x.DebitLineId,
                x.CreditLineId,
                x.Amount,
                x.SettledOn,
                x.DebitEntryNumber,
                x.CreditEntryNumber))
            .ToListAsync(ct);

        var closingBalance = ledgerLines.Sum(x => x.Debit - x.Credit);

        return new PartyStatementDto(
            party.Id,
            party.Code,
            party.NameAr,
            party.TypeFlags,
            party.LedgerMode,
            party.ReceivableAccountId,
            party.PayableAccountId,
            fromDate,
            toDate,
            openingBalance,
            closingBalance,
            movements,
            openItems,
            settlements,
            BuildAgingBuckets(openItems));
    }

    private static IReadOnlyList<PartyAgingBucketDto> BuildAgingBuckets(IReadOnlyList<PartyOpenItemDto> openItems)
    {
        return new[]
        {
            CreateAgingBucket("الحالي", openItems, 0, 30),
            CreateAgingBucket("31-60", openItems, 31, 60),
            CreateAgingBucket("61-90", openItems, 61, 90),
            CreateAgingBucket("+90", openItems, 91, int.MaxValue)
        };
    }

    private static PartyAgingBucketDto CreateAgingBucket(string label, IReadOnlyList<PartyOpenItemDto> openItems, int fromDays, int toDays)
    {
        var bucketItems = openItems
            .Where(x => x.AgeDays >= fromDays && x.AgeDays <= toDays)
            .ToList();

        return new PartyAgingBucketDto(
            label,
            bucketItems.Where(x => x.Debit > 0).Sum(x => x.OpenAmount),
            bucketItems.Where(x => x.Credit > 0).Sum(x => x.OpenAmount));
    }

    private static async Task<ControlAccountIds> GetControlAccountIdsAsync(
        PromixDbContext db,
        Guid companyId,
        CancellationToken ct)
    {
        var controls = await db.Set<Account>()
            .AsNoTracking()
            .Where(x => x.CompanyId == companyId
                && x.SystemRole != null
                && (x.SystemRole == "ARControl" || x.SystemRole == "APControl"))
            .Select(x => new { x.Id, x.SystemRole })
            .ToListAsync(ct);

        return new ControlAccountIds(
            controls.FirstOrDefault(x => x.SystemRole == "ARControl")?.Id,
            controls.FirstOrDefault(x => x.SystemRole == "APControl")?.Id);
    }

    private static Guid[] GetStatementAccountIds(PartyProjection party, ControlAccountIds controls)
        => GetReceivableAccountIds(party, controls)
            .Concat(GetPayableAccountIds(party, controls))
            .Distinct()
            .ToArray();

    private static IReadOnlyList<Guid> GetReceivableAccountIds(PartyProjection party, ControlAccountIds controls)
    {
        var ids = new List<Guid>();

        if (party.TypeFlags.HasFlag(PartyTypeFlags.Customer) && controls.ReceivableAccountId is Guid receivableControlId)
            ids.Add(receivableControlId);

        if (party.LedgerMode == PartyLedgerMode.LegacyLinkedAccounts && party.ReceivableAccountId is Guid legacyId)
            ids.Add(legacyId);

        return ids.Distinct().ToArray();
    }

    private static IReadOnlyList<Guid> GetPayableAccountIds(PartyProjection party, ControlAccountIds controls)
    {
        var ids = new List<Guid>();

        if (party.TypeFlags.HasFlag(PartyTypeFlags.Vendor) && controls.PayableAccountId is Guid payableControlId)
            ids.Add(payableControlId);

        if (party.LedgerMode == PartyLedgerMode.LegacyLinkedAccounts && party.PayableAccountId is Guid legacyId)
            ids.Add(legacyId);

        return ids.Distinct().ToArray();
    }

    private static async Task<Dictionary<Guid, PartyOpenLineSnapshot>> GetOpenLineMapAsync(
        PromixDbContext db,
        Guid companyId,
        IReadOnlyList<Guid> partyIds,
        IReadOnlyList<Guid>? accountIds,
        CancellationToken ct)
    {
        var baseLines = await db.JournalLines
            .AsNoTracking()
            .Where(line => line.PartyId.HasValue
                && partyIds.Contains(line.PartyId.Value)
                && line.JournalEntry.CompanyId == companyId
                && !line.JournalEntry.IsDeleted
                && line.JournalEntry.Status == JournalEntryStatus.Posted
                && (accountIds == null || accountIds.Contains(line.AccountId)))
            .Select(line => new
            {
                line.Id,
                PartyId = line.PartyId!.Value,
                line.AccountId,
                line.Debit,
                line.Credit
            })
            .ToListAsync(ct);

        if (baseLines.Count == 0)
            return new Dictionary<Guid, PartyOpenLineSnapshot>();

        var lineIds = baseLines.Select(x => x.Id).ToArray();

        var debitSettled = await db.PartySettlements
            .AsNoTracking()
            .Where(x => x.CompanyId == companyId && lineIds.Contains(x.DebitLineId))
            .GroupBy(x => x.DebitLineId)
            .Select(group => new { LineId = group.Key, Amount = group.Sum(x => x.Amount) })
            .ToDictionaryAsync(x => x.LineId, x => x.Amount, ct);

        var creditSettled = await db.PartySettlements
            .AsNoTracking()
            .Where(x => x.CompanyId == companyId && lineIds.Contains(x.CreditLineId))
            .GroupBy(x => x.CreditLineId)
            .Select(group => new { LineId = group.Key, Amount = group.Sum(x => x.Amount) })
            .ToDictionaryAsync(x => x.LineId, x => x.Amount, ct);

        return baseLines.ToDictionary(
            x => x.Id,
            x =>
            {
                var settledAmount = x.Debit > 0
                    ? debitSettled.GetValueOrDefault(x.Id)
                    : creditSettled.GetValueOrDefault(x.Id);
                var originalAmount = x.Debit > 0 ? x.Debit : x.Credit;

                return new PartyOpenLineSnapshot(
                    x.PartyId,
                    x.AccountId,
                    x.Debit,
                    x.Credit,
                    settledAmount,
                    Math.Max(0m, originalAmount - settledAmount));
            });
    }

    private sealed record PartyProjection(
        Guid Id,
        string Code,
        string NameAr,
        string? NameEn,
        PartyTypeFlags TypeFlags,
        PartyLedgerMode LedgerMode,
        string? Phone,
        string? Mobile,
        string? Email,
        string? TaxNo,
        string? Address,
        string? Notes,
        bool IsActive,
        Guid? ReceivableAccountId,
        Guid? PayableAccountId);

    private sealed record ControlAccountIds(Guid? ReceivableAccountId, Guid? PayableAccountId);

    private sealed record PartyLedgerLineRow(
        Guid LineId,
        Guid EntryId,
        string EntryNumber,
        DateOnly EntryDate,
        Guid AccountId,
        string AccountCode,
        string AccountNameAr,
        decimal Debit,
        decimal Credit,
        string? ReferenceNo,
        string? Description);

    private sealed record PartyOpenLineSnapshot(
        Guid PartyId,
        Guid AccountId,
        decimal Debit,
        decimal Credit,
        decimal SettledAmount,
        decimal OpenAmount);
}
