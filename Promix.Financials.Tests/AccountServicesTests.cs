using Promix.Financials.Application.Features.Accounts.Commands;
using Promix.Financials.Application.Features.Accounts.Queries;
using Promix.Financials.Application.Features.Accounts.Services;
using Promix.Financials.Domain.Aggregates.Accounts;
using Promix.Financials.Domain.Aggregates.Parties;
using Promix.Financials.Domain.Enums;
using Promix.Financials.Domain.Exceptions;
using Promix.Financials.Tests.Support;

namespace Promix.Financials.Tests;

public sealed class AccountServicesTests
{
    [Fact]
    public async Task CreateAccountService_BlocksDuplicateArabicNameWithinSameCompany()
    {
        var companyId = Guid.NewGuid();
        var existing = new Account(companyId, "1211", "محمد   ميا", null, AccountNature.Debit, true, null, null, null, null, true);
        var accounts = new FakeAccountRepository([existing]);
        var service = new CreateAccountService(accounts);

        var ex = await Assert.ThrowsAsync<BusinessRuleException>(() => service.CreateAsync(new CreateAccountCommand(
            companyId,
            null,
            "1212",
            "  محمد ميا  ",
            null,
            true,
            AccountNature.Debit,
            null,
            null,
            true,
            null)));

        Assert.Contains("بنفس الاسم", ex.Message);
    }

    [Fact]
    public async Task EditAccountService_BlocksDuplicateArabicNameWithinSameCompany()
    {
        var companyId = Guid.NewGuid();
        var left = new Account(companyId, "1211", "الصندوق الرئيسي", null, AccountNature.Debit, true, null, null, null, null, true);
        var right = new Account(companyId, "1212", "المصرف", null, AccountNature.Debit, true, null, null, null, null, true);
        var accounts = new FakeAccountRepository([left, right]);
        var chartQuery = new FakeChartOfAccountsQuery();
        chartQuery.DetailsById[right.Id] = BuildDetail(right);
        var usageRules = new AccountUsageRulesService(chartQuery);
        var service = new EditAccountService(accounts, usageRules);

        var ex = await Assert.ThrowsAsync<BusinessRuleException>(() => service.EditAsync(new EditAccountCommand(
            right.Id,
            companyId,
            " الصندوق   الرئيسي ",
            null,
            true,
            null)));

        Assert.Contains("بنفس الاسم", ex.Message);
    }

    [Fact]
    public async Task DeleteAccountService_BlocksWhenAccountIsLinkedToParty()
    {
        var companyId = Guid.NewGuid();
        var linkedAccount = new Account(companyId, "1211", "ذمم محمد ميا", null, AccountNature.Debit, true, null, null, null, null, true);
        var party = new Party(
            companyId,
            "PRT0001",
            "محمد ميا",
            null,
            PartyTypeFlags.Customer,
            linkedAccount.Id,
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            true);
        var accounts = new FakeAccountRepository([linkedAccount]);
        var chartQuery = new FakeChartOfAccountsQuery();
        chartQuery.DetailsById[linkedAccount.Id] = BuildDetail(
            linkedAccount,
            linkedPartyNames: [party.NameAr],
            blockingReasons:
            [
                $"مربوط بأطراف مثل: {party.NameAr}."
            ],
            canDelete: false,
            canDeactivate: false);
        var usageRules = new AccountUsageRulesService(chartQuery);
        var service = new DeleteAccountService(accounts, usageRules);

        var ex = await Assert.ThrowsAsync<BusinessRuleException>(() => service.DeleteAsync(linkedAccount.Id, companyId));

        Assert.Contains("مربوط بأطراف", ex.Message);
        Assert.Contains("محمد ميا", ex.Message);
    }

    [Fact]
    public async Task DeleteAccountService_DeletesUnlinkedAccountWithoutMovements()
    {
        var companyId = Guid.NewGuid();
        var account = new Account(companyId, "5311", "مصروفات تشغيل", null, AccountNature.Debit, true, null, null, null, null, true);
        var accounts = new FakeAccountRepository([account]);
        var chartQuery = new FakeChartOfAccountsQuery();
        chartQuery.DetailsById[account.Id] = BuildDetail(account);
        var usageRules = new AccountUsageRulesService(chartQuery);
        var service = new DeleteAccountService(accounts, usageRules);

        await service.DeleteAsync(account.Id, companyId);

        var deleted = await accounts.GetByIdAsync(account.Id, companyId);
        Assert.Null(deleted);
    }

    [Fact]
    public async Task EditAccountService_BlocksDeactivationWhenAccountHasNonZeroBalance()
    {
        var companyId = Guid.NewGuid();
        var account = new Account(companyId, "1111", "الصندوق الرئيسي", null, AccountNature.Debit, true, null, null, null, null, true);
        var accounts = new FakeAccountRepository([account]);
        var chartQuery = new FakeChartOfAccountsQuery();
        chartQuery.DetailsById[account.Id] = BuildDetail(
            account,
            currentBalance: 1250m,
            blockingReasons:
            [
                "الرصيد الحالي للحساب ليس صفراً (1,250.00)."
            ],
            canDelete: false,
            canDeactivate: false);
        var usageRules = new AccountUsageRulesService(chartQuery);
        var service = new EditAccountService(accounts, usageRules);

        var ex = await Assert.ThrowsAsync<BusinessRuleException>(() => service.EditAsync(new EditAccountCommand(
            account.Id,
            companyId,
            account.NameAr,
            null,
            false,
            null)));

        Assert.Contains("لا يمكن إيقاف الحساب", ex.Message);
    }

    [Fact]
    public void Account_Constructor_DerivesClassificationAndCloseBehaviorFromCode()
    {
        var companyId = Guid.NewGuid();
        var revenue = new Account(companyId, "411", "إيرادات المبيعات", null, AccountNature.Credit, true, null, null, null, null, true);
        var asset = new Account(companyId, "1111", "الصندوق", null, AccountNature.Debit, true, null, null, null, null, true);

        Assert.Equal(AccountClass.Revenue, revenue.Classification);
        Assert.Equal(AccountCloseBehavior.YearEndClosing, revenue.CloseBehavior);
        Assert.Equal(AccountClass.Assets, asset.Classification);
        Assert.Equal(AccountCloseBehavior.Permanent, asset.CloseBehavior);
    }

    private static AccountDetailDto BuildDetail(
        Account account,
        decimal currentBalance = 0m,
        IReadOnlyList<string>? linkedPartyNames = null,
        IReadOnlyList<string>? blockingReasons = null,
        bool canDelete = true,
        bool canDeactivate = true)
    {
        linkedPartyNames ??= Array.Empty<string>();
        blockingReasons ??= Array.Empty<string>();

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
            account.Code.Count(c => c == '.') + 1,
            null,
            null,
            Array.Empty<AccountChildPreviewDto>(),
            new AccountUsageSummaryDto(
                0,
                0,
                linkedPartyNames.Count,
                currentBalance,
                false,
                false,
                false,
                false,
                linkedPartyNames,
                blockingReasons,
                canDelete,
                canDeactivate));
    }
}
