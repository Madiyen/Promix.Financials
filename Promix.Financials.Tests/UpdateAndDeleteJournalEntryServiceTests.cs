using Promix.Financials.Application.Features.Journals.Commands;
using Promix.Financials.Application.Features.Journals.Services;
using Promix.Financials.Application.Features.Parties.Services;
using Promix.Financials.Domain.Accounting;
using Promix.Financials.Domain.Aggregates.Accounts;
using Promix.Financials.Domain.Aggregates.Journals;
using Promix.Financials.Domain.Aggregates.Parties;
using Promix.Financials.Domain.Enums;
using Promix.Financials.Domain.Exceptions;
using Promix.Financials.Domain.Security;
using Promix.Financials.Tests.Support;

namespace Promix.Financials.Tests;

public sealed class UpdateAndDeleteJournalEntryServiceTests
{
    [Fact]
    public async Task UpdateAsync_RejectsNonAdmin()
    {
        var company = new Company("CMP3001", "Main", "USD", new DateOnly(2026, 1, 1));
        var cashAccount = new Account(company.Id, "1301", "الصندوق", null, AccountNature.Debit, true, null, null, "cash", null, true);
        var revenueAccount = new Account(company.Id, "4101", "إيراد خدمات", null, AccountNature.Credit, true, null, null, null, null, true);
        var userContext = new TestUserContext { UserId = Guid.NewGuid(), IsAuthenticated = true };
        var clock = new FixedDateTimeProvider(DateTimeOffset.UtcNow);
        var repository = new FakeJournalEntryRepository();
        var lockService = new JournalPeriodLockService(new FakeCompanyJournalLockRepository(company), userContext, clock);
        var rebuildService = new RebuildPartySettlementsService(new FakePartySettlementRepository(), userContext, clock);

        var entry = CreatePostedReceipt(company.Id, cashAccount.Id, revenueAccount.Id);
        await repository.AddAsync(entry);

        var service = new UpdateJournalEntryService(
            repository,
            new FakeAccountRepository([cashAccount, revenueAccount]),
            new FakePartyRepository(),
            new FakeCompanyCurrencyRepository([new CompanyCurrency(company.Id, "USD", "دولار", "USD", "$", 2, 1m, true)]),
            userContext,
            clock,
            lockService,
            rebuildService);

        var command = new UpdateJournalEntryCommand(
            company.Id,
            entry.Id,
            new DateOnly(2026, 3, 29),
            "RV-UPDATED",
            "قبض معدل",
            "USD",
            1m,
            250m,
            PostNow: false,
            [
                new CreateJournalEntryLineCommand(cashAccount.Id, 250m, 0m, "الحساب النقدي"),
                new CreateJournalEntryLineCommand(revenueAccount.Id, 0m, 250m, "حساب الإيراد", "عميل 1")
            ]);

        var ex = await Assert.ThrowsAsync<BusinessRuleException>(() => service.UpdateAsync(command));

        Assert.Contains("Only Admin", ex.Message);
    }

