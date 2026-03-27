using Promix.Financials.Application.Features.Journals.Commands;
using Promix.Financials.Application.Features.Journals.Services;
using Promix.Financials.Domain.Accounting;
using Promix.Financials.Domain.Aggregates.Accounts;
using Promix.Financials.Domain.Enums;
using Promix.Financials.Domain.Exceptions;
using Promix.Financials.Domain.Security;
using Promix.Financials.Tests.Support;

namespace Promix.Financials.Tests;

public sealed class CreateJournalEntryServiceTests
{
    [Fact]
    public async Task CreateAsync_RejectsEntryInsideLockedPeriod()
    {
        var company = new Company("CMP1001", "Main", "USD");
        company.LockJournalThrough(new DateOnly(2026, 3, 20), Guid.NewGuid(), DateTimeOffset.UtcNow);

        var cashAccount = new Account(company.Id, "1301", "الصندوق", null, AccountNature.Debit, true, null, null, "cash", null, true);
        var revenueAccount = new Account(company.Id, "4101", "إيراد خدمات", null, AccountNature.Credit, true, null, null, null, null, true);
        var userContext = new TestUserContext { UserId = Guid.NewGuid(), IsAuthenticated = true };
        var lockService = new JournalPeriodLockService(
            new FakeCompanyJournalLockRepository(company),
            userContext,
            new FixedDateTimeProvider(DateTimeOffset.UtcNow));

        var service = new CreateJournalEntryService(
            new FakeJournalEntryRepository(),
            new FakeAccountRepository([cashAccount, revenueAccount]),
            new FakeCompanyCurrencyRepository([new CompanyCurrency(company.Id, "USD", "دولار", "USD", "$", 2, 1m, true)]),
            userContext,
            new FixedDateTimeProvider(DateTimeOffset.UtcNow),
            lockService);

        var command = new CreateJournalEntryCommand(
            company.Id,
            new DateOnly(2026, 3, 15),
            JournalEntryType.ReceiptVoucher,
            null,
            "قبض داخل فترة مقفلة",
            "USD",
            1m,
            100m,
            PostNow: true,
            [
                new CreateJournalEntryLineCommand(cashAccount.Id, 100m, 0m, null),
                new CreateJournalEntryLineCommand(revenueAccount.Id, 0m, 100m, null)
            ]);

        var ex = await Assert.ThrowsAsync<BusinessRuleException>(() => service.CreateAsync(command));

        Assert.Contains("مقفلة", ex.Message);
    }

    [Fact]
    public async Task CreateDailyCashClosingAsync_CanLockThroughEntryDate()
    {
        var company = new Company("CMP1002", "Branch", "USD");
        var companyId = company.Id;
        var userContext = new TestUserContext { UserId = Guid.NewGuid(), IsAuthenticated = true };
        var clock = new FixedDateTimeProvider(new DateTimeOffset(2026, 3, 28, 10, 0, 0, TimeSpan.Zero));
        var lockRepository = new FakeCompanyJournalLockRepository(company);
        var lockService = new JournalPeriodLockService(lockRepository, userContext, clock);

        var sourceAccount = new Account(companyId, "1301", "الصندوق", null, AccountNature.Debit, true, null, null, "cash", null, true);
        var targetAccount = new Account(companyId, "1310", "الأموال الجاهزة", null, AccountNature.Debit, true, null, null, "treasury", null, true);

        var journalRepository = new FakeJournalEntryRepository
        {
            DailyMovementSummary = new(320m, 0m)
        };

        var createService = new CreateJournalEntryService(
            journalRepository,
            new FakeAccountRepository([sourceAccount, targetAccount]),
            new FakeCompanyCurrencyRepository([new CompanyCurrency(companyId, "USD", "دولار", "USD", "$", 2, 1m, true)]),
            userContext,
            clock,
            lockService);

        var cashClosingService = new CreateDailyCashClosingService(
            journalRepository,
            new FakeAccountRepository([sourceAccount, targetAccount]),
            createService,
            lockService);

        var entryId = await cashClosingService.CreateAsync(new CreateDailyCashClosingCommand(
            companyId,
            new DateOnly(2026, 3, 28),
            sourceAccount.Id,
            targetAccount.Id,
            "CL-01",
            null,
            LockThroughEntryDate: true));

        Assert.NotEqual(Guid.Empty, entryId);
        Assert.Equal(new DateOnly(2026, 3, 28), company.JournalLockedThroughDate);
        Assert.NotNull(journalRepository.AddedEntry);
        Assert.Equal(JournalEntryStatus.Posted, journalRepository.AddedEntry!.Status);
    }
}
