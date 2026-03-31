using Promix.Financials.Application.Abstractions;
using Promix.Financials.Application.Features.Parties.Commands;
using Promix.Financials.Application.Features.Parties.Queries;
using Promix.Financials.Application.Features.Parties.Services;
using Promix.Financials.Domain.Aggregates.Accounts;
using Promix.Financials.Domain.Aggregates.Parties;
using Promix.Financials.Domain.Enums;
using Promix.Financials.Domain.Exceptions;
using Promix.Financials.Domain.Security;
using Promix.Financials.Tests.Support;

namespace Promix.Financials.Tests;

public sealed class PartyServicesTests
{
    [Fact]
    public async Task CreatePartyService_CreatesCustomerWithLinkedReceivableAccount()
    {
        var companyId = Guid.NewGuid();
        var customerParent = new Account(companyId, "121", "الزبائن", null, AccountNature.Debit, false, null, null, "ARControl", null, true, AccountOrigin.Template);
        var accounts = new FakeAccountRepository([customerParent]);
        var parties = new FakePartyRepository();
        var userContext = new TestUserContext { IsAuthenticated = true, UserId = Guid.NewGuid() };
        var provisioning = new PartyAccountProvisioningService(accounts, parties);
        var service = new CreatePartyService(parties, userContext, provisioning);

        var partyId = await service.CreateAsync(new CreatePartyCommand(
            companyId,
            string.Empty,
            "محمد ميا",
            null,
            PartyTypeFlags.Customer,
            PartyLedgerMode.LegacyLinkedAccounts,
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            null));

        var createdParty = await parties.GetByIdAsync(companyId, partyId);
        Assert.NotNull(createdParty);
        Assert.Equal(PartyLedgerMode.LegacyLinkedAccounts, createdParty!.LedgerMode);
        Assert.NotNull(createdParty.ReceivableAccountId);
        Assert.Null(createdParty.PayableAccountId);

        var createdAccount = await accounts.GetByIdAsync(createdParty.ReceivableAccountId!.Value, companyId);
        Assert.NotNull(createdAccount);
        Assert.Equal(customerParent.Id, createdAccount!.ParentId);
        Assert.Equal(AccountOrigin.PartyGenerated, createdAccount.Origin);
    }

    [Fact]
    public async Task CreatePartyService_CreatesBothTypeWithTwoSeparateAccounts()
    {
        var companyId = Guid.NewGuid();
        var customerParent = new Account(companyId, "121", "الزبائن", null, AccountNature.Debit, false, null, null, "ARControl", null, true, AccountOrigin.Template);
        var vendorParent = new Account(companyId, "221", "الموردون", null, AccountNature.Credit, false, null, null, "APControl", null, true, AccountOrigin.Template);
        var accounts = new FakeAccountRepository([customerParent, vendorParent]);
        var parties = new FakePartyRepository();
        var userContext = new TestUserContext { IsAuthenticated = true, UserId = Guid.NewGuid() };
        var provisioning = new PartyAccountProvisioningService(accounts, parties);
        var service = new CreatePartyService(parties, userContext, provisioning);

        var partyId = await service.CreateAsync(new CreatePartyCommand(
            companyId,
            "PRT9001",
            "طرف مزدوج",
            null,
            PartyTypeFlags.Both,
            PartyLedgerMode.LegacyLinkedAccounts,
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            null));

        var createdParty = await parties.GetByIdAsync(companyId, partyId);
        Assert.NotNull(createdParty);
        Assert.NotNull(createdParty!.ReceivableAccountId);
        Assert.NotNull(createdParty.PayableAccountId);
        Assert.NotEqual(createdParty.ReceivableAccountId, createdParty.PayableAccountId);

        var receivable = await accounts.GetByIdAsync(createdParty.ReceivableAccountId!.Value, companyId);
        var payable = await accounts.GetByIdAsync(createdParty.PayableAccountId!.Value, companyId);

        Assert.Equal("طرف مزدوج - عميل", receivable!.NameAr);
        Assert.Equal("طرف مزدوج - مورد", payable!.NameAr);
    }