    [Fact]
    public async Task UpdateAsync_AdminCanUpdatePostedEntry_AndKeepsItPosted()
    {
        var company = new Company("CMP3002", "Main", "USD", new DateOnly(2026, 1, 1));
        var cashAccount = new Account(company.Id, "1301", "الصندوق", null, AccountNature.Debit, true, null, null, "cash", null, true);
        var revenueAccount = new Account(company.Id, "4101", "إيراد خدمات", null, AccountNature.Credit, true, null, null, null, null, true);
        var receivableControlAccount = new Account(company.Id, "1210", "ذمم العملاء", null, AccountNature.Debit, true, null, null, "ARControl", null, true);
        var customerParty = new Party(
            company.Id,
            "PRT0001",
            "عميل نقدي",
            null,
            PartyTypeFlags.Customer,
            PartyLedgerMode.Subledger,
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            true);
        var userContext = new TestUserContext { UserId = Guid.NewGuid(), IsAuthenticated = true };
        userContext.SetRoles("Admin");
        var clock = new FixedDateTimeProvider(new DateTimeOffset(2026, 3, 29, 9, 0, 0, TimeSpan.Zero));
        var repository = new FakeJournalEntryRepository();
        var lockService = new JournalPeriodLockService(new FakeCompanyJournalLockRepository(company), userContext, clock);
        var rebuildService = new RebuildPartySettlementsService(new FakePartySettlementRepository(), userContext, clock);

        var entry = CreatePostedReceipt(company.Id, cashAccount.Id, revenueAccount.Id);
        await repository.AddAsync(entry);

        var service = new UpdateJournalEntryService(
            repository,
            new FakeAccountRepository([cashAccount, revenueAccount, receivableControlAccount]),
            new FakePartyRepository([customerParty]),
            new FakeCompanyCurrencyRepository([new CompanyCurrency(company.Id, "USD", "دولار", "USD", "$", 2, 1m, true)]),
            userContext,
            clock,
            lockService,
            rebuildService);

        var command = new UpdateJournalEntryCommand(
            company.Id,
            entry.Id,
            new DateOnly(2026, 3, 29),
            "RV-0001-REV",
            "قبض معدل",
            "USD",
            1m,
            300m,
            PostNow: false,
            [
                new CreateJournalEntryLineCommand(cashAccount.Id, 300m, 0m, "الحساب النقدي"),
                new CreateJournalEntryLineCommand(receivableControlAccount.Id, 0m, 300m, "مقابل القبض", customerParty.NameAr, customerParty.Id)
            ]);

        await service.UpdateAsync(command);

        Assert.Equal(JournalEntryStatus.Posted, entry.Status);
        Assert.Equal(new DateOnly(2026, 3, 29), entry.EntryDate);
        Assert.Equal("RV-0001-REV", entry.ReferenceNo);
        Assert.Equal("قبض معدل", entry.Description);
        Assert.Equal(300m, entry.CurrencyAmount);
        Assert.Equal(userContext.UserId, entry.ModifiedByUserId);
        Assert.Equal(clock.UtcNow, entry.ModifiedAtUtc);
        Assert.Equal(2, entry.Lines.Count);
        Assert.Contains(entry.Lines, x => x.AccountId == receivableControlAccount.Id && x.PartyId == customerParty.Id && x.PartyName == "عميل نقدي" && x.Credit == 300m);
    }

    [Fact]
    public async Task DeleteAsync_RejectsNonAdmin()
    {
        var company = new Company("CMP3003", "Main", "USD", new DateOnly(2026, 1, 1));
        var userContext = new TestUserContext { UserId = Guid.NewGuid(), IsAuthenticated = true };
        var clock = new FixedDateTimeProvider(DateTimeOffset.UtcNow);
        var repository = new FakeJournalEntryRepository();

        var entry = CreatePostedReceipt(company.Id, Guid.NewGuid(), Guid.NewGuid());
        await repository.AddAsync(entry);

        var service = new DeleteJournalEntryService(repository, userContext, clock, new RebuildPartySettlementsService(new FakePartySettlementRepository(), userContext, clock));
        var ex = await Assert.ThrowsAsync<BusinessRuleException>(() => service.DeleteAsync(new DeleteJournalEntryCommand(company.Id, entry.Id)));

        Assert.Contains("Only Admin", ex.Message);
    }

    [Fact]
    public async Task DeleteAsync_AdminSoftDeletesEntry()
    {
        var company = new Company("CMP3004", "Main", "USD", new DateOnly(2026, 1, 1));
        var userContext = new TestUserContext { UserId = Guid.NewGuid(), IsAuthenticated = true };
        userContext.SetRoles("Admin");
        var clock = new FixedDateTimeProvider(new DateTimeOffset(2026, 3, 29, 10, 0, 0, TimeSpan.Zero));
        var repository = new FakeJournalEntryRepository();

        var entry = CreatePostedReceipt(company.Id, Guid.NewGuid(), Guid.NewGuid());
        await repository.AddAsync(entry);

        var service = new DeleteJournalEntryService(repository, userContext, clock, new RebuildPartySettlementsService(new FakePartySettlementRepository(), userContext, clock));
        await service.DeleteAsync(new DeleteJournalEntryCommand(company.Id, entry.Id));

        Assert.True(entry.IsDeleted);
        Assert.Equal(userContext.UserId, entry.DeletedByUserId);
        Assert.Equal(clock.UtcNow, entry.DeletedAtUtc);
    }

