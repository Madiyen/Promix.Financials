using Microsoft.EntityFrameworkCore;
using Promix.Financials.Domain.Aggregates.Accounts;
using Promix.Financials.Domain.Aggregates.Journals;
using Promix.Financials.Domain.Aggregates.Parties;
using Promix.Financials.Domain.Enums;
using Promix.Financials.Domain.Security;
using Promix.Financials.Infrastructure.Persistence;
using Promix.Financials.Infrastructure.Persistence.Queries;
using Promix.Financials.Tests.Support;

namespace Promix.Financials.Tests;

public sealed class PartyQueryTests
{
    [Fact]
    public async Task GetStatementAsync_ReturnsPostedPartyMovements()
    {
        var databaseName = Guid.NewGuid().ToString("N");
        var options = new DbContextOptionsBuilder<PromixDbContext>()
            .UseInMemoryDatabase(databaseName)
            .Options;

        var company = new Company("CMP3001", "Parties", "USD", new DateOnly(2026, 1, 1));
        var cashAccount = new Account(company.Id, "1301", "الصندوق", null, AccountNature.Debit, true, null, null, "cash", null, true);
        var receivableAccount = new Account(company.Id, "1211", "ذمم عملاء", null, AccountNature.Debit, true, null, null, "ARControl", null, true);
        var party = new Party(
            company.Id,
            "CUST001",
            "عميل أول",
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
            null);

        await using (var db = new PromixDbContext(options))
        {
            db.Companies.Add(company);
            db.Accounts.AddRange(cashAccount, receivableAccount);
            db.Parties.Add(party);

            var entry = new JournalEntry(
                company.Id,
                "RV-1001",
                new DateOnly(2026, 3, 29),
                JournalEntryType.ReceiptVoucher,
                "USD",
                1m,
                250m,
                Guid.NewGuid(),
                DateTimeOffset.UtcNow,
                "REF-PARTY",
                "قبض من عميل");
            entry.AddLine(cashAccount.Id, null, "النقدية", 250m, 0m);
            entry.AddLine(receivableAccount.Id, party.Id, party.NameAr, "ذمم العميل", 0m, 250m);
            entry.Post(Guid.NewGuid(), DateTimeOffset.UtcNow);

            db.JournalEntries.Add(entry);
            await db.SaveChangesAsync();
        }

        var query = new PartyQuery(new TestDbContextFactory(options));
        var statement = await query.GetStatementAsync(
            company.Id,
            party.Id,
            new DateOnly(2026, 3, 1),
            new DateOnly(2026, 3, 31));

        Assert.NotNull(statement);
        Assert.Equal("CUST001", statement!.Code);
        Assert.Equal(PartyLedgerMode.Subledger, statement.LedgerMode);
        Assert.Single(statement.Movements);
        Assert.Single(statement.OpenItems);
        Assert.Equal(-250m, statement.ClosingBalance);
    }
}
