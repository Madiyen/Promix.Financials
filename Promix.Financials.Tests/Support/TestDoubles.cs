using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Promix.Financials.Application.Abstractions;
using Promix.Financials.Application.Features.Journals.Services;
using Promix.Financials.Application.Features.Accounts.Queries;
using Promix.Financials.Application.Features.Journals.Queries;
using Promix.Financials.Application.Features.Parties.Queries;
using Promix.Financials.Application.Features.Parties.Services;
using Promix.Financials.Domain.Accounting;
using Promix.Financials.Domain.Aggregates.Accounts;
using Promix.Financials.Domain.Aggregates.Journals;
using Promix.Financials.Domain.Aggregates.Parties;
using Promix.Financials.Domain.Enums;
using Promix.Financials.Domain.Security;
using Promix.Financials.Infrastructure.Persistence;

namespace Promix.Financials.Tests.Support;

internal sealed class TestUserContext : IUserContext
{
    private string[] _roleNames = Array.Empty<string>();

    public Guid UserId { get; set; } = Guid.NewGuid();
    public Guid? CompanyId { get; private set; }
    public string Username { get; set; } = "tester";
    public bool IsAuthenticated { get; set; } = true;
    public IReadOnlyList<string> RoleNames => _roleNames;

    public void SetCompany(Guid? companyId) => CompanyId = companyId;

    public void SetRoles(params string[] roleNames)
        => _roleNames = roleNames
            .Where(role => !string.IsNullOrWhiteSpace(role))
            .Select(role => role.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

    public bool IsInRole(string roleName)
        => !string.IsNullOrWhiteSpace(roleName)
            && _roleNames.Any(role => string.Equals(role, roleName, StringComparison.OrdinalIgnoreCase));
}

internal sealed class FixedDateTimeProvider : IDateTimeProvider
{
    public FixedDateTimeProvider(DateTimeOffset utcNow)
    {
        UtcNow = utcNow;
    }

    public DateTimeOffset UtcNow { get; }
}

internal sealed class FakeJournalEntryRepository : IJournalEntryRepository
{
    private readonly Dictionary<Guid, JournalEntry> _entries = new();

    public string NextNumber { get; set; } = "JV-0001";
    public JournalDailyMovementSummary DailyMovementSummary { get; set; } = new(500m, 0m);
    public bool DailyCashClosingExists { get; set; }
    public JournalEntry? AddedEntry { get; private set; }

    public Task AddAsync(JournalEntry entry, CancellationToken ct = default)
    {
        AddedEntry = entry;
        _entries[entry.Id] = entry;
        return Task.CompletedTask;
    }

    public Task<JournalEntry?> GetByIdAsync(Guid companyId, Guid entryId, CancellationToken ct = default)
        => Task.FromResult(
            _entries.TryGetValue(entryId, out var entry)
            && entry.CompanyId == companyId
            && !entry.IsDeleted
                ? entry
                : null);

    public Task<string> GenerateNextNumberAsync(Guid companyId, Guid financialYearId, JournalEntryType type, CancellationToken ct = default)
        => Task.FromResult(NextNumber);

    public Task<JournalDailyMovementSummary> GetDailyMovementSummaryAsync(Guid companyId, Guid accountId, DateOnly entryDate, CancellationToken ct = default)
        => Task.FromResult(DailyMovementSummary);

    public Task<bool> HasDailyCashClosingAsync(Guid companyId, Guid accountId, DateOnly entryDate, CancellationToken ct = default)
        => Task.FromResult(DailyCashClosingExists);

    public Task SaveChangesAsync(CancellationToken ct = default) => Task.CompletedTask;
}

internal sealed class FakeAccountRepository : IAccountRepository
{
    private readonly Dictionary<Guid, Account> _accounts;
    public HashSet<Guid> AccountsWithChildren { get; } = [];
    public HashSet<Guid> AccountsWithMovements { get; } = [];

    public FakeAccountRepository(IEnumerable<Account> accounts)
    {
        _accounts = accounts.ToDictionary(x => x.Id);
    }

