using System;

namespace Promix.Financials.UI.ViewModels.Ledger.Models;

public sealed class FinancialYearRowVm
{
    public FinancialYearRowVm(Guid id, string code, string name, DateOnly startDate, DateOnly endDate, bool isActive)
    {
        Id = id;
        Code = code;
        Name = name;
        StartDate = startDate;
        EndDate = endDate;
        IsActive = isActive;
    }

    public Guid Id { get; }
    public string Code { get; }
    public string Name { get; }
    public DateOnly StartDate { get; }
    public DateOnly EndDate { get; }
    public bool IsActive { get; }

    public string DisplayName => $"{Code} - {Name}";
    public string RangeText => $"{StartDate:yyyy-MM-dd} إلى {EndDate:yyyy-MM-dd}";
    public string StatusText => IsActive ? "نشطة" : "غير نشطة";
}
