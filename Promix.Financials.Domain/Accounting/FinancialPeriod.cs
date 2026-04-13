using Promix.Financials.Domain.Common;
using Promix.Financials.Domain.Enums;
using Promix.Financials.Domain.Exceptions;

namespace Promix.Financials.Domain.Accounting;

public sealed class FinancialPeriod : Entity<Guid>
{
    private FinancialPeriod() { }

    public FinancialPeriod(
        Guid companyId,
        Guid financialYearId,
        string code,
        string name,
        DateOnly startDate,
        DateOnly endDate,
        FinancialPeriodStatus status = FinancialPeriodStatus.Open,
        bool isAdjustmentPeriod = false)
    {
        if (companyId == Guid.Empty)
            throw new BusinessRuleException("CompanyId is required.");

        if (financialYearId == Guid.Empty)
            throw new BusinessRuleException("FinancialYearId is required.");

        if (string.IsNullOrWhiteSpace(code))
            throw new BusinessRuleException("Financial period code is required.");

        if (string.IsNullOrWhiteSpace(name))
            throw new BusinessRuleException("Financial period name is required.");

        if (startDate == default || endDate == default)
            throw new BusinessRuleException("Financial period dates are required.");

        if (endDate < startDate)
            throw new BusinessRuleException("تاريخ نهاية الفترة يجب أن يكون بعد تاريخ البداية.");

        Id = Guid.NewGuid();
        CompanyId = companyId;
        FinancialYearId = financialYearId;
        Code = code.Trim();
        Name = name.Trim();
        StartDate = startDate;
        EndDate = endDate;
        Status = status;
        IsAdjustmentPeriod = isAdjustmentPeriod;
    }

    public Guid CompanyId { get; private set; }
    public Guid FinancialYearId { get; private set; }
    public string Code { get; private set; } = default!;
    public string Name { get; private set; } = default!;
    public DateOnly StartDate { get; private set; }
    public DateOnly EndDate { get; private set; }
    public FinancialPeriodStatus Status { get; private set; }
    public bool IsAdjustmentPeriod { get; private set; }

    public bool Contains(DateOnly date)
        => date >= StartDate && date <= EndDate;

    public void Close() => Status = FinancialPeriodStatus.Closed;

    public void Reopen() => Status = FinancialPeriodStatus.Open;
}
