using Microsoft.EntityFrameworkCore;
using Promix.Financials.Application.Features.Accounts.Queries;
using Promix.Financials.Domain.Aggregates.Accounts;
using Promix.Financials.Domain.Aggregates.Journals;
using Promix.Financials.Domain.Aggregates.Parties;
using Promix.Financials.Domain.Enums;
using Promix.Financials.Infrastructure.Persistence;

namespace Promix.Financials.Infrastructure.Persistence.Queries;

internal sealed class ChartOfAccountsQuery : IChartOfAccountsQuery
{
    private static readonly string[] SalesRoles =
    [
        "SalesRevenue",
        "SalesReturns",
        "SalesDiscountAllowed"
    ];

    private static readonly string[] InventoryRoles =
    [
        "InventoryControl",
        "COGS",
        "InventoryAdjustments"
    ];

    private readonly PromixDbContext _db;

    public ChartOfAccountsQuery(PromixDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<AccountFlatDto>> GetAccountsAsync(Guid companyId)
    {
        return await _db.Set<Account>()
            .AsNoTracking()
            .Where(a => a.CompanyId == companyId)
            .OrderBy(a => a.Code)
            .Select(a => new AccountFlatDto(
                a.Id,
                a.ParentId,
                a.Code,
                a.NameAr,
                a.Nature,
                a.Classification,
                a.CloseBehavior,
                a.IsPosting,
                a.AllowManualPosting,
                a.AllowChildren,
                a.IsSystem,
                a.Origin,
                a.IsActive,
                a.CurrencyCode,
                a.SystemRole))
            .ToListAsync();
    }

    public async Task<AccountsWorkspaceDto> GetAccountsWorkspaceAsync(Guid companyId)
    {
        var accounts = await _db.Set<Account>()
            .AsNoTracking()
            .Where(a => a.CompanyId == companyId)
            .OrderBy(a => a.Code)
            .Select(a => new
            {
                a.Id,
                a.ParentId,
                a.Code,
                a.NameAr,
                a.Nature,
                a.Classification,
                a.CloseBehavior,
                a.IsPosting,
                a.AllowManualPosting,
                a.AllowChildren,
                a.IsSystem,
                a.Origin,
                a.IsActive,
                a.CurrencyCode,
                a.SystemRole
            })
            .ToListAsync();

        var movementLookup = await _db.Set<JournalLine>()
            .AsNoTracking()
            .Where(x => x.JournalEntry.CompanyId == companyId
                        && x.JournalEntry.Status == JournalEntryStatus.Posted
                        && !x.JournalEntry.IsDeleted)
            .GroupBy(x => x.AccountId)
            .Select(g => new
            {
                AccountId = g.Key,
                Balance = g.Sum(x => x.Debit - x.Credit),
                LastMovementDate = g.Max(x => x.JournalEntry.EntryDate)
            })
            .ToDictionaryAsync(
                x => x.AccountId,
                x => new
                {
                    Balance = decimal.Round(x.Balance, 2, MidpointRounding.AwayFromZero),
                    x.LastMovementDate
                });

        var childrenLookup = accounts
            .Where(x => x.ParentId.HasValue)
            .GroupBy(x => x.ParentId!.Value)
            .ToDictionary(g => g.Key, g => g.Count());

        var accountMap = accounts.ToDictionary(x => x.Id);

        var rows = accounts
            .Select(account =>
            {
                accountMap.TryGetValue(account.ParentId ?? Guid.Empty, out var parent);
                movementLookup.TryGetValue(account.Id, out var movement);
                childrenLookup.TryGetValue(account.Id, out var childCount);

                return new AccountWorkspaceRowDto(
                    account.Id,
                    account.ParentId,
                    account.Code,
                    account.NameAr,
                    account.Nature,
                    account.Classification,
                    account.CloseBehavior,
                    account.IsPosting,
                    account.AllowManualPosting,
                    account.AllowChildren,
                    account.IsSystem,
                    account.Origin,
                    account.IsActive,
                    account.CurrencyCode,
                    account.SystemRole,
                    parent?.Code,
                    parent?.NameAr,
                    movement?.Balance ?? 0m,
                    movement?.LastMovementDate,
                    childCount);
            })
            .ToList();

        var classBreakdown = rows
            .GroupBy(x => x.Classification)
            .Select(g => new AccountClassBreakdownDto(
                g.Key,
                g.Count(),
                decimal.Round(g.Sum(x => x.Balance), 2, MidpointRounding.AwayFromZero)))
            .OrderBy(x => x.Classification)
            .ToList();

        var summary = new AccountsWorkspaceSummaryDto(
            rows.Count,
            rows.Count(x => x.Origin == AccountOrigin.Template),
            rows.Count(x => x.Origin == AccountOrigin.PartyGenerated),
            rows.Count(x => x.Origin == AccountOrigin.Manual),
            rows.Count(x => x.IsActive),
            classBreakdown);

        return new AccountsWorkspaceDto(rows, summary);
    }

    public async Task<AccountDetailDto?> GetAccountDetailsAsync(Guid companyId, Guid accountId)
    {
        var account = await _db.Set<Account>()
            .AsNoTracking()
            .Where(a => a.CompanyId == companyId && a.Id == accountId)
            .Select(a => new
            {
                a.Id,
                a.ParentId,
                a.Code,
                a.NameAr,
                a.NameEn,
                a.Nature,
                a.Classification,
                a.CloseBehavior,
                a.IsPosting,
                a.AllowManualPosting,
                a.AllowChildren,
                a.IsSystem,
                a.Origin,
                a.IsActive,
                a.CurrencyCode,
                a.SystemRole,
                a.Notes
            })
            .SingleOrDefaultAsync();

        if (account is null)
            return null;

        var children = await _db.Set<Account>()
            .AsNoTracking()
            .Where(a => a.CompanyId == companyId && a.ParentId == accountId)
            .OrderBy(a => a.Code)
            .Select(a => new AccountChildPreviewDto(
                a.Id,
                a.Code,
                a.NameAr,
                a.IsPosting,
                a.IsActive,
                a.Origin))
            .ToListAsync();

        var parent = account.ParentId is Guid parentId
            ? await _db.Set<Account>()
                .AsNoTracking()
                .Where(a => a.CompanyId == companyId && a.Id == parentId)
                .Select(a => new { a.Code, a.NameAr })
                .SingleOrDefaultAsync()
            : null;

        var linkedPartyNames = await _db.Set<Party>()
            .AsNoTracking()
            .Where(p => p.CompanyId == companyId
                        && (p.ReceivableAccountId == accountId || p.PayableAccountId == accountId))
            .OrderBy(p => p.Code)
            .Select(p => p.NameAr)
            .ToListAsync();

        var movementStats = await _db.Set<JournalLine>()
            .AsNoTracking()
            .Where(x => x.AccountId == accountId
                        && x.JournalEntry.CompanyId == companyId
                        && x.JournalEntry.Status == JournalEntryStatus.Posted
                        && !x.JournalEntry.IsDeleted)
            .GroupBy(_ => 1)
            .Select(g => new
            {
                PostedMovementLinesCount = g.Count(),
                CurrentBalance = g.Sum(x => x.Debit - x.Credit)
            })
            .FirstOrDefaultAsync();

        var isSalesLinked = HasSystemRole(account.SystemRole, SalesRoles);
        var isInventoryLinked = HasSystemRole(account.SystemRole, InventoryRoles);
        var isTaxLinked = !string.IsNullOrWhiteSpace(account.SystemRole)
                          && account.SystemRole.Contains("Tax", StringComparison.OrdinalIgnoreCase);
        var isYearCloseLinked = string.Equals(account.SystemRole, "RetainedEarnings", StringComparison.OrdinalIgnoreCase);

        var postedMovementLinesCount = movementStats?.PostedMovementLinesCount ?? 0;
        var currentBalance = decimal.Round(movementStats?.CurrentBalance ?? 0m, 2, MidpointRounding.AwayFromZero);
        var childCount = children.Count;
        var linkedPartiesCount = linkedPartyNames.Count;

        var deleteReasons = BuildDeleteReasons(
            account.IsSystem,
            account.Origin,
            childCount,
            postedMovementLinesCount,
            linkedPartiesCount,
            isSalesLinked,
            isInventoryLinked,
            isTaxLinked,
            isYearCloseLinked,
            linkedPartyNames);

        var deactivateReasons = BuildDeactivateReasons(
            account.IsSystem,
            account.Origin,
            childCount,
            currentBalance,
            linkedPartiesCount,
            isSalesLinked,
            isInventoryLinked,
            isTaxLinked,
            isYearCloseLinked,
            linkedPartyNames);

        var usageSummary = new AccountUsageSummaryDto(
            childCount,
            postedMovementLinesCount,
            linkedPartiesCount,
            currentBalance,
            isSalesLinked,
            isInventoryLinked,
            isTaxLinked,
            isYearCloseLinked,
            linkedPartyNames,
            deleteReasons
                .Concat(deactivateReasons)
                .Distinct(StringComparer.Ordinal)
                .ToList(),
            deleteReasons.Count == 0,
            deactivateReasons.Count == 0);

        return new AccountDetailDto(
            account.Id,
            account.ParentId,
            account.Code,
            account.NameAr,
            account.NameEn,
            account.Nature,
            account.Classification,
            account.CloseBehavior,
            account.IsPosting,
            account.AllowManualPosting,
            account.AllowChildren,
            account.IsSystem,
            account.Origin,
            account.IsActive,
            account.CurrencyCode,
            account.SystemRole,
            account.Notes,
            GetLevel(account.Code),
            parent?.Code,
            parent?.NameAr,
            children,
            usageSummary);
    }

    private static int GetLevel(string code)
        => string.IsNullOrWhiteSpace(code)
            ? 0
            : code.Count(c => c == '.') + 1;

    private static bool HasSystemRole(string? systemRole, IEnumerable<string> roles)
        => !string.IsNullOrWhiteSpace(systemRole)
           && roles.Any(role => string.Equals(role, systemRole, StringComparison.OrdinalIgnoreCase));

    private static List<string> BuildDeleteReasons(
        bool isSystem,
        AccountOrigin origin,
        int childCount,
        int postedMovementLinesCount,
        int linkedPartiesCount,
        bool isSalesLinked,
        bool isInventoryLinked,
        bool isTaxLinked,
        bool isYearCloseLinked,
        IReadOnlyList<string> linkedPartyNames)
    {
        var reasons = new List<string>();

        if (origin == AccountOrigin.Template || isSystem)
            reasons.Add("حساب افتراضي أو نظامي محمي من الحذف.");

        if (childCount > 0)
            reasons.Add($"يحتوي على {childCount} حسابات فرعية.");

        if (postedMovementLinesCount > 0)
            reasons.Add($"مرتبط بـ {postedMovementLinesCount} سطور قيود مرحلة.");

        if (linkedPartiesCount > 0)
            reasons.Add($"مربوط بأطراف مثل: {string.Join("، ", linkedPartyNames.Take(3))}{(linkedPartyNames.Count > 3 ? "..." : string.Empty)}.");

        if (isSalesLinked)
            reasons.Add("مستخدم كحساب تشغيلي في المبيعات أو مردوداتها.");

        if (isInventoryLinked)
            reasons.Add("مستخدم كحساب تشغيلي في المخزون أو تكلفة المبيعات.");

        if (isTaxLinked)
            reasons.Add("مستخدم كحساب ضريبي أو مهيأ للضرائب.");

        if (isYearCloseLinked)
            reasons.Add("مستخدم ضمن إقفال السنة أو الأرباح المحتجزة.");

        return reasons;
    }

    private static List<string> BuildDeactivateReasons(
        bool isSystem,
        AccountOrigin origin,
        int childCount,
        decimal currentBalance,
        int linkedPartiesCount,
        bool isSalesLinked,
        bool isInventoryLinked,
        bool isTaxLinked,
        bool isYearCloseLinked,
        IReadOnlyList<string> linkedPartyNames)
    {
        var reasons = new List<string>();

        if (origin == AccountOrigin.Template || isSystem)
            reasons.Add("حساب افتراضي أو نظامي محمي من الإيقاف.");

        if (childCount > 0)
            reasons.Add("لا يمكن إيقاف حساب ما زال يحمل بنية فرعية تحته.");

        if (currentBalance != 0)
            reasons.Add($"الرصيد الحالي للحساب ليس صفراً ({currentBalance:N2}).");

        if (linkedPartiesCount > 0)
            reasons.Add($"مربوط بأطراف مثل: {string.Join("، ", linkedPartyNames.Take(3))}{(linkedPartyNames.Count > 3 ? "..." : string.Empty)}.");

        if (isSalesLinked || isInventoryLinked || isTaxLinked || isYearCloseLinked)
            reasons.Add("الحساب مرتبط بإعدادات تشغيلية أو محاسبية يجب فكها قبل الإيقاف.");

        return reasons;
    }
}
