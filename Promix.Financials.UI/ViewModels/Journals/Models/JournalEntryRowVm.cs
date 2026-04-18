using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using Promix.Financials.Domain.Enums;
using Windows.UI;

namespace Promix.Financials.UI.ViewModels.Journals.Models;

public sealed class JournalEntryRowVm : INotifyPropertyChanged
{
    private bool _isDetailsLoading;
    private bool _hasLoadedDetails;

    public JournalEntryRowVm(
        Guid id,
        string entryNumber,
        DateOnly entryDate,
        JournalEntryType type,
        JournalEntryStatus status,
        string? referenceNo,
        string? description,
        string currencyCode,
        decimal exchangeRate,
        decimal currencyAmount,
        decimal totalDebit,
        decimal totalCredit,
        int lineCount,
        DateTimeOffset createdAtUtc,
        DateTimeOffset? postedAtUtc,
        DateTimeOffset? modifiedAtUtc)
    {
        Id = id;
        EntryNumber = entryNumber;
        EntryDate = entryDate;
        EntryDateText = entryDate.ToString("yyyy-MM-dd");
        Type = type;
        Status = status;
        ReferenceNo = string.IsNullOrWhiteSpace(referenceNo) ? "—" : referenceNo;
        Description = string.IsNullOrWhiteSpace(description) ? "بدون وصف إضافي" : description;
        CurrencyCode = string.IsNullOrWhiteSpace(currencyCode) ? "—" : currencyCode.Trim().ToUpperInvariant();
        ExchangeRate = exchangeRate <= 0 ? 1m : exchangeRate;
        CurrencyAmount = currencyAmount;
        TotalDebit = totalDebit;
        TotalCredit = totalCredit;
        LineCount = lineCount;
        LineCountText = $"{lineCount} سطر";
        CreatedAtUtc = createdAtUtc;
        PostedAtUtc = postedAtUtc;
        ModifiedAtUtc = modifiedAtUtc;
    }

