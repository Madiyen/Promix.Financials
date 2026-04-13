using Microsoft.EntityFrameworkCore;
using Promix.Financials.Application.Features.Journals.Commands;
using Promix.Financials.Application.Features.Journals.Services;
using Promix.Financials.Application.Features.Parties.Services;
using Promix.Financials.Domain.Accounting;
using Promix.Financials.Domain.Aggregates.Accounts;
using Promix.Financials.Domain.Aggregates.Journals;
using Promix.Financials.Domain.Enums;
using Promix.Financials.Domain.Exceptions;
using Promix.Financials.Domain.Security;
using Promix.Financials.Infrastructure.Persistence;
using Promix.Financials.Infrastructure.Persistence.Repositories;
using Promix.Financials.Tests.Support;

namespace Promix.Financials.Tests;

public sealed class FinancialCoreStabilizationTests
{
    [Fact]
    public void FinancialYear_BuildMonthlyPeriods_GeneratesClippedMonthlyCalendar()
    {
        var companyId = Guid.NewGuid();
        var year = new FinancialYear(
            companyId,
            "FY-2026-P",
            "سنة جزئية",
            new DateOnly(2026, 3, 15),
            new DateOnly(2026, 5, 10),
            true);

        var periods = year.BuildMonthlyPeriods();

        Assert.Equal(3, periods.Count);
        Assert.Collection(
            periods,
            first =>
            {
                Assert.Equal(new DateOnly(2026, 3, 15), first.StartDate);
                Assert.Equal(new DateOnly(2026, 3, 31), first.EndDate);
            },
            second =>
            {
                Assert.Equal(new DateOnly(2026, 4, 1), second.StartDate);
                Assert.Equal(new DateOnly(2026, 4, 30), second.EndDate);
            },
            third =>
            {
                Assert.Equal(new DateOnly(2026, 5, 1), third.StartDate);
                Assert.Equal(new DateOnly(2026, 5, 10), third.EndDate);
            });
    }

    [Fact]
    public async Task FinancialPeriodRepository_DetectsOverlap_AndResolvesByDate()
    {
        var databaseName = Guid.NewGuid().ToString("N");
        var options = new DbContextOptionsBuilder<PromixDbContext>()
            .UseInMemoryDatabase(databaseName)
            .Options;

        var company = new Company("CMP9201", "Periods", "USD", new DateOnly(2026, 1, 1));
        var year = new FinancialYear(company.Id, "FY-2026", "السنة المالية 2026", new DateOnly(2026, 1, 1), new DateOnly(2026, 12, 31), true);
        var january = new FinancialPeriod(company.Id, year.Id, "2026-01", "يناير 2026", new DateOnly(2026, 1, 1), new DateOnly(2026, 1, 31));
        var february = new FinancialPeriod(company.Id, year.Id, "2026-02", "فبراير 2026", new DateOnly(2026, 2, 1), new DateOnly(2026, 2, 28));

        await using (var db = new PromixDbContext(options))
        {
            db.Companies.Add(company);
            db.FinancialYears.Add(year);
            db.FinancialPeriods.AddRange(january, february);
            await db.SaveChangesAsync();
        }

        await using var readDb = new PromixDbContext(options);
        var repository = new EfFinancialPeriodRepository(readDb);

        Assert.True(await repository.HasOverlapAsync(company.Id, year.Id, new DateOnly(2026, 1, 15), new DateOnly(2026, 2, 2)));
        Assert.False(await repository.HasOverlapAsync(company.Id, year.Id, new DateOnly(2026, 3, 1), new DateOnly(2026, 3, 31)));

        var resolved = await repository.GetByDateAsync(company.Id, year.Id, new DateOnly(2026, 2, 10));
        Assert.NotNull(resolved);
        Assert.Equal(february.Id, resolved!.Id);
    }

    [Fact]
    public async Task FinancialPeriodGuard_RejectsClosedPeriod()
    {
        var company = new Company("CMP9202", "Closed", "USD", new DateOnly(2026, 1, 1));
        var (year, period) = AccountingPostingTestFactory.CreateActiveCalendar(company.Id, new DateOnly(2026, 3, 20));
        period.Close();
        var guard = new FinancialPeriodGuard(
            new FakeFinancialYearRepository([year]),
            new FakeFinancialPeriodRepository([period]),
            new FakeCompanyJournalLockRepository(company));

        var ex = await Assert.ThrowsAsync<BusinessRuleException>(() => guard.ResolveOpenPeriodAsync(company.Id, new DateOnly(2026, 3, 20)));

        Assert.Contains("الفترة المالية", ex.Message);
        Assert.Contains("مقفلة", ex.Message);
    }

