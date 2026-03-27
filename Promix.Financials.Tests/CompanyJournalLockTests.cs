using Promix.Financials.Domain.Exceptions;
using Promix.Financials.Domain.Security;

namespace Promix.Financials.Tests;

public sealed class CompanyJournalLockTests
{
    [Fact]
    public void EnsureJournalDateIsOpen_ThrowsInsideLockedPeriod()
    {
        var company = new Company("CMP4001", "Main", "USD");
        company.LockJournalThrough(new DateOnly(2026, 3, 31), Guid.NewGuid(), DateTimeOffset.UtcNow);

        var ex = Assert.Throws<BusinessRuleException>(() => company.EnsureJournalDateIsOpen(new DateOnly(2026, 3, 15)));

        Assert.Contains("مقفلة", ex.Message);
    }

    [Fact]
    public void LockJournalThrough_IgnoresEarlierDates()
    {
        var company = new Company("CMP4002", "Main", "USD");
        company.LockJournalThrough(new DateOnly(2026, 4, 10), Guid.NewGuid(), DateTimeOffset.UtcNow);

        company.LockJournalThrough(new DateOnly(2026, 4, 1), Guid.NewGuid(), DateTimeOffset.UtcNow.AddDays(1));

        Assert.Equal(new DateOnly(2026, 4, 10), company.JournalLockedThroughDate);
    }
}
