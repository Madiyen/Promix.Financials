using Promix.Financials.Domain.Enums;
using System;

namespace Promix.Financials.UI.ViewModels.Ledger.Models;

public sealed class FinancialPeriodRowVm
{
    public FinancialPeriodRowVm(
        Guid id,
        string code,
        string name,
        DateOnly startDate,
        DateOnly endDate,
        FinancialPeriodStatus status,
        bool isAdjustmentPeriod,
        int entryCount)
    {
        Id = id;
        Code = code;
        Name = name;
        StartDate = startDate;
        EndDate = endDate;
        Status = status;
        IsAdjustmentPeriod = isAdjustmentPeriod;
        EntryCount = entryCount;
    }

    public Guid Id { get; }
    public string Code { get; }
    public string Name { get; }
    public DateOnly StartDate { get; }
    public DateOnly EndDate { get; }
    public FinancialPeriodStatus Status { get; }
    public bool IsAdjustmentPeriod { get; }
    public int EntryCount { get; }

    public bool IsClosed => Status == FinancialPeriodStatus.Closed;
    public string DisplayName => $"{Code} - {Name}";
    public string RangeText => $"{StartDate:yyyy-MM-dd} إلى {EndDate:yyyy-MM-dd}";
    public string StatusText => IsClosed ? "مقفلة" : "مفتوحة";
    public string EntryCountText => EntryCount == 0 ? "بدون قيود" : $"{EntryCount} قيد";
    public string AdjustmentText => IsAdjustmentPeriod ? "فترة تسويات" : "فترة تشغيلية";
}