    [Fact]
    public async Task FinancialPeriodGuard_RejectsWhenNoActiveYearMatchesDate()
    {
        var company = new Company("CMP9203", "No Year", "USD", new DateOnly(2026, 1, 1));
        var inactiveYear = new FinancialYear(company.Id, "FY-2025", "السنة المالية 2025", new DateOnly(2025, 1, 1), new DateOnly(2025, 12, 31), false);
        var period = inactiveYear.BuildMonthlyPeriods().First();
        var guard = new FinancialPeriodGuard(
            new FakeFinancialYearRepository([inactiveYear]),
            new FakeFinancialPeriodRepository([period]),
            new FakeCompanyJournalLockRepository(company));

        var ex = await Assert.ThrowsAsync<BusinessRuleException>(() => guard.ResolveOpenPeriodAsync(company.Id, new DateOnly(2026, 3, 20)));

        Assert.Contains("سنة مالية نشطة", ex.Message);
    }

    [Fact]
    public async Task PostJournalEntryService_RejectsBeforeLockedThroughDate()
    {
        var company = new Company("CMP9204", "Locked", "USD", new DateOnly(2026, 1, 1));
        company.LockJournalThrough(new DateOnly(2026, 3, 20), Guid.NewGuid(), DateTimeOffset.UtcNow);
        var cashAccount = new Account(company.Id, "1301", "الصندوق", null, AccountNature.Debit, true, null, null, "cash", null, true);
        var revenueAccount = new Account(company.Id, "4101", "إيراد خدمات", null, AccountNature.Credit, true, null, null, null, null, true);
        var entry = CreateDraftReceipt(company.Id, cashAccount.Id, revenueAccount.Id, new DateOnly(2026, 3, 15));
        var repository = new FakeJournalEntryRepository();
        await repository.AddAsync(entry);

        var service = CreatePostService(company, entry.EntryDate, repository, [cashAccount, revenueAccount]);

        var ex = await Assert.ThrowsAsync<BusinessRuleException>(() => service.PostAsync(new PostJournalEntryCommand(company.Id, entry.Id)));

        Assert.Contains("الفترة المحاسبية مقفلة", ex.Message);
        Assert.Equal(JournalEntryStatus.Draft, entry.Status);
    }

    [Fact]
    public async Task PostJournalEntryService_AssignsPostingMetadataAndFinancialContext()
    {
        var company = new Company("CMP9205", "Posting", "USD", new DateOnly(2026, 1, 1));
        var cashAccount = new Account(company.Id, "1301", "الصندوق", null, AccountNature.Debit, true, null, null, "cash", null, true);
        var revenueAccount = new Account(company.Id, "4101", "إيراد خدمات", null, AccountNature.Credit, true, null, null, null, null, true);
        var entryDate = new DateOnly(2026, 3, 25);
        var entry = CreateDraftReceipt(company.Id, cashAccount.Id, revenueAccount.Id, entryDate);
        var repository = new FakeJournalEntryRepository();
        await repository.AddAsync(entry);
        var userContext = new TestUserContext { UserId = Guid.NewGuid(), IsAuthenticated = true };
        var clock = new FixedDateTimeProvider(new DateTimeOffset(2026, 3, 25, 13, 0, 0, TimeSpan.Zero));
        var (year, period) = AccountingPostingTestFactory.CreateActiveCalendar(company.Id, entryDate);
        var posting = CreatePostingService(company, repository, [cashAccount, revenueAccount], entryDate, year, period);
        var settlements = new RebuildPartySettlementsService(new FakePartySettlementRepository(), userContext, clock);
        var service = new PostJournalEntryService(repository, userContext, clock, posting, settlements);

        await service.PostAsync(new PostJournalEntryCommand(company.Id, entry.Id));

        Assert.Equal(JournalEntryStatus.Posted, entry.Status);
        Assert.Equal(userContext.UserId, entry.PostedByUserId);
        Assert.Equal(clock.UtcNow, entry.PostedAtUtc);
        Assert.Equal(year.Id, entry.FinancialYearId);
        Assert.Equal(period.Id, entry.FinancialPeriodId);
    }

