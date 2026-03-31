using Promix.Financials.Domain.Common;
using Promix.Financials.Domain.Exceptions;

namespace Promix.Financials.Domain.Accounting;

public sealed class FinancialYear : Entity<Guid>
{
    private FinancialYear() { }

    public FinancialYear(Guid companyId, string code, string name, DateOnly startDate, DateOnly endDate, bool isActive)
    {
        if (companyId == Guid.Empty)
            throw new BusinessRuleException("CompanyId is required.");

        if (string.IsNullOrWhiteSpace(code))
            throw new BusinessRuleException("Financial year code is required.");

        if (string.IsNullOrWhiteSpace(name))
            throw new BusinessRuleException("Financial year name is required.");

        if (startDate == default || endDate == default)
            throw new BusinessRuleException("Financial year dates are required.");

        if (endDate < startDate)
            throw new BusinessRuleException("تاريخ نهاية السنة المالية يجب أن يكون بعد تاريخ البداية.");

        Id = Guid.NewGuid();
        CompanyId = companyId;
        Code = code.Trim();
        Name = name.Trim();
        StartDate = startDate;
        EndDate = endDate;
        IsActive = isActive;
    }

    public Guid CompanyId { get; private set; }
    public string Code { get; private set; } = default!;
    public string Name { get; private set; } = default!;
    public DateOnly StartDate { get; private set; }
    public DateOnly EndDate { get; private set; }
    public bool IsActive { get; private set; }

    public void Update(string code, string name, DateOnly startDate, DateOnly endDate)
    {
        if (string.IsNullOrWhiteSpace(code))
            throw new BusinessRuleException("Financial year code is required.");

        if (string.IsNullOrWhiteSpace(name))
            throw new BusinessRuleException("Financial year name is required.");

        if (startDate == default || endDate == default)
            throw new BusinessRuleException("Financial year dates are required.");

        if (endDate < startDate)
            throw new BusinessRuleException("تاريخ نهاية السنة المالية يجب أن يكون بعد تاريخ البداية.");

        Code = code.Trim();
        Name = name.Trim();
        StartDate = startDate;
        EndDate = endDate;
    }

    public void Activate() => IsActive = true;

    public void Deactivate() => IsActive = false;
}
