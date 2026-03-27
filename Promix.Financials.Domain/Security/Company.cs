using Promix.Financials.Domain.Common;
using Promix.Financials.Domain.Exceptions;

namespace Promix.Financials.Domain.Security;

public sealed class Company : Entity<Guid>
{
    public string Code { get; private set; } = default!;
    public string Name { get; private set; } = default!;
    public string BaseCurrency { get; private set; } = "USD";
    public bool IsActive { get; private set; } = true;
    public DateOnly? JournalLockedThroughDate { get; private set; }
    public Guid? JournalLockedByUserId { get; private set; }
    public DateTimeOffset? JournalLockedAtUtc { get; private set; }

    private Company() { }

    public Company(string code, string name, string baseCurrency)
    {
        if (string.IsNullOrWhiteSpace(code))
            throw new BusinessRuleException("Company code is required.");

        if (string.IsNullOrWhiteSpace(name))
            throw new BusinessRuleException("Company name is required.");

        if (string.IsNullOrWhiteSpace(baseCurrency))
            throw new BusinessRuleException("Base currency is required.");

        Id = Guid.NewGuid(); // <-- هذا هو الإصلاح الأهم

        Code = code.Trim();
        Name = name.Trim();
        BaseCurrency = baseCurrency.Trim().ToUpperInvariant();
    }

    public void Deactivate() => IsActive = false;

    public void LockJournalThrough(DateOnly lockedThroughDate, Guid lockedByUserId, DateTimeOffset lockedAtUtc)
    {
        if (lockedByUserId == Guid.Empty)
            throw new BusinessRuleException("LockedByUserId is required.");

        if (JournalLockedThroughDate is not null && lockedThroughDate <= JournalLockedThroughDate.Value)
            return;

        JournalLockedThroughDate = lockedThroughDate;
        JournalLockedByUserId = lockedByUserId;
        JournalLockedAtUtc = lockedAtUtc;
    }

    public void EnsureJournalDateIsOpen(DateOnly entryDate)
    {
        if (JournalLockedThroughDate is not DateOnly lockedThroughDate || entryDate > lockedThroughDate)
            return;

        throw new BusinessRuleException($"الفترة المحاسبية مقفلة حتى {lockedThroughDate:yyyy-MM-dd}. لا يمكن إنشاء أو ترحيل سند داخل فترة مقفلة.");
    }
}
