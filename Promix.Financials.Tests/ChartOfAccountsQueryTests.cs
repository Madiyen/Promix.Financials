using Microsoft.EntityFrameworkCore;
using Promix.Financials.Domain.Aggregates.Accounts;
using Promix.Financials.Domain.Aggregates.Journals;
using Promix.Financials.Domain.Enums;
using Promix.Financials.Domain.Security;
using Promix.Financials.Infrastructure.Persistence;
using Promix.Financials.Infrastructure.Persistence.Queries;

namespace Promix.Financials.Tests;

public sealed class ChartOfAccountsQueryTests
{
    [Fact]
    public async Task GetAccountsWorkspaceAsync_IncludesPartyGeneratedAccountsUnderDirectParent()
    {
        var databaseName = Guid.NewGuid().ToString("N");
        var options = new DbContextOptionsBuilder<PromixDbContext>()
            .UseInMemoryDatabase(databaseName)
            .Options;

        var company = new Company("CMP5001", "Accounts", "USD", new DateOnly(2026, 1, 1));
        var customerParent = new Account(company.Id, "121", "الزبائن", null, AccountNature.Debit, false, null, null, null, null, true, AccountOrigin.Template);
        var vendorParent = new Account(company.Id, "221", "الموردون", null, AccountNature.Credit, false, null, null, null, null, true, AccountOrigin.Template);
        var cashParent = new Account(company.Id, "131", "الأموال الجاهزة", null, AccountNature.Debit, false, null, null, null, null, true, AccountOrigin.Template);
        var customerAccount = new Account(company.Id, "1211", "عميل رقم 1", null, AccountNature.Debit, true, customerParent.Id, null, null, null, true, AccountOrigin.PartyGenerated);
        var vendorAccount = new Account(company.Id, "2211", "مورد رقم 1", null, AccountNature.Credit, true, vendorParent.Id, null, null, null, true, AccountOrigin.PartyGenerated);
        var cashAccount = new Account(company.Id, "1311", "الصندوق الرئيسي", null, AccountNature.Debit, true, cashParent.Id, null, "CashMain", null, true, AccountOrigin.Template);

        await using (var db = new PromixDbContext(options))
        {
            db.Companies.Add(company);
            db.Accounts.AddRange(customerParent, vendorParent, cashParent, customerAccount, vendorAccount, cashAccount);

            var entry = new JournalEntry(
                company.Id,
                "TR-1001",
                new DateOnly(2026, 3, 30),
                JournalEntryType.TransferVoucher,
                "USD",
                1m,
                450m,
                Guid.NewGuid(),
                DateTimeOffset.UtcNow,
                "TEST-COA",
                "حركة اختبار على حسابات الأطراف");

            entry.AddLine(customerAccount.Id, null, "عميل رقم 1", 450m, 0m);
            entry.AddLine(cashAccount.Id, null, "الصندوق الرئيسي", 0m, 450m);
            entry.Post(Guid.NewGuid(), DateTimeOffset.UtcNow);

            db.JournalEntries.Add(entry);
            await db.SaveChangesAsync();
        }

        await using var queryDb = new PromixDbContext(options);
        var query = new ChartOfAccountsQuery(queryDb);
        var workspace = await query.GetAccountsWorkspaceAsync(company.Id);

        var customerRow = Assert.Single(workspace.Rows, x => x.Id == customerAccount.Id);
        var vendorRow = Assert.Single(workspace.Rows, x => x.Id == vendorAccount.Id);

        Assert.Equal("121", customerRow.ParentCode);
        Assert.Equal("الزبائن", customerRow.ParentName);
        Assert.Equal(AccountOrigin.PartyGenerated, customerRow.Origin);
        Assert.Equal(450m, customerRow.Balance);
        Assert.Equal(new DateOnly(2026, 3, 30), customerRow.LastMovementDate);

        Assert.Equal("221", vendorRow.ParentCode);
        Assert.Equal("الموردون", vendorRow.ParentName);
        Assert.Equal(AccountOrigin.PartyGenerated, vendorRow.Origin);
        Assert.Equal(0m, vendorRow.Balance);
        Assert.Null(vendorRow.LastMovementDate);

        Assert.Equal(2, workspace.Summary.PartyAccounts);
        Assert.Contains(workspace.Summary.ClassBreakdown, x => x.Classification == AccountClass.Assets && x.AccountsCount >= 4);
        Assert.Contains(workspace.Rows, x => x.Id == cashAccount.Id && x.ParentCode == "131");
    }
}
