using Promix.Financials.Domain.Common;
using Promix.Financials.Domain.Enums;

namespace Promix.Financials.Domain.Aggregates.Accounts;

public sealed class Account : AggregateRoot<Guid>  // ✅ تغيّر من Entity<Guid>
{
    private Account() { } // EF

    public Account(
        Guid companyId, string code, string nameAr, string? nameEn,
        AccountNature nature, bool isPosting, Guid? parentId,
        string? currencyCode, string? systemRole, string? notes, bool isActive,
        AccountOrigin origin = AccountOrigin.Manual,
        AccountClass? accountClass = null,
        AccountCloseBehavior? closeBehavior = null,
        bool? allowManualPosting = null,
        bool? allowChildren = null)
    {
        if (companyId == Guid.Empty) throw new Exceptions.BusinessRuleException("CompanyId is required.");
        if (string.IsNullOrWhiteSpace(code)) throw new Exceptions.BusinessRuleException("Account code is required.");
        if (string.IsNullOrWhiteSpace(nameAr)) throw new Exceptions.BusinessRuleException("Arabic name is required.");

        var normalizedCode = code.Trim();
        var normalizedClass = accountClass ?? DeriveAccountClass(normalizedCode, nature);
        var normalizedCloseBehavior = closeBehavior ?? DeriveCloseBehavior(normalizedClass);
        var normalizedAllowChildren = allowChildren ?? !isPosting;
        var normalizedAllowManualPosting = allowManualPosting ?? isPosting;

        if (isPosting && normalizedAllowChildren)
            throw new Exceptions.BusinessRuleException("Postable accounts cannot accept child accounts.");

        if (!isPosting && normalizedAllowManualPosting)
            throw new Exceptions.BusinessRuleException("Group accounts cannot accept manual posting.");

        Id = Guid.NewGuid();
        CompanyId = companyId;
        Code = normalizedCode;
        NameAr = nameAr.Trim();
        NameEn = string.IsNullOrWhiteSpace(nameEn) ? null : nameEn.Trim();
        Nature = nature;
        Classification = normalizedClass;
        CloseBehavior = normalizedCloseBehavior;
        IsPosting = isPosting;
        AllowManualPosting = normalizedAllowManualPosting;
        AllowChildren = normalizedAllowChildren;
        ParentId = parentId;
        CurrencyCode = string.IsNullOrWhiteSpace(currencyCode) ? null : currencyCode.Trim().ToUpperInvariant();
        SystemRole = string.IsNullOrWhiteSpace(systemRole) ? null : systemRole.Trim();
        Notes = string.IsNullOrWhiteSpace(notes) ? null : notes.Trim();
        Origin = origin;
        IsSystem = origin == AccountOrigin.Template || SystemRole is not null;
        IsActive = isActive;
    }

    public Guid CompanyId { get; private set; }
    public string Code { get; private set; } = default!;
    public string NameAr { get; private set; } = default!;
    public string? NameEn { get; private set; }
    public AccountNature Nature { get; private set; }
    public AccountClass Classification { get; private set; }
    public AccountCloseBehavior CloseBehavior { get; private set; }
    public bool IsPosting { get; private set; }
    public bool AllowManualPosting { get; private set; }
    public bool AllowChildren { get; private set; }
    public Guid? ParentId { get; private set; }
    public string? CurrencyCode { get; private set; }
    public string? SystemRole { get; private set; }
    public string? Notes { get; private set; }
    public AccountOrigin Origin { get; private set; }
    public bool IsSystem { get; private set; }
    public bool IsActive { get; private set; }

    public void Update(string nameAr, string? nameEn, bool isActive, string? notes)
    {
        if (string.IsNullOrWhiteSpace(nameAr))
            throw new Exceptions.BusinessRuleException("Arabic name is required.");

        NameAr = nameAr.Trim();
        NameEn = string.IsNullOrWhiteSpace(nameEn) ? null : nameEn.Trim();
        IsActive = isActive;
        Notes = string.IsNullOrWhiteSpace(notes) ? null : notes.Trim();
    }

    public void Deactivate()
    {
        if (IsSystem)
            throw new Exceptions.BusinessRuleException("System accounts cannot be deactivated.");
        IsActive = false;
    }

    public void AssignSystemRole(string? systemRole)
    {
        SystemRole = string.IsNullOrWhiteSpace(systemRole) ? null : systemRole.Trim();
        IsSystem = Origin == AccountOrigin.Template || SystemRole is not null;
    }

    private static AccountClass DeriveAccountClass(string code, AccountNature nature)
    {
        var normalized = code.Trim();
        var root = normalized.FirstOrDefault(char.IsDigit);

        return root switch
        {
            '1' => AccountClass.Assets,
            '2' => AccountClass.Liabilities,
            '3' => AccountClass.Equity,
            '4' => AccountClass.Revenue,
            '5' => AccountClass.Expenses,
            _ => nature == AccountNature.Credit ? AccountClass.Liabilities : AccountClass.Assets
        };
    }

    private static AccountCloseBehavior DeriveCloseBehavior(AccountClass accountClass)
        => accountClass is AccountClass.Revenue or AccountClass.Expenses
            ? AccountCloseBehavior.YearEndClosing
            : AccountCloseBehavior.Permanent;
}
