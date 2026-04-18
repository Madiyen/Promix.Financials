using System;
using System.ComponentModel;
using System.Globalization;
using System.Runtime.CompilerServices;
using Microsoft.UI;
using Microsoft.UI.Xaml.Media;
using Promix.Financials.Domain.Enums;

namespace Promix.Financials.UI.ViewModels.Accounts.Models;

public sealed class AccountListRowVm : INotifyPropertyChanged
{
    private bool _isSelected;

    public Guid Id { get; init; }
    public Guid? ParentId { get; init; }
    public string ParentCode { get; init; } = "—";
    public string ParentName { get; init; } = "—";
    public string Code { get; init; } = "—";
    public string ArabicName { get; init; } = "—";
    public AccountNature Nature { get; init; }
    public AccountClass Classification { get; init; }
    public AccountCloseBehavior CloseBehavior { get; init; }
    public AccountOrigin Origin { get; init; }
    public bool IsPosting { get; init; }
    public bool AllowManualPosting { get; init; }
    public bool AllowChildren { get; init; }
    public bool IsSystem { get; init; }
    public bool IsActive { get; init; }
    public string? CurrencyCode { get; init; }
    public string? SystemRole { get; init; }
    public decimal Balance { get; init; }
    public DateOnly? LastMovementDate { get; init; }
    public int ChildAccountsCount { get; init; }

    public bool IsSelected
    {
        get => _isSelected;
        set
        {
            if (_isSelected == value) return;
            _isSelected = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(RowBackgroundBrush));
            OnPropertyChanged(nameof(RowBorderBrush));
            OnPropertyChanged(nameof(TitleForegroundBrush));
        }
    }

    public string ClassificationText => Classification switch
    {
        AccountClass.Assets => "أصول",
        AccountClass.Liabilities => "خصوم",
        AccountClass.Equity => "حقوق ملكية",
        AccountClass.Revenue => "إيرادات",
        _ => "مصروفات"
    };

    public string TypeText => IsPosting ? "حركي" : "تجميعي";

    public string OriginText => Origin switch
    {
        AccountOrigin.Template => "افتراضي",
        AccountOrigin.PartyGenerated => "طرف",
        _ => "يدوي"
    };

    public string StatusText => IsActive ? "نشط" : "موقوف";

    public string BalanceText => Math.Abs(Balance).ToString("#,##0.00", CultureInfo.InvariantCulture);

    public string BalanceSideText => Balance switch
    {
        > 0m => "مدين",
        < 0m => "دائن",
        _ => "—"
    };

    public string LastMovementText => LastMovementDate is null
        ? "لا توجد حركة"
        : $"آخر حركة: {LastMovementDate:yyyy-MM-dd}";

    public Brush RowBackgroundBrush => CreateBrush(IsSelected ? "#EEF5FF" : "#FFFFFF");
    public Brush RowBorderBrush => CreateBrush(IsSelected ? "#7CB3FF" : "#E2E8F0");
    public Brush TitleForegroundBrush => CreateBrush(IsSelected ? "#163A68" : "#16324F");
    public Brush OriginSurfaceBrush => CreateBrush(Origin switch
    {
        AccountOrigin.Template => "#E0E7FF",
        AccountOrigin.PartyGenerated => "#DCFCE7",
        _ => "#F1F5F9"
    });
    public Brush OriginForegroundBrush => CreateBrush(Origin switch
    {
        AccountOrigin.Template => "#3730A3",
        AccountOrigin.PartyGenerated => "#166534",
        _ => "#334155"
    });
    public Brush StatusSurfaceBrush => CreateBrush(IsActive ? "#ECFDF5" : "#FEF2F2");
    public Brush StatusForegroundBrush => CreateBrush(IsActive ? "#166534" : "#B91C1C");
    public Brush ClassificationSurfaceBrush => CreateBrush(Classification switch
    {
        AccountClass.Assets => "#DCFCE7",
        AccountClass.Liabilities => "#FEE2E2",
        AccountClass.Equity => "#EDE9FE",
        AccountClass.Revenue => "#DBEAFE",
        _ => "#FEF3C7"
    });
    public Brush ClassificationForegroundBrush => CreateBrush(Classification switch
    {
        AccountClass.Assets => "#166534",
        AccountClass.Liabilities => "#B91C1C",
        AccountClass.Equity => "#6D28D9",
        AccountClass.Revenue => "#1D4ED8",
        _ => "#B45309"
    });
    public Brush BalanceForegroundBrush => CreateBrush(Balance switch
    {
        > 0m => "#1D4ED8",
        < 0m => "#B91C1C",
        _ => "#475569"
    });

    public Brush NatureSurfaceBrush => CreateBrush(Balance switch
    {
        > 0m => "#DBEAFE",
        < 0m => "#FEE2E2",
        _ => "#F1F5F9"
    });

    public Brush NatureForegroundBrush => CreateBrush(Balance switch
    {
        > 0m => "#1D4ED8",
        < 0m => "#B91C1C",
        _ => "#475569"
    });

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

    private static SolidColorBrush CreateBrush(string hex)
    {
        var value = hex.TrimStart('#');
        var color = ColorHelper.FromArgb(
            255,
            Convert.ToByte(value[..2], 16),
            Convert.ToByte(value.Substring(2, 2), 16),
            Convert.ToByte(value.Substring(4, 2), 16));

        return new SolidColorBrush(color);
    }
}