    [Fact]
    public async Task UpdateAsync_TransferVoucherSwitchingFromAutomaticToNone_RebuildsPriorSettlementScopes()
    {
        var company = new Company("CMP3005", "Main", "USD", new DateOnly(2026, 1, 1));
        var sourceAccount = new Account(company.Id, "1211", "ذمم عميل أول", null, AccountNature.Debit, true, null, null, null, null, true);
        var targetAccount = new Account(company.Id, "1212", "ذمم عميل ثان", null, AccountNature.Debit, true, null, null, null, null, true);
        var sourceParty = CreateLegacyCustomer(company.Id, "PRT1001", "عميل أول", sourceAccount.Id);
        var targetParty = CreateLegacyCustomer(company.Id, "PRT1002", "عميل ثان", targetAccount.Id);
        var userContext = new TestUserContext { UserId = Guid.NewGuid(), IsAuthenticated = true };
        userContext.SetRoles("Admin");
        var clock = new FixedDateTimeProvider(new DateTimeOffset(2026, 3, 30, 9, 0, 0, TimeSpan.Zero));
        var repository = new FakeJournalEntryRepository();
        var settlementsRepository = new FakePartySettlementRepository();
        var rebuildService = new RebuildPartySettlementsService(settlementsRepository, userContext, clock);
        var lockService = new JournalPeriodLockService(new FakeCompanyJournalLockRepository(company), userContext, clock);

        var entry = CreatePostedTransfer(company.Id, sourceAccount.Id, sourceParty, targetAccount.Id, targetParty, TransferSettlementMode.Automatic);
        await repository.AddAsync(entry);

        var service = new UpdateJournalEntryService(
            repository,
            new FakeAccountRepository([sourceAccount, targetAccount]),
            new FakePartyRepository([sourceParty, targetParty]),
            new FakeCompanyCurrencyRepository([new CompanyCurrency(company.Id, "USD", "دولار", "USD", "$", 2, 1m, true)]),
            userContext,
            clock,
            lockService,
            rebuildService);

        await service.UpdateAsync(new UpdateJournalEntryCommand(
            company.Id,
            entry.Id,
            new DateOnly(2026, 3, 30),
            "TV-REV-01",
            "تحويل معدل",
            "USD",
            1m,
            80m,
            PostNow: false,
            [
                new CreateJournalEntryLineCommand(targetAccount.Id, 80m, 0m, "الجهة المستلمة", targetParty.NameAr, targetParty.Id),
                new CreateJournalEntryLineCommand(sourceAccount.Id, 0m, 80m, "جهة المصدر", sourceParty.NameAr, sourceParty.Id)
            ],
            TransferSettlementMode.None));

        Assert.True(settlementsRepository.GetPostedLedgerLinesCallCount > 0);
        Assert.Equal(TransferSettlementMode.None, entry.TransferSettlementMode);
    }

    private static JournalEntry CreatePostedReceipt(Guid companyId, Guid cashAccountId, Guid counterpartyAccountId)
    {
        var entry = new JournalEntry(
            companyId,
            "RV-0001",
            new DateOnly(2026, 3, 28),
            JournalEntryType.ReceiptVoucher,
            "USD",
            1m,
            200m,
            Guid.NewGuid(),
            DateTimeOffset.UtcNow.AddDays(-1),
            "RV-0001",
            "قبض");

        entry.AddLine(cashAccountId, null, 200m, 0m);
        entry.AddLine(counterpartyAccountId, "عميل سابق", null, 0m, 200m);
        entry.Post(Guid.NewGuid(), DateTimeOffset.UtcNow);
        return entry;
    }

    private static JournalEntry CreatePostedTransfer(
        Guid companyId,
        Guid sourceAccountId,
        Party sourceParty,
        Guid targetAccountId,
        Party targetParty,
        TransferSettlementMode settlementMode)
    {
        var entry = new JournalEntry(
            companyId,
            "TV-0001",
            new DateOnly(2026, 3, 29),
            JournalEntryType.TransferVoucher,
            "USD",
            1m,
            100m,
            Guid.NewGuid(),
            DateTimeOffset.UtcNow.AddDays(-1),
            "TV-0001",
            "تحويل",
            settlementMode);

        entry.AddLine(targetAccountId, targetParty.Id, targetParty.NameAr, "الجهة المستلمة", 100m, 0m);
        entry.AddLine(sourceAccountId, sourceParty.Id, sourceParty.NameAr, "جهة المصدر", 0m, 100m);
        entry.Post(Guid.NewGuid(), DateTimeOffset.UtcNow);
        return entry;
    }

    private static Party CreateLegacyCustomer(Guid companyId, string code, string name, Guid receivableAccountId)
        => new(
            companyId,
            code,
            name,
            null,
            PartyTypeFlags.Customer,
            PartyLedgerMode.LegacyLinkedAccounts,
            receivableAccountId,
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            true);
}