    public Task<bool> CodeExistsAsync(Guid companyId, string code, CancellationToken ct = default)
        => Task.FromResult(_accounts.Values.Any(x => x.CompanyId == companyId && x.Code == code));
    public Task<bool> SystemRoleExistsAsync(Guid companyId, string systemRole, CancellationToken ct = default)
        => Task.FromResult(_accounts.Values.Any(x => x.CompanyId == companyId && x.SystemRole == systemRole));
    public Task<Account?> GetByIdAsync(Guid id, CancellationToken ct = default) => Task.FromResult(_accounts.GetValueOrDefault(id));
    public Task AddAsync(Account account, CancellationToken ct = default)
    {
        _accounts[account.Id] = account;
        return Task.CompletedTask;
    }
    public Task SaveChangesAsync(CancellationToken ct = default) => Task.CompletedTask;
    public Task<Account?> GetByIdAsync(Guid id, Guid companyId, CancellationToken ct = default)
        => Task.FromResult(_accounts.TryGetValue(id, out var account) && account.CompanyId == companyId ? account : null);
    public Task<Account?> GetByCodeAsync(Guid companyId, string code, CancellationToken ct = default)
        => Task.FromResult(_accounts.Values.FirstOrDefault(x => x.CompanyId == companyId && x.Code == code));
    public Task<Account?> GetBySystemRoleAsync(Guid companyId, string systemRole, CancellationToken ct = default)
        => Task.FromResult(_accounts.Values.FirstOrDefault(x => x.CompanyId == companyId && x.SystemRole == systemRole));
    public Task<IReadOnlyList<Account>> GetAllAsync(Guid companyId, CancellationToken ct = default)
        => Task.FromResult<IReadOnlyList<Account>>(_accounts.Values.Where(x => x.CompanyId == companyId).OrderBy(x => x.Code).ToList());
    public Task<IReadOnlyList<Account>> GetChildrenAsync(Guid companyId, Guid parentId, CancellationToken ct = default)
        => Task.FromResult<IReadOnlyList<Account>>(_accounts.Values.Where(x => x.CompanyId == companyId && x.ParentId == parentId).OrderBy(x => x.Code).ToList());
    public Task<IReadOnlyList<Account>> GetPostingAccountsAsync(Guid companyId, CancellationToken ct = default)
        => Task.FromResult<IReadOnlyList<Account>>(_accounts.Values.Where(x => x.CompanyId == companyId && x.IsPosting).ToList());
    public Task<bool> HasChildrenAsync(Guid accountId, Guid companyId, CancellationToken ct = default)
        => Task.FromResult(AccountsWithChildren.Contains(accountId));
    public Task<bool> HasMovementsAsync(Guid accountId, Guid companyId, CancellationToken ct = default)
        => Task.FromResult(AccountsWithMovements.Contains(accountId));
    public void Remove(Account account) => _accounts.Remove(account.Id);
}

internal sealed class FakeCompanyCurrencyRepository : ICompanyCurrencyRepository
{
    private readonly IReadOnlyList<CompanyCurrency> _currencies;

    public FakeCompanyCurrencyRepository(IReadOnlyList<CompanyCurrency> currencies)
    {
        _currencies = currencies;
    }

    public Task<bool> ExistsAsync(Guid companyId, string currencyCode, CancellationToken ct = default)
        => Task.FromResult(_currencies.Any(x => x.CompanyId == companyId && x.CurrencyCode == currencyCode));

    public Task<CompanyCurrency?> GetByIdAsync(Guid id, Guid companyId, CancellationToken ct = default)
        => Task.FromResult(_currencies.FirstOrDefault(x => x.Id == id && x.CompanyId == companyId));

    public Task<CompanyCurrency?> GetBaseCurrencyAsync(Guid companyId, CancellationToken ct = default)
        => Task.FromResult(_currencies.FirstOrDefault(x => x.CompanyId == companyId && x.IsBaseCurrency));

    public Task<IReadOnlyList<CompanyCurrency>> GetAllAsync(Guid companyId, CancellationToken ct = default)
        => Task.FromResult<IReadOnlyList<CompanyCurrency>>(_currencies.Where(x => x.CompanyId == companyId).ToList());

