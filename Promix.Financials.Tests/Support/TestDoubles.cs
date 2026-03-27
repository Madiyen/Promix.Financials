using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Promix.Financials.Application.Abstractions;
using Promix.Financials.Application.Features.Journals.Queries;
using Promix.Financials.Domain.Accounting;
using Promix.Financials.Domain.Aggregates.Accounts;
using Promix.Financials.Domain.Aggregates.Journals;
using Promix.Financials.Domain.Enums;
using Promix.Financials.Domain.Security;
using Promix.Financials.Infrastructure.Persistence;

namespace Promix.Financials.Tests.Support;

internal sealed class TestUserContext : IUserContext
{
    public Guid UserId { get; set; } = Guid.NewGuid();
    public Guid? CompanyId { get; private set; }
    public string Username { get; set; } = "tester";
    public bool IsAuthenticated { get; set; } = true;

    public void SetCompany(Guid? companyId) => CompanyId = companyId;
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
        => Task.FromResult(_entries.TryGetValue(entryId, out var entry) && entry.CompanyId == companyId ? entry : null);

    public Task<string> GenerateNextNumberAsync(Guid companyId, JournalEntryType type, CancellationToken ct = default)
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

    public FakeAccountRepository(IEnumerable<Account> accounts)
    {
        _accounts = accounts.ToDictionary(x => x.Id);
    }

    public Task<bool> CodeExistsAsync(Guid companyId, string code) => Task.FromResult(false);
    public Task<bool> SystemRoleExistsAsync(Guid companyId, string systemRole) => Task.FromResult(false);
    public Task<Account?> GetByIdAsync(Guid id) => Task.FromResult(_accounts.GetValueOrDefault(id));
    public Task AddAsync(Account account) => Task.CompletedTask;
    public Task SaveChangesAsync() => Task.CompletedTask;
    public Task<Account?> GetByIdAsync(Guid id, Guid companyId)
        => Task.FromResult(_accounts.TryGetValue(id, out var account) && account.CompanyId == companyId ? account : null);
    public Task<IReadOnlyList<Account>> GetPostingAccountsAsync(Guid companyId)
        => Task.FromResult<IReadOnlyList<Account>>(_accounts.Values.Where(x => x.CompanyId == companyId && x.IsPosting).ToList());
    public Task<bool> HasChildrenAsync(Guid accountId, Guid companyId) => Task.FromResult(false);
    public Task<bool> HasMovementsAsync(Guid accountId, Guid companyId) => Task.FromResult(false);
    public void Remove(Account account) { }
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

internal sealed class FakeJournalEntriesQuery : IJournalEntriesQuery
{
    public IReadOnlyList<JournalEntrySummaryDto> Entries { get; set; } = Array.Empty<JournalEntrySummaryDto>();
    public IReadOnlyList<JournalPostingAccountDto> Accounts { get; set; } = Array.Empty<JournalPostingAccountDto>();
    public IReadOnlyList<JournalCurrencyOptionDto> Currencies { get; set; } = Array.Empty<JournalCurrencyOptionDto>();
    public IReadOnlyList<int> FiscalYears { get; set; } = Array.Empty<int>();
    public JournalPeriodLockDto PeriodLock { get; set; } = new(null, null, null);
    public AccountStatementDto? AccountStatement { get; set; }
    public IReadOnlyList<TrialBalanceRowDto> TrialBalanceRows { get; set; } = Array.Empty<TrialBalanceRowDto>();
    public IReadOnlyList<JournalAccountBalanceDto> AccountBalances { get; set; } = Array.Empty<JournalAccountBalanceDto>();
    public IReadOnlyList<JournalCashMovementDto> CashMovements { get; set; } = Array.Empty<JournalCashMovementDto>();

    public Task<IReadOnlyList<JournalEntrySummaryDto>> GetEntriesAsync(Guid companyId, CancellationToken ct = default) => Task.FromResult(Entries);
    public Task<IReadOnlyList<JournalPostingAccountDto>> GetPostingAccountsAsync(Guid companyId, CancellationToken ct = default) => Task.FromResult(Accounts);
    public Task<IReadOnlyList<JournalCurrencyOptionDto>> GetActiveCurrenciesAsync(Guid companyId, CancellationToken ct = default) => Task.FromResult(Currencies);
    public Task<IReadOnlyList<int>> GetAvailableFiscalYearsAsync(Guid companyId, CancellationToken ct = default) => Task.FromResult(FiscalYears);
    public Task<JournalPeriodLockDto> GetJournalPeriodLockAsync(Guid companyId, CancellationToken ct = default) => Task.FromResult(PeriodLock);
    public Task<AccountStatementDto?> GetAccountStatementAsync(Guid companyId, Guid accountId, DateOnly fromDate, DateOnly toDate, CancellationToken ct = default) => Task.FromResult(AccountStatement);
    public Task<IReadOnlyList<TrialBalanceRowDto>> GetTrialBalanceAsync(Guid companyId, DateOnly fromDate, DateOnly toDate, bool includeZeroBalance, CancellationToken ct = default) => Task.FromResult(TrialBalanceRows);
    public Task<IReadOnlyList<JournalAccountBalanceDto>> GetPostedAccountBalancesAsync(Guid companyId, IReadOnlyList<Guid> accountIds, DateOnly throughDate, CancellationToken ct = default) => Task.FromResult(AccountBalances);
    public Task<IReadOnlyList<JournalCashMovementDto>> GetCashMovementSeriesAsync(Guid companyId, DateOnly fromDate, DateOnly toDate, CancellationToken ct = default) => Task.FromResult(CashMovements);
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
