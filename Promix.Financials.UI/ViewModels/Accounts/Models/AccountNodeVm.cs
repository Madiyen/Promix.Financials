using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Microsoft.UI;
using Microsoft.UI.Xaml.Media;
using Promix.Financials.Domain.Enums;

namespace Promix.Financials.UI.ViewModels.Accounts.Models;

public sealed class AccountNodeVm : INotifyPropertyChanged
{
    private bool _isSelected;

    public AccountNodeVm(
        Guid id,
        string code,
        string arabicName,
        AccountNature nature,
        AccountClass classification,
        AccountCloseBehavior closeBehavior,
        bool isPosting,
        bool allowManualPosting,
        bool allowChildren,
        bool isSystem,
        AccountOrigin origin,
        bool isActive,
        string? currencyCode,
        string? systemRole,
        Guid? parentId = null)
    {
        Id = id;
        ParentId = parentId;
        Code = code;
        ArabicName = arabicName;
        Nature = nature;
        Classification = classification;
        CloseBehavior = closeBehavior;
        IsPosting = isPosting;
        AllowManualPosting = allowManualPosting;
        AllowChildren = allowChildren;
        IsSystem = isSystem;
        Origin = origin;
        IsActive = isActive;
        CurrencyCode = currencyCode;
        SystemRole = systemRole;
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public Guid Id { get; }
    public Guid? ParentId { get; }
    public string Code { get; }
    public string ArabicName { get; }
    public AccountNature Nature { get; }
    public AccountClass Classification { get; }
    public AccountCloseBehavior CloseBehavior { get; }
    public bool IsPosting { get; }
    public bool AllowManualPosting { get; }
    public bool AllowChildren { get; }
    public bool IsSystem { get; }
    public AccountOrigin Origin { get; }
    public bool IsActive { get; }
    public string? CurrencyCode { get; }
    public string? SystemRole { get; }

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
            OnPropertyChanged(nameof(RowForegroundBrush));
            OnPropertyChanged(nameof(CodeForegroundBrush));
            OnPropertyChanged(nameof(NameFontWeight));
        }
    }

    public string TypeText => IsPosting ? "حركي" : "تجميعي";
    public string NatureText => Nature == AccountNature.Debit ? "مدين" : "دائن";
    public string ClassificationText => Classification switch
    {
        AccountClass.Assets => "أصول",
        AccountClass.Liabilities => "خصوم",
        AccountClass.Equity => "حقوق ملكية",
        AccountClass.Revenue => "إيرادات",
        _ => "مصروفات"
    };
    public string CloseBehaviorText => CloseBehavior switch
    {
        AccountCloseBehavior.Permanent => "دائم",
        AccountCloseBehavior.Temporary => "مؤقت",
        _ => "يقفل آخر السنة"
    };
    public string OriginText => Origin switch
    {
        AccountOrigin.Template => "افتراضي",
        AccountOrigin.PartyGenerated => "طرف",
        _ => "يدوي"
    };
    public string StatusText => IsActive ? "نشط" : "موقوف";

    public Brush RowBackgroundBrush => CreateBrush(IsSelected ? "#EAF3FF" : "#F8FAFC");
    public Brush RowBorderBrush => CreateBrush(IsSelected ? "#7CB3FF" : "#E2E8F0");
    public Brush RowForegroundBrush => CreateBrush(IsSelected ? "#163A68" : IsActive ? "#16324F" : "#7A8CA4");
    public Brush CodeForegroundBrush => CreateBrush(IsSelected ? "#1D4ED8" : "#5F728A");
    public Windows.UI.Text.FontWeight NameFontWeight => IsSelected
        ? Microsoft.UI.Text.FontWeights.Bold
        : Microsoft.UI.Text.FontWeights.SemiBold;

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
        _ => "#FFEDD5"
    });
    public Brush ClassificationForegroundBrush => CreateBrush(Classification switch
    {
        AccountClass.Assets => "#166534",
        AccountClass.Liabilities => "#B91C1C",
        AccountClass.Equity => "#6D28D9",
        AccountClass.Revenue => "#1D4ED8",
        _ => "#C2410C"
    });

    public ObservableCollection<AccountNodeVm> Children { get; } = new();
    public bool HasChildren => Children.Count > 0;

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
