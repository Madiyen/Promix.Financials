using System;

namespace Promix.Financials.UI.ViewModels.Reports.Models;

public sealed class FinancialYearOptionVm
{
    public FinancialYearOptionVm(Guid? id, string displayText, DateOnly startDate, DateOnly endDate, bool isActive, bool isDerivedFallback)
    {
        Id = id;
        DisplayText = displayText;
        StartDate = startDate;
        EndDate = endDate;
        IsActive = isActive;
        IsDerivedFallback = isDerivedFallback;
    }

    public Guid? Id { get; }
    public string DisplayText { get; }
    public DateOnly StartDate { get; }
    public DateOnly EndDate { get; }
    public bool IsActive { get; }
    public bool IsDerivedFallback { get; }
}