    [Fact]
    public async Task CreatePartyService_ReusesMatchingUnlinkedPostingAccountUnderCorrectParent()
    {
        var companyId = Guid.NewGuid();
        var customerParent = new Account(companyId, "121", "الزبائن", null, AccountNature.Debit, false, null, null, "ARControl", null, true, AccountOrigin.Template);
        var reusableAccount = new Account(companyId, "1218", "محمد ميا", null, AccountNature.Debit, true, customerParent.Id, null, null, null, true);
        var accounts = new FakeAccountRepository([customerParent, reusableAccount]);
        var parties = new FakePartyRepository();
        var userContext = new TestUserContext { IsAuthenticated = true, UserId = Guid.NewGuid() };
        var provisioning = new PartyAccountProvisioningService(accounts, parties);
        var service = new CreatePartyService(parties, userContext, provisioning);

        var partyId = await service.CreateAsync(new CreatePartyCommand(
            companyId,
            string.Empty,
            "محمد ميا",
            null,
            PartyTypeFlags.Customer,
            PartyLedgerMode.LegacyLinkedAccounts,
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            null));

        var createdParty = await parties.GetByIdAsync(companyId, partyId);
        Assert.Equal(reusableAccount.Id, createdParty!.ReceivableAccountId);
    }

    [Fact]
    public async Task EditPartyService_BlocksChangingLinkedAccountWhenLegacyPartyHasPostedHistory()
    {
        var company = new Company("CMP", "شركة اختبار", "USD", new DateOnly(2026, 1, 1));
        var customerParent = new Account(company.Id, "121", "الزبائن", null, AccountNature.Debit, false, null, null, "ARControl", null, true, AccountOrigin.Template);
        var oldAccount = new Account(company.Id, "1211", "ذمم قديمة", null, AccountNature.Debit, true, customerParent.Id, null, null, null, true);
        var party = new Party(
            company.Id,
            "PRT0001",
            "محمد ميا",
            null,
            PartyTypeFlags.Customer,
            PartyLedgerMode.LegacyLinkedAccounts,
            oldAccount.Id,
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            true);

        var parties = new FakePartyRepository([party]);
        var accounts = new FakeAccountRepository([customerParent, oldAccount]);
        var query = new FakePartyQuery
        {
            Statement = new PartyStatementDto(
                party.Id,
                party.Code,
                party.NameAr,
                party.TypeFlags,
                party.LedgerMode,
                party.ReceivableAccountId,
                party.PayableAccountId,
                new DateOnly(2026, 1, 1),
                new DateOnly(2026, 3, 30),
                0m,
                100m,
                [new PartyStatementMovementDto(Guid.NewGuid(), Guid.NewGuid(), "RV-0001", new DateOnly(2026, 3, 1), "1211", "ذمم قديمة", 100m, 0m, 100m, null, null)],
                Array.Empty<PartyOpenItemDto>(),
                Array.Empty<PartySettlementDto>(),
                Array.Empty<PartyAgingBucketDto>())
        };
        var userContext = new TestUserContext { IsAuthenticated = true, UserId = Guid.NewGuid() };
        var service = new EditPartyService(
            parties,
            query,
            new FakeCompanyJournalLockRepository(company),
            userContext,
            new FixedDateTimeProvider(new DateTimeOffset(2026, 3, 30, 9, 0, 0, TimeSpan.Zero)),
            new PartyAccountProvisioningService(accounts, parties));

        var ex = await Assert.ThrowsAsync<BusinessRuleException>(() => service.EditAsync(new EditPartyCommand(
            company.Id,
            party.Id,
            party.Code,
            party.NameAr,
            null,
            PartyTypeFlags.Both,
            PartyLedgerMode.LegacyLinkedAccounts,
            null,
            null,
            null,
            null,
            null,
            null,
            true,
            oldAccount.Id,
            null)));

        Assert.Contains("لا يمكن تغيير نوع الطرف", ex.Message);
    }