    public Guid Id { get; }
    public string EntryNumber { get; }
    public DateOnly EntryDate { get; }
    public string EntryDateText { get; }
    public JournalEntryType Type { get; }
    public JournalEntryStatus Status { get; }
    public string ReferenceNo { get; }
    public string Description { get; }
    public string CurrencyCode { get; }
    public decimal ExchangeRate { get; }
    public decimal CurrencyAmount { get; }
    public decimal TotalDebit { get; }
    public decimal TotalCredit { get; }
    public int LineCount { get; }
    public string LineCountText { get; }
    public DateTimeOffset CreatedAtUtc { get; }
    public DateTimeOffset? PostedAtUtc { get; }
    public DateTimeOffset? ModifiedAtUtc { get; }
    public ObservableCollection<JournalEntryLineDetailVm> Details { get; } = new();
    public bool IsDetailsLoading
    {
        get => _isDetailsLoading;
        private set
        {
            if (_isDetailsLoading == value) return;
            _isDetailsLoading = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(DetailsLoadingVisibility));
            OnPropertyChanged(nameof(DetailsVisibility));
            OnPropertyChanged(nameof(DetailsEmptyVisibility));
        }
    }

    public bool HasLoadedDetails
    {
        get => _hasLoadedDetails;
        private set
        {
            if (_hasLoadedDetails == value) return;
            _hasLoadedDetails = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(DetailsLoadingVisibility));
            OnPropertyChanged(nameof(DetailsVisibility));
            OnPropertyChanged(nameof(DetailsEmptyVisibility));
        }
    }

    public bool IsDraft => Status == JournalEntryStatus.Draft;
    public bool IsBalanced => TotalDebit == TotalCredit && TotalDebit > 0;
    public string TotalDebitText => TotalDebit.ToString("N2");
    public string TotalCreditText => TotalCredit.ToString("N2");
    public string DifferenceText => Math.Abs(TotalDebit - TotalCredit).ToString("N2");
    public string ReferenceDisplay => ReferenceNo == "—" ? "بدون رقم مرجعي" : ReferenceNo;
    public string CreatedAtText => CreatedAtUtc.ToLocalTime().ToString("yyyy-MM-dd HH:mm");
    public string CurrencySummaryText => $"{CurrencyCode} · {CurrencyAmount:N2}";
    public string ExchangeRateText => $"سعر الصرف {ExchangeRate:N4}";
    public string PostedAtText => PostedAtUtc?.ToLocalTime().ToString("yyyy-MM-dd HH:mm") ?? "لم يرحل بعد";
    public string ModifiedAtText => ModifiedAtUtc?.ToLocalTime().ToString("yyyy-MM-dd HH:mm") ?? "بدون تعديل";
    public string LifecycleText => ModifiedAtUtc is not null
        ? $"آخر تعديل {ModifiedAtText}"
        : PostedAtUtc is not null
            ? $"تم الترحيل {PostedAtText}"
            : "بانتظار الترحيل";
    public string AuditSummaryText => Status == JournalEntryStatus.Posted
        ? ModifiedAtUtc is not null
            ? $"مرحل • آخر تعديل {ModifiedAtText}"
            : $"مرحل في {PostedAtText}"
        : "مسودة قابلة للمراجعة والتعديل";

    public decimal SignedMovementAmount => Status != JournalEntryStatus.Posted
        ? 0m
        : Type switch
        {
            JournalEntryType.ReceiptVoucher => TotalDebit,
            JournalEntryType.PaymentVoucher => -TotalDebit,
            JournalEntryType.TransferVoucher => 0m,
            JournalEntryType.DailyCashClosing => 0m,
            JournalEntryType.OpeningEntry => 0m,
            JournalEntryType.Adjustment => 0m,
            _ => 0m
        };

    public string TypeText => Type switch
    {
        JournalEntryType.ReceiptVoucher => "سند قبض",
        JournalEntryType.PaymentVoucher => "سند صرف",
        JournalEntryType.TransferVoucher => "تحويل",
        JournalEntryType.OpeningEntry => "قيد افتتاحي",
        JournalEntryType.DailyCashClosing => "إقفال صندوق",
        JournalEntryType.Adjustment => "قيد تسوية",
        _ => "قيد يومية"
    };

    public string TypeGlyph => Type switch
    {
        JournalEntryType.ReceiptVoucher => "\uE8C7",
        JournalEntryType.PaymentVoucher => "\uEAFD",
        JournalEntryType.TransferVoucher => "\uE8AB",
        JournalEntryType.OpeningEntry => "\uE8B8",
        JournalEntryType.DailyCashClosing => "\uE8D4",
        JournalEntryType.Adjustment => "\uE777",
        _ => "\uE8A5"
    };

    public Brush TypeBackgroundBrush => Type switch
    {
        JournalEntryType.ReceiptVoucher => CreateBrush("#F0FDF4"),
        JournalEntryType.PaymentVoucher => CreateBrush("#FEF2F2"),
        JournalEntryType.TransferVoucher => CreateBrush("#EEF2FF"),
        JournalEntryType.OpeningEntry => CreateBrush("#FFFBEB"),
        JournalEntryType.DailyCashClosing => CreateBrush("#F0F9FF"),
        JournalEntryType.Adjustment => CreateBrush("#FFF7ED"),
        _ => CreateBrush("#EFF6FF")
    };

    public Brush TypeForegroundBrush => Type switch
    {
        JournalEntryType.ReceiptVoucher => CreateBrush("#166534"),
        JournalEntryType.PaymentVoucher => CreateBrush("#991B1B"),
        JournalEntryType.TransferVoucher => CreateBrush("#3730A3"),
        JournalEntryType.OpeningEntry => CreateBrush("#92400E"),
        JournalEntryType.DailyCashClosing => CreateBrush("#0C4A6E"),
        JournalEntryType.Adjustment => CreateBrush("#9A3412"),
        _ => CreateBrush("#1D4ED8")
    };

    public string StatusText => Status == JournalEntryStatus.Posted ? "مرحل" : "مسودة";
    public Brush StatusBackgroundBrush => Status == JournalEntryStatus.Posted ? CreateBrush("#F0FDF4") : CreateBrush("#FFFBEB");
    public Brush StatusForegroundBrush => Status == JournalEntryStatus.Posted ? CreateBrush("#166534") : CreateBrush("#92400E");

    public string StatusNoteText => Status == JournalEntryStatus.Posted
        ? "تم ترحيل هذا السند وهو مؤثر على الأرصدة والتقارير. السندات المرحلة للعرض فقط ولا يمكن تعديلها أو حذفها."
        : "السند ما يزال مسودة ويمكن مراجعته وتعديله قبل الترحيل.";

    public string BalanceText => IsBalanced ? "متوازن" : $"فرق {DifferenceText}";
    public Brush BalanceBrush => IsBalanced ? CreateBrush("#16A34A") : CreateBrush("#DC2626");
    public Brush BalanceBackgroundBrush => IsBalanced ? CreateBrush("#F0FDF4") : CreateBrush("#FEF2F2");

    public string MovementText => Type switch
    {
        JournalEntryType.ReceiptVoucher when Status == JournalEntryStatus.Posted => $"+{TotalDebit:N2} تدفق نقدي داخل",
        JournalEntryType.PaymentVoucher when Status == JournalEntryStatus.Posted => $"-{TotalDebit:N2} تدفق نقدي خارج",
        JournalEntryType.TransferVoucher => "تحويل داخلي بلا صافي نقدي",
        JournalEntryType.DailyCashClosing => "نقل صافي التشغيل إلى الخزينة",
        JournalEntryType.OpeningEntry => "إثبات أرصدة افتتاحية",
        JournalEntryType.Adjustment => "تسوية أو تصحيح محاسبي",
        JournalEntryType.DailyJournal => "قيد يومية متعدد الأسطر",
        JournalEntryType.ReceiptVoucher => "سند قبض بانتظار الترحيل",
        JournalEntryType.PaymentVoucher => "سند صرف بانتظار الترحيل",
        _ => "حركة محاسبية"
    };

    public Brush MovementBrush => Type switch
    {
        JournalEntryType.ReceiptVoucher => CreateBrush("#059669"),
        JournalEntryType.PaymentVoucher => CreateBrush("#DC2626"),
        JournalEntryType.TransferVoucher => CreateBrush("#4F46E5"),
        JournalEntryType.DailyCashClosing => CreateBrush("#0369A1"),
        JournalEntryType.OpeningEntry => CreateBrush("#B45309"),
        JournalEntryType.Adjustment => CreateBrush("#EA580C"),
        _ => CreateBrush("#2563EB")
    };

    public Brush MovementBackgroundBrush => Type switch
    {
        JournalEntryType.ReceiptVoucher => CreateBrush("#ECFDF5"),
        JournalEntryType.PaymentVoucher => CreateBrush("#FEF2F2"),
        JournalEntryType.TransferVoucher => CreateBrush("#EEF2FF"),
        JournalEntryType.DailyCashClosing => CreateBrush("#F0F9FF"),
        JournalEntryType.OpeningEntry => CreateBrush("#FFFBEB"),
        JournalEntryType.Adjustment => CreateBrush("#FFF7ED"),
        _ => CreateBrush("#EFF6FF")
    };

    public Visibility DetailsLoadingVisibility => IsDetailsLoading ? Visibility.Visible : Visibility.Collapsed;
    public Visibility DetailsVisibility => Details.Count > 0 ? Visibility.Visible : Visibility.Collapsed;
    public Visibility DetailsEmptyVisibility => !IsDetailsLoading && HasLoadedDetails && Details.Count == 0 ? Visibility.Visible : Visibility.Collapsed;

    public bool MatchesSearch(string? query)
    {
        if (string.IsNullOrWhiteSpace(query))
            return true;

        var normalized = query.Trim();
        return EntryNumber.Contains(normalized, StringComparison.CurrentCultureIgnoreCase)
            || ReferenceNo.Contains(normalized, StringComparison.CurrentCultureIgnoreCase)
            || Description.Contains(normalized, StringComparison.CurrentCultureIgnoreCase)
            || CurrencyCode.Contains(normalized, StringComparison.CurrentCultureIgnoreCase)
            || TypeText.Contains(normalized, StringComparison.CurrentCultureIgnoreCase)
            || EntryDateText.Contains(normalized, StringComparison.CurrentCultureIgnoreCase);
    }

    private static Brush CreateBrush(string hex)
    {
        var raw = hex.TrimStart('#');
        if (raw.Length == 6)
            raw = "FF" + raw;

        return new SolidColorBrush(Color.FromArgb(
            Convert.ToByte(raw[..2], 16),
            Convert.ToByte(raw.Substring(2, 2), 16),
            Convert.ToByte(raw.Substring(4, 2), 16),
            Convert.ToByte(raw.Substring(6, 2), 16)));
    }

    public void BeginDetailsLoading()
    {
        if (HasLoadedDetails || IsDetailsLoading)
            return;

        IsDetailsLoading = true;
    }

    public void SetDetails(IEnumerable<JournalEntryLineDetailVm> details)
    {
        Details.Clear();
        foreach (var detail in details)
            Details.Add(detail);

        HasLoadedDetails = true;
        IsDetailsLoading = false;
        OnPropertyChanged(nameof(DetailsVisibility));
        OnPropertyChanged(nameof(DetailsEmptyVisibility));
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}