    public Task AddAsync(CompanyCurrency currency, CancellationToken ct = default) => Task.CompletedTask;
    public Task SaveChangesAsync(CancellationToken ct = default) => Task.CompletedTask;
}

internal sealed class FakePartyRepository : IPartyRepository
{
    private readonly Dictionary<Guid, Party> _parties;

    public FakePartyRepository(IEnumerable<Party>? parties = null)
    {
        _parties = (parties ?? Array.Empty<Party>()).ToDictionary(x => x.Id);
    }

    public Task<bool> CodeExistsAsync(Guid companyId, string code, Guid? excludePartyId = null, CancellationToken ct = default)
        => Task.FromResult(_parties.Values.Any(x =>
            x.CompanyId == companyId
            && x.Code == code
            && (!excludePartyId.HasValue || x.Id != excludePartyId.Value)));

    public Task<Party?> GetByIdAsync(Guid companyId, Guid partyId, CancellationToken ct = default)
        => Task.FromResult(_parties.TryGetValue(partyId, out var party) && party.CompanyId == companyId ? party : null);

    public Task<IReadOnlyList<Party>> GetActiveAsync(Guid companyId, CancellationToken ct = default)
        => Task.FromResult<IReadOnlyList<Party>>(_parties.Values.Where(x => x.CompanyId == companyId && x.IsActive).OrderBy(x => x.Code).ToList());

    public Task AddAsync(Party party, CancellationToken ct = default)
    {
        _parties[party.Id] = party;
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<Party>> GetByLinkedAccountAsync(Guid companyId, Guid accountId, Guid? excludePartyId = null, CancellationToken ct = default)
        => Task.FromResult<IReadOnlyList<Party>>(_parties.Values
            .Where(x => x.CompanyId == companyId)
            .Where(x => (!excludePartyId.HasValue || x.Id != excludePartyId.Value)
                        && (x.ReceivableAccountId == accountId || x.PayableAccountId == accountId))
            .OrderBy(x => x.Code)
            .ToList());

    public Task SaveChangesAsync(CancellationToken ct = default) => Task.CompletedTask;
}

internal sealed class FakePartySettlementRepository : IPartySettlementRepository
{
    private readonly List<PartySettlement> _settlements = [];
    public List<PartySettlementLedgerLine> LedgerLines { get; } = [];
    public IReadOnlyList<PartySettlement> Settlements => _settlements;
    public int GetPostedLedgerLinesCallCount { get; private set; }

    public Task<IReadOnlyList<PartySettlement>> GetByPairAsync(Guid companyId, Guid partyId, Guid accountId, CancellationToken ct = default)
        => Task.FromResult<IReadOnlyList<PartySettlement>>(_settlements.Where(x => x.CompanyId == companyId && x.PartyId == partyId && x.AccountId == accountId).ToList());

    public Task<IReadOnlyList<PartySettlementLedgerLine>> GetPostedLedgerLinesAsync(Guid companyId, Guid partyId, Guid accountId, CancellationToken ct = default)
    {
        GetPostedLedgerLinesCallCount++;
        return Task.FromResult<IReadOnlyList<PartySettlementLedgerLine>>(LedgerLines.Where(x => x.PartyId == partyId && x.AccountId == accountId).ToList());
    }

    public Task AddRangeAsync(IReadOnlyList<PartySettlement> settlements, CancellationToken ct = default)
    {
        _settlements.AddRange(settlements);
        return Task.CompletedTask;
    }

    public void RemoveRange(IEnumerable<PartySettlement> settlements)
    {
        foreach (var settlement in settlements.ToList())
            _settlements.Remove(settlement);
    }

    public Task SaveChangesAsync(CancellationToken ct = default) => Task.CompletedTask;
}

internal sealed class FakeCompanyJournalLockRepository : ICompanyJournalLockRepository
{
    public FakeCompanyJournalLockRepository(Company company)
    {
        Company = company;
    }

    public Company Company { get; }