    [Fact]
    public async Task RebuildPartySettlementsService_RebuildsOldestFirst()
    {
        var partyId = Guid.NewGuid();
        var accountId = Guid.NewGuid();
        var companyId = Guid.NewGuid();
        var userContext = new TestUserContext { IsAuthenticated = true, UserId = Guid.NewGuid() };
        var clock = new FixedDateTimeProvider(DateTimeOffset.UtcNow);
        var repository = new FakePartySettlementRepository();

        repository.LedgerLines.AddRange(
        [
            new PartySettlementLedgerLine(Guid.NewGuid(), partyId, accountId, new DateOnly(2026, 3, 1), "JV-0001", 1, 100m, 0m),
            new PartySettlementLedgerLine(Guid.NewGuid(), partyId, accountId, new DateOnly(2026, 3, 5), "JV-0002", 1, 50m, 0m),
            new PartySettlementLedgerLine(Guid.NewGuid(), partyId, accountId, new DateOnly(2026, 3, 10), "RV-0001", 1, 0m, 120m)
        ]);

        var service = new RebuildPartySettlementsService(repository, userContext, clock);
        await service.RebuildAsync(companyId, [new RebuildPartySettlementsService.PartyAccountScope(partyId, accountId)]);

        Assert.Equal(2, repository.Settlements.Count);
        Assert.Equal(100m, repository.Settlements[0].Amount);
        Assert.Equal(20m, repository.Settlements[1].Amount);
        Assert.Equal(repository.LedgerLines[0].LineId, repository.Settlements[0].DebitLineId);
        Assert.Equal(repository.LedgerLines[1].LineId, repository.Settlements[1].DebitLineId);
        Assert.Equal(repository.LedgerLines[2].LineId, repository.Settlements[0].CreditLineId);
    }

    [Fact]
    public async Task DeactivatePartyService_BlocksWhenPartyHasOutstandingBalance()
    {
        var company = new Company("CMP", "شركة اختبار", "USD", new DateOnly(2026, 1, 1));
        var party = new Party(
            company.Id,
            "PRT0001",
            "عميل نشط",
            null,
            PartyTypeFlags.Customer,
            PartyLedgerMode.LegacyLinkedAccounts,
            Guid.NewGuid(),
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            true);
        var parties = new FakePartyRepository([party]);
        var query = new FakePartyQuery
        {
            Statement = new PartyStatementDto(
                party.Id,
                party.Code,
                party.NameAr,
                party.TypeFlags,
                party.LedgerMode,
                party.ReceivableAccountId,
                party.PayableAccountId,
                new DateOnly(2026, 1, 1),
                new DateOnly(2026, 3, 30),
                0m,
                250m,
                Array.Empty<PartyStatementMovementDto>(),
                Array.Empty<PartyOpenItemDto>(),
                Array.Empty<PartySettlementDto>(),
                Array.Empty<PartyAgingBucketDto>())
        };
        var userContext = new TestUserContext { IsAuthenticated = true, UserId = Guid.NewGuid() };
        var clock = new FixedDateTimeProvider(new DateTimeOffset(2026, 3, 30, 9, 0, 0, TimeSpan.Zero));
        var service = new DeactivatePartyService(
            parties,
            query,
            new FakeCompanyJournalLockRepository(company),
            userContext,
            clock);

        var ex = await Assert.ThrowsAsync<BusinessRuleException>(() =>
            service.DeactivateAsync(new DeactivatePartyCommand(company.Id, party.Id)));

        Assert.Contains("لا يمكن إيقاف التعامل", ex.Message);
        Assert.True(party.IsActive);
    }

    [Fact]
    public async Task ActivatePartyService_ActivatesInactiveParty()
    {
        var companyId = Guid.NewGuid();
        var party = new Party(
            companyId,
            "PRT0002",
            "طرف موقوف",
            null,
            PartyTypeFlags.Vendor,
            PartyLedgerMode.LegacyLinkedAccounts,
            null,
            Guid.NewGuid(),
            null,
            null,
            null,
            null,
            null,
            null,
            false);
        var parties = new FakePartyRepository([party]);
        var userContext = new TestUserContext { IsAuthenticated = true, UserId = Guid.NewGuid() };
        var service = new ActivatePartyService(parties, userContext);

        await service.ActivateAsync(new ActivatePartyCommand(companyId, party.Id));

        Assert.True(party.IsActive);
    }
}
