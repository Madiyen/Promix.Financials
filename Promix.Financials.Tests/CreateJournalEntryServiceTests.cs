using Promix.Financials.Application.Features.Journals.Commands;
using Promix.Financials.Application.Features.Journals.Services;
using Promix.Financials.Application.Features.Parties.Services;
using Promix.Financials.Domain.Accounting;
using Promix.Financials.Domain.Aggregates.Accounts;
using Promix.Financials.Domain.Aggregates.Parties;
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
        var company = new Company("CMP1001", "Main", "USD", new DateOnly(2026, 1, 1));
        company.LockJournalThrough(new DateOnly(2026, 3, 20), Guid.NewGuid(), DateTimeOffset.UtcNow);

        var cashAccount = new Account(company.Id, "1301", "الصندوق", null, AccountNature.Debit, true, null, null, "cash", null, true);
        var revenueAccount = new Account(company.Id, "4101", "إيراد خدمات", null, AccountNature.Credit, true, null, null, null, null, true);
        var userContext = new TestUserContext { UserId = Guid.NewGuid(), IsAuthenticated = true };
        var repository = new FakeJournalEntryRepository();
        var accountRepository = new FakeAccountRepository([cashAccount, revenueAccount]);
        var partyRepository = new FakePartyRepository();
        var currencyRepository = new FakeCompanyCurrencyRepository([new CompanyCurrency(company.Id, "USD", "دولار", "USD", "$", 2, 1m, true)]);
        var clock = new FixedDateTimeProvider(DateTimeOffset.UtcNow);

        var service = CreateService(company, new DateOnly(2026, 3, 15), repository, accountRepository, partyRepository, currencyRepository, userContext, clock);

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
    public async Task CreateAsync_RejectsEntryOutsideActiveFinancialYear()
    {
        var company = new Company("CMP1001B", "Main", "USD", new DateOnly(2026, 1, 1));
        var cashAccount = new Account(company.Id, "1301", "الصندوق", null, AccountNature.Debit, true, null, null, "cash", null, true);
        var revenueAccount = new Account(company.Id, "4101", "إيراد خدمات", null, AccountNature.Credit, true, null, null, null, null, true);
        var userContext = new TestUserContext { UserId = Guid.NewGuid(), IsAuthenticated = true };
        var repository = new FakeJournalEntryRepository();
        var accountRepository = new FakeAccountRepository([cashAccount, revenueAccount]);
        var partyRepository = new FakePartyRepository();
        var currencyRepository = new FakeCompanyCurrencyRepository([new CompanyCurrency(company.Id, "USD", "دولار", "USD", "$", 2, 1m, true)]);
        var clock = new FixedDateTimeProvider(DateTimeOffset.UtcNow);
        var inactiveYear = new FinancialYear(company.Id, "FY-2025", "السنة المالية 2025", new DateOnly(2025, 1, 1), new DateOnly(2025, 12, 31), false);
        var inactivePeriod = inactiveYear.BuildMonthlyPeriods().First();

        var service = CreateService(
            company,
            repository,
            accountRepository,
            partyRepository,
            currencyRepository,
            userContext,
            clock,
            new FakeFinancialYearRepository([inactiveYear]),
            new FakeFinancialPeriodRepository([inactivePeriod]));

        var command = new CreateJournalEntryCommand(
            company.Id,
            new DateOnly(2026, 3, 15),
            JournalEntryType.ReceiptVoucher,
            null,
            "قبض خارج السنة النشطة",
            "USD",
            1m,
            100m,
            PostNow: true,
            [
                new CreateJournalEntryLineCommand(cashAccount.Id, 100m, 0m, null),
                new CreateJournalEntryLineCommand(revenueAccount.Id, 0m, 100m, null)
            ]);

        var ex = await Assert.ThrowsAsync<BusinessRuleException>(() => service.CreateAsync(command));

        Assert.Contains("سنة مالية نشطة", ex.Message);
    }

    [Fact]
    public async Task CreateAsync_RejectsEntryInsideClosedFinancialPeriod()
    {
        var company = new Company("CMP1001C", "Main", "USD", new DateOnly(2026, 1, 1));
        var cashAccount = new Account(company.Id, "1301", "الصندوق", null, AccountNature.Debit, true, null, null, "cash", null, true);
        var revenueAccount = new Account(company.Id, "4101", "إيراد خدمات", null, AccountNature.Credit, true, null, null, null, null, true);
        var userContext = new TestUserContext { UserId = Guid.NewGuid(), IsAuthenticated = true };
        var repository = new FakeJournalEntryRepository();
        var accountRepository = new FakeAccountRepository([cashAccount, revenueAccount]);
        var partyRepository = new FakePartyRepository();
        var currencyRepository = new FakeCompanyCurrencyRepository([new CompanyCurrency(company.Id, "USD", "دولار", "USD", "$", 2, 1m, true)]);
        var clock = new FixedDateTimeProvider(DateTimeOffset.UtcNow);
        var (year, period) = AccountingPostingTestFactory.CreateActiveCalendar(company.Id, new DateOnly(2026, 3, 15));
        period.Close();

        var service = CreateService(
            company,
            repository,
            accountRepository,
            partyRepository,
            currencyRepository,
            userContext,
            clock,
            new FakeFinancialYearRepository([year]),
            new FakeFinancialPeriodRepository([period]));

        var command = new CreateJournalEntryCommand(
            company.Id,
            new DateOnly(2026, 3, 15),
            JournalEntryType.ReceiptVoucher,
            null,
            "قبض داخل فترة مالية مقفلة",
            "USD",
            1m,
            100m,
            PostNow: false,
            [
                new CreateJournalEntryLineCommand(cashAccount.Id, 100m, 0m, null),
                new CreateJournalEntryLineCommand(revenueAccount.Id, 0m, 100m, null)
            ]);

        var ex = await Assert.ThrowsAsync<BusinessRuleException>(() => service.CreateAsync(command));

        Assert.Contains("الفترة المالية", ex.Message);
        Assert.Contains("مقفلة", ex.Message);
    }

    [Fact]
    public async Task CreateDailyCashClosingAsync_CanLockThroughEntryDate()
    {
        var company = new Company("CMP1002", "Branch", "USD", new DateOnly(2026, 1, 1));
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

        var createService = CreateService(
            company,
            new DateOnly(2026, 3, 28),
            journalRepository,
            new FakeAccountRepository([sourceAccount, targetAccount]),
            new FakePartyRepository(),
            new FakeCompanyCurrencyRepository([new CompanyCurrency(companyId, "USD", "دولار", "USD", "$", 2, 1m, true)]),
            userContext,
            clock);

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

    [Fact]
    public async Task CreateAsync_RejectsReceivableControlWithoutParty()
    {
        var company = new Company("CMP1003", "Main", "USD", new DateOnly(2026, 1, 1));
        var cashAccount = new Account(company.Id, "1301", "الصندوق", null, AccountNature.Debit, true, null, null, "cash", null, true);
        var receivableControlAccount = new Account(company.Id, "1210", "ذمم العملاء", null, AccountNature.Debit, true, null, null, "ARControl", null, true);
        var userContext = new TestUserContext { UserId = Guid.NewGuid(), IsAuthenticated = true };
        var clock = new FixedDateTimeProvider(DateTimeOffset.UtcNow);

        var service = CreateService(
            company,
            new DateOnly(2026, 3, 30),
            new FakeJournalEntryRepository(),
            new FakeAccountRepository([cashAccount, receivableControlAccount]),
            new FakePartyRepository(),
            new FakeCompanyCurrencyRepository([new CompanyCurrency(company.Id, "USD", "دولار", "USD", "$", 2, 1m, true)]),
            userContext,
            clock);

        var command = new CreateJournalEntryCommand(
            company.Id,
            new DateOnly(2026, 3, 30),
            JournalEntryType.ReceiptVoucher,
            "RV-CTRL-01",
            "قبض بدون طرف",
            "USD",
            1m,
            100m,
            PostNow: false,
            [
                new CreateJournalEntryLineCommand(cashAccount.Id, 100m, 0m, "الحساب النقدي"),
                new CreateJournalEntryLineCommand(receivableControlAccount.Id, 0m, 100m, "حساب ضبط بدون طرف")
            ]);

        var ex = await Assert.ThrowsAsync<BusinessRuleException>(() => service.CreateAsync(command));

        Assert.Contains("بدون اختيار طرف", ex.Message);
    }

    [Fact]
    public async Task CreateAsync_RejectsLinkedPartyAccountWithoutSelectingTheParty()
    {
        var company = new Company("CMP1004", "Main", "USD", new DateOnly(2026, 1, 1));
        var cashAccount = new Account(company.Id, "1301", "الصندوق", null, AccountNature.Debit, true, null, null, "cash", null, true);
        var legacyReceivableAccount = new Account(company.Id, "1211", "ذمم محمد", null, AccountNature.Debit, true, null, null, null, null, true);
        var legacyParty = new Party(
            company.Id,
            "PRT0009",
            "محمد ميا",
            null,
            PartyTypeFlags.Customer,
            PartyLedgerMode.LegacyLinkedAccounts,
            legacyReceivableAccount.Id,
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            true);
        var userContext = new TestUserContext { UserId = Guid.NewGuid(), IsAuthenticated = true };
        var clock = new FixedDateTimeProvider(DateTimeOffset.UtcNow);

        var service = CreateService(
            company,
            new DateOnly(2026, 3, 30),
            new FakeJournalEntryRepository(),
            new FakeAccountRepository([cashAccount, legacyReceivableAccount]),
            new FakePartyRepository([legacyParty]),
            new FakeCompanyCurrencyRepository([new CompanyCurrency(company.Id, "USD", "دولار", "USD", "$", 2, 1m, true)]),
            userContext,
            clock);

        var command = new CreateJournalEntryCommand(
            company.Id,
            new DateOnly(2026, 3, 30),
            JournalEntryType.ReceiptVoucher,
            "RV-LEG-01",
            "قبض على حساب قديم",
            "USD",
            1m,
            100m,
            PostNow: false,
            [
                new CreateJournalEntryLineCommand(cashAccount.Id, 100m, 0m, "الحساب النقدي"),
                new CreateJournalEntryLineCommand(legacyReceivableAccount.Id, 0m, 100m, "ذمة قديمة", legacyParty.NameAr)
            ]);

        var ex = await Assert.ThrowsAsync<BusinessRuleException>(() => service.CreateAsync(command));

        Assert.Contains("مرتبط بطرف", ex.Message);
    }

    [Fact]
    public async Task CreateAsync_UsesLinkedReceivableAccountWhenLegacyCustomerPartyIsProvided()
    {
        var company = new Company("CMP1005", "Main", "USD", new DateOnly(2026, 1, 1));
        var cashAccount = new Account(company.Id, "1301", "الصندوق", null, AccountNature.Debit, true, null, null, "cash", null, true);
        var linkedReceivableAccount = new Account(company.Id, "1211", "ذمم عميل تجريبي", null, AccountNature.Debit, true, null, null, null, null, true);
        var customerParty = new Party(
            company.Id,
            "PRT0002",
            "عميل تجريبي",
            null,
            PartyTypeFlags.Customer,
            PartyLedgerMode.LegacyLinkedAccounts,
            linkedReceivableAccount.Id,
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            true);
        var repository = new FakeJournalEntryRepository();
        var userContext = new TestUserContext { UserId = Guid.NewGuid(), IsAuthenticated = true };
        var clock = new FixedDateTimeProvider(DateTimeOffset.UtcNow);

        var service = CreateService(
            company,
            new DateOnly(2026, 3, 30),
            repository,
            new FakeAccountRepository([cashAccount, linkedReceivableAccount]),
            new FakePartyRepository([customerParty]),
            new FakeCompanyCurrencyRepository([new CompanyCurrency(company.Id, "USD", "دولار", "USD", "$", 2, 1m, true)]),
            userContext,
            clock);

        var entryId = await service.CreateAsync(new CreateJournalEntryCommand(
            company.Id,
            new DateOnly(2026, 3, 30),
            JournalEntryType.ReceiptVoucher,
            "RV-LNK-01",
            "قبض من عميل مرتبط",
            "USD",
            1m,
            100m,
            PostNow: false,
            [
                new CreateJournalEntryLineCommand(cashAccount.Id, 100m, 0m, "الحساب النقدي"),
                new CreateJournalEntryLineCommand(linkedReceivableAccount.Id, 0m, 100m, "مقابل القبض", customerParty.NameAr, customerParty.Id)
            ]));

        Assert.NotEqual(Guid.Empty, entryId);
        Assert.NotNull(repository.AddedEntry);
        Assert.Contains(
            repository.AddedEntry!.Lines,
            x => x.AccountId == linkedReceivableAccount.Id && x.PartyId == customerParty.Id && x.PartyName == customerParty.NameAr);
    }

    [Fact]
    public async Task CreateAsync_UsesReceivableControlWhenSubledgerCustomerPartyIsProvided()
    {
        var company = new Company("CMP1005", "Main", "USD", new DateOnly(2026, 1, 1));
        var cashAccount = new Account(company.Id, "1301", "الصندوق", null, AccountNature.Debit, true, null, null, "cash", null, true);
        var receivableControlAccount = new Account(company.Id, "1210", "ذمم العملاء", null, AccountNature.Debit, true, null, null, "ARControl", null, true);
        var customerParty = new Party(
            company.Id,
            "PRT0002",
            "عميل تجريبي",
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
        var repository = new FakeJournalEntryRepository();
        var userContext = new TestUserContext { UserId = Guid.NewGuid(), IsAuthenticated = true };
        var clock = new FixedDateTimeProvider(DateTimeOffset.UtcNow);

        var service = CreateService(
            company,
            new DateOnly(2026, 3, 30),
            repository,
            new FakeAccountRepository([cashAccount, receivableControlAccount]),
            new FakePartyRepository([customerParty]),
            new FakeCompanyCurrencyRepository([new CompanyCurrency(company.Id, "USD", "دولار", "USD", "$", 2, 1m, true)]),
            userContext,
            clock);

        var entryId = await service.CreateAsync(new CreateJournalEntryCommand(
            company.Id,
            new DateOnly(2026, 3, 30),
            JournalEntryType.ReceiptVoucher,
            "RV-SUB-01",
            "قبض من عميل",
            "USD",
            1m,
            100m,
            PostNow: false,
            [
                new CreateJournalEntryLineCommand(cashAccount.Id, 100m, 0m, "الحساب النقدي"),
                new CreateJournalEntryLineCommand(receivableControlAccount.Id, 0m, 100m, "مقابل القبض", customerParty.NameAr, customerParty.Id)
            ]));

        Assert.NotEqual(Guid.Empty, entryId);
        Assert.NotNull(repository.AddedEntry);
        Assert.Contains(
            repository.AddedEntry!.Lines,
            x => x.AccountId == receivableControlAccount.Id && x.PartyId == customerParty.Id && x.PartyName == customerParty.NameAr);
    }

    [Fact]
    public async Task CreateAsync_PostNow_AssignsFinancialContextPostingMetadataAndTraceability()
    {
        var company = new Company("CMP1005B", "Main", "USD", new DateOnly(2026, 1, 1));
        var cashAccount = new Account(company.Id, "1301", "الصندوق", null, AccountNature.Debit, true, null, null, "cash", null, true);
        var revenueAccount = new Account(company.Id, "4101", "إيراد خدمات", null, AccountNature.Credit, true, null, null, null, null, true);
        var repository = new FakeJournalEntryRepository { NextNumber = "RV-2026-000001" };
        var userContext = new TestUserContext { UserId = Guid.NewGuid(), IsAuthenticated = true };
        var clock = new FixedDateTimeProvider(new DateTimeOffset(2026, 3, 30, 8, 45, 0, TimeSpan.Zero));
        var sourceDocumentId = Guid.NewGuid();
        var sourceLineId = Guid.NewGuid();
        var (year, period) = AccountingPostingTestFactory.CreateActiveCalendar(company.Id, new DateOnly(2026, 3, 30));

        var service = CreateService(
            company,
            repository,
            new FakeAccountRepository([cashAccount, revenueAccount]),
            new FakePartyRepository(),
            new FakeCompanyCurrencyRepository([new CompanyCurrency(company.Id, "USD", "دولار", "USD", "$", 2, 1m, true)]),
            userContext,
            clock,
            new FakeFinancialYearRepository([year]),
            new FakeFinancialPeriodRepository([period]));

        await service.CreateAsync(new CreateJournalEntryCommand(
            company.Id,
            new DateOnly(2026, 3, 30),
            JournalEntryType.ReceiptVoucher,
            "RV-SRC-01",
            "قبض مع تتبع المصدر",
            "USD",
            1m,
            100m,
            PostNow: true,
            [
                new CreateJournalEntryLineCommand(cashAccount.Id, 100m, 0m, "الحساب النقدي"),
                new CreateJournalEntryLineCommand(revenueAccount.Id, 0m, 100m, "مقابل القبض")
            ],
            SourceDocumentType: SourceDocumentType.ReceiptVoucher,
            SourceDocumentId: sourceDocumentId,
            SourceDocumentNumber: "RCPT-2026-0001",
            SourceLineId: sourceLineId));

        Assert.NotNull(repository.AddedEntry);
        Assert.Equal("RV-2026-000001", repository.AddedEntry!.EntryNumber);
        Assert.Equal(JournalEntryStatus.Posted, repository.AddedEntry.Status);
        Assert.Equal(year.Id, repository.AddedEntry.FinancialYearId);
        Assert.Equal(period.Id, repository.AddedEntry.FinancialPeriodId);
        Assert.Equal(SourceDocumentType.ReceiptVoucher, repository.AddedEntry.SourceDocumentType);
        Assert.Equal(sourceDocumentId, repository.AddedEntry.SourceDocumentId);
        Assert.Equal("RCPT-2026-0001", repository.AddedEntry.SourceDocumentNumber);
        Assert.Equal(sourceLineId, repository.AddedEntry.SourceLineId);
        Assert.Equal(userContext.UserId, repository.AddedEntry.PostedByUserId);
        Assert.Equal(clock.UtcNow, repository.AddedEntry.PostedAtUtc);
    }

    [Fact]
    public async Task CreateAsync_TransferVoucherWithoutAutomaticSettlement_DoesNotRebuildPartySettlements()
    {
        var company = new Company("CMP1006", "Main", "USD", new DateOnly(2026, 1, 1));
        var sourceAccount = new Account(company.Id, "1211", "ذمم عميل أول", null, AccountNature.Debit, true, null, null, null, null, true);
        var targetAccount = new Account(company.Id, "1212", "ذمم عميل ثان", null, AccountNature.Debit, true, null, null, null, null, true);
        var sourceParty = CreateLegacyCustomer(company.Id, "PRT0006", "عميل أول", sourceAccount.Id);
        var targetParty = CreateLegacyCustomer(company.Id, "PRT0007", "عميل ثان", targetAccount.Id);
        var settlementsRepository = new FakePartySettlementRepository();
        var userContext = new TestUserContext { UserId = Guid.NewGuid(), IsAuthenticated = true };
        var clock = new FixedDateTimeProvider(DateTimeOffset.UtcNow);

        var service = CreateService(
            company,
            new DateOnly(2026, 3, 30),
            new FakeJournalEntryRepository(),
            new FakeAccountRepository([sourceAccount, targetAccount]),
            new FakePartyRepository([sourceParty, targetParty]),
            new FakeCompanyCurrencyRepository([new CompanyCurrency(company.Id, "USD", "دولار", "USD", "$", 2, 1m, true)]),
            userContext,
            clock,
            settlementsRepository);

        await service.CreateAsync(new CreateJournalEntryCommand(
            company.Id,
            new DateOnly(2026, 3, 30),
            JournalEntryType.TransferVoucher,
            "TV-0001",
            "نقل رصيد بين عميلين",
            "USD",
            1m,
            100m,
            PostNow: true,
            [
                new CreateJournalEntryLineCommand(targetAccount.Id, 100m, 0m, "الجهة المستلمة", targetParty.NameAr, targetParty.Id),
                new CreateJournalEntryLineCommand(sourceAccount.Id, 0m, 100m, "جهة المصدر", sourceParty.NameAr, sourceParty.Id)
            ],
            TransferSettlementMode.None));

        Assert.Equal(0, settlementsRepository.GetPostedLedgerLinesCallCount);
    }

    [Fact]
    public async Task CreateAsync_TransferVoucherWithAutomaticSettlement_RebuildsPartySettlements()
    {
        var company = new Company("CMP1007", "Main", "USD", new DateOnly(2026, 1, 1));
        var sourceAccount = new Account(company.Id, "1211", "ذمم عميل أول", null, AccountNature.Debit, true, null, null, null, null, true);
        var targetAccount = new Account(company.Id, "1212", "ذمم عميل ثان", null, AccountNature.Debit, true, null, null, null, null, true);
        var sourceParty = CreateLegacyCustomer(company.Id, "PRT0008", "عميل أول", sourceAccount.Id);
        var targetParty = CreateLegacyCustomer(company.Id, "PRT0009", "عميل ثان", targetAccount.Id);
        var settlementsRepository = new FakePartySettlementRepository();
        var userContext = new TestUserContext { UserId = Guid.NewGuid(), IsAuthenticated = true };
        var clock = new FixedDateTimeProvider(DateTimeOffset.UtcNow);

        var service = CreateService(
            company,
            new DateOnly(2026, 3, 30),
            new FakeJournalEntryRepository(),
            new FakeAccountRepository([sourceAccount, targetAccount]),
            new FakePartyRepository([sourceParty, targetParty]),
            new FakeCompanyCurrencyRepository([new CompanyCurrency(company.Id, "USD", "دولار", "USD", "$", 2, 1m, true)]),
            userContext,
            clock,
            settlementsRepository);

        await service.CreateAsync(new CreateJournalEntryCommand(
            company.Id,
            new DateOnly(2026, 3, 30),
            JournalEntryType.TransferVoucher,
            "TV-0002",
            "نقل رصيد بين عميلين",
            "USD",
            1m,
            100m,
            PostNow: true,
            [
                new CreateJournalEntryLineCommand(targetAccount.Id, 100m, 0m, "الجهة المستلمة", targetParty.NameAr, targetParty.Id),
                new CreateJournalEntryLineCommand(sourceAccount.Id, 0m, 100m, "جهة المصدر", sourceParty.NameAr, sourceParty.Id)
            ],
            TransferSettlementMode.Automatic));

        Assert.True(settlementsRepository.GetPostedLedgerLinesCallCount > 0);
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

    private static CreateJournalEntryService CreateService(
        Company company,
        DateOnly entryDate,
        FakeJournalEntryRepository repository,
        FakeAccountRepository accountRepository,
        FakePartyRepository partyRepository,
        FakeCompanyCurrencyRepository currencyRepository,
        TestUserContext userContext,
        FixedDateTimeProvider clock,
        FakePartySettlementRepository? settlementsRepository = null)
    {
        var settlements = new RebuildPartySettlementsService(
            settlementsRepository ?? new FakePartySettlementRepository(),
            userContext,
            clock);

        return new CreateJournalEntryService(
            repository,
            userContext,
            clock,
            AccountingPostingTestFactory.CreatePostingService(
                repository,
                accountRepository,
                partyRepository,
                currencyRepository,
                company,
                entryDate),
            settlements);
    }

    private static CreateJournalEntryService CreateService(
        Company company,
        FakeJournalEntryRepository repository,
        FakeAccountRepository accountRepository,
        FakePartyRepository partyRepository,
        FakeCompanyCurrencyRepository currencyRepository,
        TestUserContext userContext,
        FixedDateTimeProvider clock,
        FakeFinancialYearRepository yearRepository,
        FakeFinancialPeriodRepository periodRepository,
        FakePartySettlementRepository? settlementsRepository = null)
    {
        var settlements = new RebuildPartySettlementsService(
            settlementsRepository ?? new FakePartySettlementRepository(),
            userContext,
            clock);

        var partyRules = new PartyPostingRulesService(accountRepository, partyRepository);
        var guard = new FinancialPeriodGuard(yearRepository, periodRepository, new FakeCompanyJournalLockRepository(company));
        var posting = new AccountingPostingService(repository, accountRepository, currencyRepository, partyRules, guard);

        return new CreateJournalEntryService(repository, userContext, clock, posting, settlements);
    }
}