    public Task<Company?> GetByIdAsync(Guid companyId, CancellationToken ct = default)
        => Task.FromResult(Company.Id == companyId ? Company : null);

    public Task SaveChangesAsync(CancellationToken ct = default) => Task.CompletedTask;
}

internal sealed class FakeFinancialYearRepository : IFinancialYearRepository
{
    private readonly List<FinancialYear> _years;

    public FakeFinancialYearRepository(IEnumerable<FinancialYear>? years = null)
    {
        _years = (years ?? Array.Empty<FinancialYear>()).ToList();
    }

    public Task<FinancialYear?> GetByIdAsync(Guid companyId, Guid financialYearId, CancellationToken ct = default)
        => Task.FromResult(_years.FirstOrDefault(x => x.CompanyId == companyId && x.Id == financialYearId));

    public Task<IReadOnlyList<FinancialYear>> GetByCompanyAsync(Guid companyId, CancellationToken ct = default)
        => Task.FromResult<IReadOnlyList<FinancialYear>>(_years.Where(x => x.CompanyId == companyId).OrderBy(x => x.StartDate).ToList());

    public Task<FinancialYear?> GetActiveAsync(Guid companyId, CancellationToken ct = default)
        => Task.FromResult(_years.FirstOrDefault(x => x.CompanyId == companyId && x.IsActive));

    public Task<bool> CodeExistsAsync(Guid companyId, string code, Guid? excludeFinancialYearId = null, CancellationToken ct = default)
        => Task.FromResult(_years.Any(x => x.CompanyId == companyId && x.Code == code && (!excludeFinancialYearId.HasValue || x.Id != excludeFinancialYearId.Value)));

    public Task<bool> HasOverlapAsync(Guid companyId, DateOnly startDate, DateOnly endDate, Guid? excludeFinancialYearId = null, CancellationToken ct = default)
        => Task.FromResult(_years.Any(x => x.CompanyId == companyId
            && (!excludeFinancialYearId.HasValue || x.Id != excludeFinancialYearId.Value)
            && x.StartDate <= endDate
            && x.EndDate >= startDate));

    public Task<bool> HasEntriesAsync(Guid companyId, Guid financialYearId, CancellationToken ct = default)
        => Task.FromResult(false);

    public Task AddAsync(FinancialYear financialYear, CancellationToken ct = default)
    {
        _years.Add(financialYear);
        return Task.CompletedTask;
    }

    public Task SaveChangesAsync(CancellationToken ct = default) => Task.CompletedTask;
}

internal sealed class FakeFinancialPeriodRepository : IFinancialPeriodRepository
{
    private readonly List<FinancialPeriod> _periods;

    public FakeFinancialPeriodRepository(IEnumerable<FinancialPeriod>? periods = null)
    {
        _periods = (periods ?? Array.Empty<FinancialPeriod>()).ToList();
    }

    public Task<FinancialPeriod?> GetByIdAsync(Guid companyId, Guid financialPeriodId, CancellationToken ct = default)
        => Task.FromResult(_periods.FirstOrDefault(x => x.CompanyId == companyId && x.Id == financialPeriodId));

    public Task<IReadOnlyList<FinancialPeriod>> GetByFinancialYearAsync(Guid companyId, Guid financialYearId, CancellationToken ct = default)
        => Task.FromResult<IReadOnlyList<FinancialPeriod>>(_periods.Where(x => x.CompanyId == companyId && x.FinancialYearId == financialYearId).OrderBy(x => x.StartDate).ToList());

    public Task<FinancialPeriod?> GetByDateAsync(Guid companyId, Guid financialYearId, DateOnly date, CancellationToken ct = default)
        => Task.FromResult(_periods.FirstOrDefault(x => x.CompanyId == companyId && x.FinancialYearId == financialYearId && x.StartDate <= date && x.EndDate >= date));

    public Task<bool> HasOverlapAsync(Guid companyId, Guid financialYearId, DateOnly startDate, DateOnly endDate, Guid? excludeFinancialPeriodId = null, CancellationToken ct = default)
        => Task.FromResult(_periods.Any(x => x.CompanyId == companyId
            && x.FinancialYearId == financialYearId
            && (!excludeFinancialPeriodId.HasValue || x.Id != excludeFinancialPeriodId.Value)
            && x.StartDate <= endDate
            && x.EndDate >= startDate));

