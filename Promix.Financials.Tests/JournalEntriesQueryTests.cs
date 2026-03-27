using Microsoft.EntityFrameworkCore;
using Promix.Financials.Domain.Aggregates.Accounts;
using Promix.Financials.Domain.Aggregates.Journals;
using Promix.Financials.Domain.Enums;
using Promix.Financials.Domain.Security;
using Promix.Financials.Infrastructure.Persistence;
using Promix.Financials.Infrastructure.Persistence.Queries;
using Promix.Financials.Tests.Support;

namespace Promix.Financials.Tests;

public sealed class JournalEntriesQueryTests
{
    [Fact]
    public async Task GetTrialBalanceAsync_ReturnsExpectedClosingColumns()
    {
        var databaseName = Guid.NewGuid().ToString("N");
        var options = new DbContextOptionsBuilder<PromixDbContext>()
            .UseInMemoryDatabase(databaseName)
            .Options;

        var company = new Company("CMP2001", "Main", "USD");
        company.LockJournalThrough(new DateOnly(2026, 3, 31), Guid.NewGuid(), DateTimeOffset.UtcNow);

        var cashAccount = new Account(company.Id, "1301", "الصندوق", null, AccountNature.Debit, true, null, null, "cash", null, true);
        var capitalAccount = new Account(company.Id, "3101", "رأس المال", null, AccountNature.Credit, true, null, null, null, null, true);
        var revenueAccount = new Account(company.Id, "4101", "إيراد خدمات", null, AccountNature.Credit, true, null, null, null, null, true);

        await using (var db = new PromixDbContext(options))
        {
            db.Companies.Add(company);
            db.Accounts.AddRange(cashAccount, capitalAccount, revenueAccount);

            var openingEntry = new JournalEntry(
                company.Id,
                "OP-0001",
                new DateOnly(2026, 3, 1),
                JournalEntryType.OpeningEntry,
                "USD",
                1m,
                1000m,
                Guid.NewGuid(),
                DateTimeOffset.UtcNow.AddDays(-10),
                null,
                "Opening");
            openingEntry.AddLine(cashAccount.Id, null, 1000m, 0m);
            openingEntry.AddLine(capitalAccount.Id, null, 0m, 1000m);
            openingEntry.Post(Guid.NewGuid(), DateTimeOffset.UtcNow.AddDays(-10));

            var receiptEntry = new JournalEntry(
                company.Id,
                "RV-0001",
                new DateOnly(2026, 3, 10),
                JournalEntryType.ReceiptVoucher,
                "USD",
                1m,
                250m,
                Guid.NewGuid(),
                DateTimeOffset.UtcNow.AddDays(-5),
                null,
                "Receipt");
            receiptEntry.AddLine(cashAccount.Id, null, 250m, 0m);
            receiptEntry.AddLine(revenueAccount.Id, null, 0m, 250m);
            receiptEntry.Post(Guid.NewGuid(), DateTimeOffset.UtcNow.AddDays(-5));

            db.JournalEntries.AddRange(openingEntry, receiptEntry);
            await db.SaveChangesAsync();
        }

        var query = new JournalEntriesQuery(new TestDbContextFactory(options));
        var rows = await query.GetTrialBalanceAsync(company.Id, new DateOnly(2026, 3, 10), new DateOnly(2026, 3, 31), includeZeroBalance: false);
        var cashRow = Assert.Single(rows, x => x.AccountId == cashAccount.Id);
        var capitalRow = Assert.Single(rows, x => x.AccountId == capitalAccount.Id);
        var revenueRow = Assert.Single(rows, x => x.AccountId == revenueAccount.Id);

        Assert.Equal(1000m, cashRow.OpeningDebit);
        Assert.Equal(250m, cashRow.PeriodDebit);
        Assert.Equal(1250m, cashRow.ClosingDebit);
        Assert.Equal(1000m, capitalRow.OpeningCredit);
        Assert.Equal(250m, revenueRow.PeriodCredit);
        Assert.Equal(1250m, rows.Sum(x => x.ClosingDebit));
        Assert.Equal(1250m, rows.Sum(x => x.ClosingCredit));
    }

    [Fact]
    public async Task GetJournalPeriodLockAsync_ReturnsCompanyLockMetadata()
    {
        var databaseName = Guid.NewGuid().ToString("N");
        var options = new DbContextOptionsBuilder<PromixDbContext>()
            .UseInMemoryDatabase(databaseName)
            .Options;

        var lockedByUserId = Guid.NewGuid();
        var company = new Company("CMP2002", "Locked", "USD");
        company.LockJournalThrough(new DateOnly(2026, 4, 1), lockedByUserId, new DateTimeOffset(2026, 4, 1, 18, 30, 0, TimeSpan.Zero));

        await using (var db = new PromixDbContext(options))
        {
            db.Companies.Add(company);
            await db.SaveChangesAsync();
        }

        var query = new JournalEntriesQuery(new TestDbContextFactory(options));
        var lockInfo = await query.GetJournalPeriodLockAsync(company.Id);

        Assert.Equal(new DateOnly(2026, 4, 1), lockInfo.LockedThroughDate);
        Assert.Equal(lockedByUserId, lockInfo.LockedByUserId);
        Assert.NotNull(lockInfo.LockedAtUtc);
    }
}
