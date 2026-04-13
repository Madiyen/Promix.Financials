using Microsoft.EntityFrameworkCore;
using Promix.Financials.Application.Features.FinancialYears.Commands;
using Promix.Financials.Application.Features.FinancialYears.Services;
using Promix.Financials.Domain.Accounting;
using Promix.Financials.Domain.Aggregates.Accounts;
using Promix.Financials.Domain.Aggregates.Journals;
using Promix.Financials.Domain.Enums;
using Promix.Financials.Domain.Exceptions;
using Promix.Financials.Domain.Security;
using Promix.Financials.Infrastructure.Persistence;
using Promix.Financials.Infrastructure.Persistence.Queries;
using Promix.Financials.Infrastructure.Persistence.Repositories;
using Promix.Financials.Tests.Support;

namespace Promix.Financials.Tests;

public sealed class FinancialYearTests
{
    [Fact]
    public async Task FinancialYearQuery_FallsBackToDerivedYearsWhenNoExplicitYearsExist()
    {
        var databaseName = Guid.NewGuid().ToString("N");
        var options = new DbContextOptionsBuilder<PromixDbContext>()
            .UseInMemoryDatabase(databaseName)
            .Options;

        var company = new Company("CMP9001", "شركة اختبار", "USD", new DateOnly(2025, 1, 1));
        var cashAccount = new Account(company.Id, "131", "الصندوق", null, AccountNature.Debit, true, null, null, "CashMain", null, true);
        var capitalAccount = new Account(company.Id, "211", "رأس المال", null, AccountNature.Credit, true, null, null, null, null, true);

        await using (var db = new PromixDbContext(options))
        {
            db.Companies.Add(company);
            db.Accounts.AddRange(cashAccount, capitalAccount);

            var year2025 = new JournalEntry(
                company.Id,
                "JV-2025",
                new DateOnly(2025, 6, 1),
                JournalEntryType.DailyJournal,
                "USD",
                1m,
                100m,
                Guid.NewGuid(),
                DateTimeOffset.UtcNow,
                null,
                "2025");
            year2025.AddLine(cashAccount.Id, null, 100m, 0m);
            year2025.AddLine(capitalAccount.Id, null, 0m, 100m);
            year2025.Post(Guid.NewGuid(), DateTimeOffset.UtcNow);

            var year2026 = new JournalEntry(
                company.Id,
                "JV-2026",
                new DateOnly(2026, 2, 1),
                JournalEntryType.DailyJournal,
                "USD",
                1m,
                100m,
                Guid.NewGuid(),
                DateTimeOffset.UtcNow,
                null,
                "2026");
            year2026.AddLine(cashAccount.Id, null, 100m, 0m);
            year2026.AddLine(capitalAccount.Id, null, 0m, 100m);
            year2026.Post(Guid.NewGuid(), DateTimeOffset.UtcNow);

            db.JournalEntries.AddRange(year2025, year2026);
            await db.SaveChangesAsync();
        }

        var query = new FinancialYearQuery(new TestDbContextFactory(options));
        var years = await query.GetSelectableYearsAsync(company.Id);

        Assert.Equal(2, years.Count);
        Assert.True(years.All(x => x.IsDerivedFallback));
        Assert.Equal(new DateOnly(2026, 1, 1), years[0].StartDate);
        Assert.Equal(new DateOnly(2025, 1, 1), years[1].StartDate);
    }

    [Fact]
    public async Task CreateFinancialYearService_BlocksOverlappingRanges()
    {
        var databaseName = Guid.NewGuid().ToString("N");
        var options = new DbContextOptionsBuilder<PromixDbContext>()
            .UseInMemoryDatabase(databaseName)
            .Options;

        var company = new Company("CMP9002", "شركة ثانية", "USD", new DateOnly(2026, 1, 1));

        await using (var db = new PromixDbContext(options))
        {
            db.Companies.Add(company);
            db.FinancialYears.Add(new FinancialYear(
                company.Id,
                "FY-2026",
                "السنة المالية 2026",
                new DateOnly(2026, 1, 1),
                new DateOnly(2026, 12, 31),
                true));
            await db.SaveChangesAsync();
        }

        await using var writeDb = new PromixDbContext(options);
        var repository = new EfFinancialYearRepository(writeDb);
        var service = new CreateFinancialYearService(repository, new FakeFinancialPeriodRepository());

        var ex = await Assert.ThrowsAsync<BusinessRuleException>(() => service.CreateAsync(new CreateFinancialYearCommand(
            company.Id,
            "FY-2026-B",
            "سنة متداخلة",
            new DateOnly(2026, 6, 1),
            new DateOnly(2027, 5, 31),
            false)));

        Assert.Contains("تتداخل", ex.Message);
    }
}