    public Task AddRangeAsync(IReadOnlyList<FinancialPeriod> periods, CancellationToken ct = default)
    {
        _periods.AddRange(periods);
        return Task.CompletedTask;
    }

    public void RemoveRange(IEnumerable<FinancialPeriod> periods)
    {
        foreach (var period in periods.ToList())
            _periods.Remove(period);
    }

    public Task SaveChangesAsync(CancellationToken ct = default) => Task.CompletedTask;
}

internal static class AccountingPostingTestFactory
{
    public static (FinancialYear Year, FinancialPeriod Period) CreateActiveCalendar(Guid companyId, DateOnly entryDate)
    {
        var year = new FinancialYear(
            companyId,
            $"FY-{entryDate.Year}",
            $"السنة المالية {entryDate.Year}",
            new DateOnly(entryDate.Year, 1, 1),
            new DateOnly(entryDate.Year, 12, 31),
            true);
        var period = year.BuildMonthlyPeriods().Single(x => x.Contains(entryDate));
        return (year, period);
    }

    public static AccountingPostingService CreatePostingService(
        IJournalEntryRepository entries,
        IAccountRepository accounts,
        IPartyRepository parties,
        ICompanyCurrencyRepository currencies,
        Company company,
        DateOnly entryDate)
    {
        var (year, period) = CreateActiveCalendar(company.Id, entryDate);
        var yearRepository = new FakeFinancialYearRepository([year]);
        var periodRepository = new FakeFinancialPeriodRepository([period]);
        var partyRules = new PartyPostingRulesService(accounts, parties);
        var guard = new FinancialPeriodGuard(yearRepository, periodRepository, new FakeCompanyJournalLockRepository(company));
        return new AccountingPostingService(entries, accounts, currencies, partyRules, guard);
    }

    public static (FakeFinancialYearRepository Years, FakeFinancialPeriodRepository Periods) CreateCalendarRepositories(Company company, DateOnly entryDate)
    {
        var (year, period) = CreateActiveCalendar(company.Id, entryDate);
        return (new FakeFinancialYearRepository([year]), new FakeFinancialPeriodRepository([period]));
    }
}

internal sealed class FakeJournalEntriesQuery : IJournalEntriesQuery
{
    public IReadOnlyList<JournalEntrySummaryDto> Entries { get; set; } = Array.Empty<JournalEntrySummaryDto>();
    public JournalEntryDetailDto? EntryDetail { get; set; }
    public IReadOnlyList<JournalPostingAccountDto> Accounts { get; set; } = Array.Empty<JournalPostingAccountDto>();
    public IReadOnlyList<JournalCurrencyOptionDto> Currencies { get; set; } = Array.Empty<JournalCurrencyOptionDto>();
    public IReadOnlyList<int> FiscalYears { get; set; } = Array.Empty<int>();
    public JournalPeriodLockDto PeriodLock { get; set; } = new(null, null, null);
    public AccountStatementDto? AccountStatement { get; set; }
    public IReadOnlyList<TrialBalanceRowDto> TrialBalanceRows { get; set; } = Array.Empty<TrialBalanceRowDto>();
    public IReadOnlyList<JournalAccountBalanceDto> AccountBalances { get; set; } = Array.Empty<JournalAccountBalanceDto>();
    public IReadOnlyList<JournalCashMovementDto> CashMovements { get; set; } = Array.Empty<JournalCashMovementDto>();