    [Fact]
    public async Task EfJournalEntryRepository_GenerateNextNumberAsync_UsesFinancialYearAndTypePrefix()
    {
        var databaseName = Guid.NewGuid().ToString("N");
        var options = new DbContextOptionsBuilder<PromixDbContext>()
            .UseInMemoryDatabase(databaseName)
            .Options;

        var company = new Company("CMP9206", "Numbering", "USD", new DateOnly(2025, 1, 1));
        var year2025 = new FinancialYear(company.Id, "FY-2025", "السنة المالية 2025", new DateOnly(2025, 1, 1), new DateOnly(2025, 12, 31), false);
        var year2026 = new FinancialYear(company.Id, "FY-2026", "السنة المالية 2026", new DateOnly(2026, 1, 1), new DateOnly(2026, 12, 31), true);

        await using (var db = new PromixDbContext(options))
        {
            db.Companies.Add(company);
            db.FinancialYears.AddRange(year2025, year2026);

            db.JournalEntries.Add(new JournalEntry(
                company.Id,
                "RV-2026-000001",
                new DateOnly(2026, 1, 10),
                year2026.Id,
                Guid.NewGuid(),
                JournalEntryType.ReceiptVoucher,
                "USD",
                1m,
                100m,
                SourceDocumentType.ReceiptVoucher,
                null,
                "RV-2026-000001",
                null,
                Guid.NewGuid(),
                DateTimeOffset.UtcNow,
                null,
                "قيد قبض"));

            db.JournalEntries.Add(new JournalEntry(
                company.Id,
                "RV-2026-000002",
                new DateOnly(2026, 1, 11),
                year2026.Id,
                Guid.NewGuid(),
                JournalEntryType.ReceiptVoucher,
                "USD",
                1m,
                100m,
                SourceDocumentType.ReceiptVoucher,
                null,
                "RV-2026-000002",
                null,
                Guid.NewGuid(),
                DateTimeOffset.UtcNow,
                null,
                "قيد قبض"));

            db.JournalEntries.Add(new JournalEntry(
                company.Id,
                "RV-2025-000005",
                new DateOnly(2025, 12, 15),
                year2025.Id,
                Guid.NewGuid(),
                JournalEntryType.ReceiptVoucher,
                "USD",
                1m,
                100m,
                SourceDocumentType.ReceiptVoucher,
                null,
                "RV-2025-000005",
                null,
                Guid.NewGuid(),
                DateTimeOffset.UtcNow,
                null,
                "قيد قبض"));

            db.JournalEntries.Add(new JournalEntry(
                company.Id,
                "PV-2026-000004",
                new DateOnly(2026, 1, 12),
                year2026.Id,
                Guid.NewGuid(),
                JournalEntryType.PaymentVoucher,
                "USD",
                1m,
                100m,
                SourceDocumentType.PaymentVoucher,
                null,
                "PV-2026-000004",
                null,
                Guid.NewGuid(),
                DateTimeOffset.UtcNow,
                null,
                "قيد صرف"));

            await db.SaveChangesAsync();
        }

        await using var readDb = new PromixDbContext(options);
        var repository = new EfJournalEntryRepository(readDb);

        var nextReceipt = await repository.GenerateNextNumberAsync(company.Id, year2026.Id, JournalEntryType.ReceiptVoucher);
        var nextPayment = await repository.GenerateNextNumberAsync(company.Id, year2026.Id, JournalEntryType.PaymentVoucher);

        Assert.Equal("RV-2026-000003", nextReceipt);
        Assert.Equal("PV-2026-000005", nextPayment);
    }

    private static PostJournalEntryService CreatePostService(Company company, DateOnly entryDate, FakeJournalEntryRepository repository, IReadOnlyList<Account> accounts)
    {
        var userContext = new TestUserContext { UserId = Guid.NewGuid(), IsAuthenticated = true };
        var clock = new FixedDateTimeProvider(DateTimeOffset.UtcNow);
        var (year, period) = AccountingPostingTestFactory.CreateActiveCalendar(company.Id, entryDate);
        var posting = CreatePostingService(
            company,
            repository,
            accounts,
            entryDate,
            year,
            period);

        return new PostJournalEntryService(
            repository,
            userContext,
            clock,
            posting,
            new RebuildPartySettlementsService(new FakePartySettlementRepository(), userContext, clock));
    }

    private static AccountingPostingService CreatePostingService(
        Company company,
        FakeJournalEntryRepository repository,
        IReadOnlyList<Account> accounts,
        DateOnly entryDate,
        FinancialYear year,
        FinancialPeriod period)
    {
        var accountRepository = new FakeAccountRepository(accounts);
        var partyRepository = new FakePartyRepository();
        var currencyRepository = new FakeCompanyCurrencyRepository([new CompanyCurrency(company.Id, "USD", "دولار", "USD", "$", 2, 1m, true)]);
        var partyRules = new PartyPostingRulesService(accountRepository, partyRepository);
        var guard = new FinancialPeriodGuard(
            new FakeFinancialYearRepository([year]),
            new FakeFinancialPeriodRepository([period]),
            new FakeCompanyJournalLockRepository(company));

        return new AccountingPostingService(repository, accountRepository, currencyRepository, partyRules, guard);
    }

    private static JournalEntry CreateDraftReceipt(Guid companyId, Guid cashAccountId, Guid counterpartyAccountId, DateOnly entryDate)
    {
        var entry = new JournalEntry(
            companyId,
            "RV-DRAFT-0001",
            entryDate,
            JournalEntryType.ReceiptVoucher,
            "USD",
            1m,
            100m,
            Guid.NewGuid(),
            DateTimeOffset.UtcNow,
            "RV-DRAFT-0001",
            "قبض مسودة");
        entry.AddLine(cashAccountId, null, 100m, 0m);
        entry.AddLine(counterpartyAccountId, "عميل", null, 0m, 100m);
        return entry;
    }
}