    public Task<IReadOnlyList<JournalEntrySummaryDto>> GetEntriesAsync(Guid companyId, CancellationToken ct = default) => Task.FromResult(Entries);
    public Task<JournalEntryDetailDto?> GetEntryDetailAsync(Guid companyId, Guid entryId, CancellationToken ct = default) => Task.FromResult(EntryDetail);
    public Task<IReadOnlyList<JournalPostingAccountDto>> GetPostingAccountsAsync(Guid companyId, CancellationToken ct = default) => Task.FromResult(Accounts);
    public Task<IReadOnlyList<JournalCurrencyOptionDto>> GetActiveCurrenciesAsync(Guid companyId, CancellationToken ct = default) => Task.FromResult(Currencies);
    public Task<IReadOnlyList<int>> GetAvailableFiscalYearsAsync(Guid companyId, CancellationToken ct = default) => Task.FromResult(FiscalYears);
    public Task<JournalPeriodLockDto> GetJournalPeriodLockAsync(Guid companyId, CancellationToken ct = default) => Task.FromResult(PeriodLock);
    public Task<AccountStatementDto?> GetAccountStatementAsync(Guid companyId, Guid accountId, DateOnly fromDate, DateOnly toDate, CancellationToken ct = default) => Task.FromResult(AccountStatement);
    public Task<IReadOnlyList<TrialBalanceRowDto>> GetTrialBalanceAsync(Guid companyId, DateOnly fromDate, DateOnly toDate, bool includeZeroBalance, CancellationToken ct = default) => Task.FromResult(TrialBalanceRows);
    public Task<IReadOnlyList<JournalAccountBalanceDto>> GetPostedAccountBalancesAsync(Guid companyId, IReadOnlyList<Guid> accountIds, DateOnly throughDate, CancellationToken ct = default) => Task.FromResult(AccountBalances);
    public Task<IReadOnlyList<JournalCashMovementDto>> GetCashMovementSeriesAsync(Guid companyId, DateOnly fromDate, DateOnly toDate, CancellationToken ct = default) => Task.FromResult(CashMovements);
}

internal sealed class FakeChartOfAccountsQuery : IChartOfAccountsQuery
{
    public IReadOnlyList<AccountFlatDto> Accounts { get; set; } = Array.Empty<AccountFlatDto>();
    public Dictionary<Guid, AccountDetailDto> DetailsById { get; } = [];
    public AccountsWorkspaceDto Workspace { get; set; } = new(
        Array.Empty<AccountWorkspaceRowDto>(),
        new AccountsWorkspaceSummaryDto(0, 0, 0, 0, 0, Array.Empty<AccountClassBreakdownDto>()));

    public Task<IReadOnlyList<AccountFlatDto>> GetAccountsAsync(Guid companyId)
        => Task.FromResult(Accounts);

    public Task<AccountDetailDto?> GetAccountDetailsAsync(Guid companyId, Guid accountId)
        => Task.FromResult(DetailsById.GetValueOrDefault(accountId));

    public Task<AccountsWorkspaceDto> GetAccountsWorkspaceAsync(Guid companyId)
        => Task.FromResult(Workspace);
}

internal sealed class FakePartyQuery : IPartyQuery
{
    public IReadOnlyList<PartyLookupDto> ActiveParties { get; set; } = Array.Empty<PartyLookupDto>();
    public IReadOnlyList<PartyListItemDto> Parties { get; set; } = Array.Empty<PartyListItemDto>();
    public PartyStatementDto? Statement { get; set; }

    public Task<IReadOnlyList<PartyLookupDto>> GetActivePartiesAsync(Guid companyId, CancellationToken ct = default)
        => Task.FromResult(ActiveParties);

    public Task<IReadOnlyList<PartyListItemDto>> GetPartiesAsync(Guid companyId, CancellationToken ct = default)
        => Task.FromResult(Parties);

    public Task<PartyStatementDto?> GetStatementAsync(Guid companyId, Guid partyId, DateOnly fromDate, DateOnly toDate, CancellationToken ct = default)
        => Task.FromResult(Statement);
}

internal sealed class TestDbContextFactory : IDbContextFactory<PromixDbContext>
{
    private readonly DbContextOptions<PromixDbContext> _options;

    public TestDbContextFactory(DbContextOptions<PromixDbContext> options)
    {
        _options = options;
    }

    public PromixDbContext CreateDbContext() => new(_options);

    public Task<PromixDbContext> CreateDbContextAsync(CancellationToken cancellationToken = default)
        => Task.FromResult(new PromixDbContext(_options));
}
