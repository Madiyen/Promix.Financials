using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using Promix.Financials.Application.Features.FinancialYears.Queries;
using Promix.Financials.Application.Features.Journals.Queries;
using Promix.Financials.UI.ViewModels.Journals.Models;
using Promix.Financials.UI.ViewModels.Reports.Models;

namespace Promix.Financials.UI.ViewModels.Reports;

public sealed class TrialBalanceViewModel : INotifyPropertyChanged
{
    private readonly IJournalEntriesQuery _query;
    private readonly IFinancialYearQuery _financialYearQuery;
    private Guid _companyId;
    private bool _isBusy;
    private string? _errorMessage;
    private DateTimeOffset _fromDate;
    private DateTimeOffset _toDate;
    private FinancialYearOptionVm? _selectedFiscalYear;
    private bool _includeZeroBalanceRows;
    private string _trialBalanceViewModeKey = "selected";
    private DateOnly? _lockedThroughDate;

    public TrialBalanceViewModel(IJournalEntriesQuery query, IFinancialYearQuery financialYearQuery)
    {
        _query = query;
        _financialYearQuery = financialYearQuery;

        var today = DateTime.Today;
        _fromDate = new DateTimeOffset(new DateTime(today.Year, 1, 1));
        _toDate = new DateTimeOffset(today);
        SetBalanceState(isBalanced: true, "متوازن", "الفرق الحالي يساوي صفراً.", "0.00");
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public ObservableCollection<FinancialYearOptionVm> AvailableFiscalYears { get; } = new();
    public ObservableCollection<TrialBalanceRowVm> TrialBalanceRows { get; } = new();

    public DateTimeOffset FromDate
    {
        get => _fromDate;
        set
        {
            if (_fromDate == value) return;
            _fromDate = value;
            OnPropertyChanged();
        }
    }

    public DateTimeOffset ToDate
    {
        get => _toDate;
        set
        {
            if (_toDate == value) return;
            _toDate = value;
            OnPropertyChanged();
        }
    }

    public FinancialYearOptionVm? SelectedFiscalYear
    {
        get => _selectedFiscalYear;
        set
        {
            if (_selectedFiscalYear == value) return;
            _selectedFiscalYear = value;
            OnPropertyChanged();

            if (value is not null)
            {
                FromDate = new DateTimeOffset(value.StartDate.ToDateTime(TimeOnly.MinValue));
                ToDate = new DateTimeOffset(value.EndDate.ToDateTime(TimeOnly.MinValue));
            }
        }
    }

    public bool IncludeZeroBalanceRows
    {
        get => _includeZeroBalanceRows;
        set
        {
            if (_includeZeroBalanceRows == value) return;
            _includeZeroBalanceRows = value;
            OnPropertyChanged();
        }
    }

    public string TrialBalanceViewModeKey
    {
        get => _trialBalanceViewModeKey;
        set
        {
            var normalized = string.IsNullOrWhiteSpace(value) ? "selected" : value.Trim().ToLowerInvariant();
            if (_trialBalanceViewModeKey == normalized) return;
            _trialBalanceViewModeKey = normalized;
            OnPropertyChanged();
            OnPropertyChanged(nameof(IsSelectedPeriodMode));
            OnPropertyChanged(nameof(IsLockedSnapshotMode));
        }
    }

    public bool IsSelectedPeriodMode => TrialBalanceViewModeKey != "locked";
    public bool IsLockedSnapshotMode => TrialBalanceViewModeKey == "locked";

    public bool IsBusy
    {
        get => _isBusy;
        private set
        {
            if (_isBusy == value) return;
            _isBusy = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(CanLoad));
        }
    }

    public string? ErrorMessage
    {
        get => _errorMessage;
        private set
        {
            if (_errorMessage == value) return;
            _errorMessage = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(HasError));
            OnPropertyChanged(nameof(ErrorVisibility));
        }
    }

    public bool HasError => !string.IsNullOrWhiteSpace(ErrorMessage);
    public Visibility ErrorVisibility => HasError ? Visibility.Visible : Visibility.Collapsed;
    public bool CanLoad => !IsBusy;
    public bool CanUseLockedSnapshot => _lockedThroughDate is not null;

    public string PeriodLockSummaryText => _lockedThroughDate is DateOnly lockedThroughDate
        ? $"آخر إقفال مرحل حتى {lockedThroughDate:yyyy-MM-dd}"
        : "لا يوجد إقفال مرحل بعد";

    public string PeriodLockHintText => _lockedThroughDate is DateOnly lockedThroughDate
        ? $"يمكنك تثبيت نهاية الميزان عند {lockedThroughDate:yyyy-MM-dd} أو الاستمرار على الفترة المختارة."
        : "سيبقى العرض على الفترة المختارة فقط إلى أن تُغلق فترة محاسبية واحدة على الأقل.";

    public string TrialBalanceSummaryText { get; private set; } = "سيظهر ميزان المراجعة للفترة المختارة هنا.";
    public string TrialBalanceRowsCountText { get; private set; } = "0 حساب";
    public string TrialBalanceRangeText { get; private set; } = "النطاق المعتمد سيظهر هنا بعد التحميل.";
    public string TrialBalancePeriodStatusText { get; private set; } = "يعتمد العرض حالياً على الفترة المحددة.";
    public string NetBalanceSignHintText => "الصافي الموجب يعني رصيدًا مدينًا، والصافي السالب يعني رصيدًا دائنًا.";
    public string TrialBalanceOpeningTotalsText { get; private set; } = "مدين 0.00 | دائن 0.00";
    public string TrialBalancePeriodTotalsText { get; private set; } = "مدين 0.00 | دائن 0.00";
    public string TrialBalanceClosingTotalsText { get; private set; } = "مدين 0.00 | دائن 0.00";
    public string BalanceStateText { get; private set; } = "متوازن";
    public string BalanceHintText { get; private set; } = "الفرق الحالي يساوي صفراً.";
    public string BalanceDifferenceText { get; private set; } = "0.00";
    public string BalanceDifferenceLabelText => $"الفرق: {BalanceDifferenceText}";
    public Brush BalanceAccentBrush { get; private set; } = JournalActivityBarVm.FromHex("#047857");
    public Brush BalanceBackgroundBrush { get; private set; } = JournalActivityBarVm.FromHex("#ECFDF5");
    public Brush BalanceBorderBrush { get; private set; } = JournalActivityBarVm.FromHex("#A7F3D0");
    public Visibility TrialBalanceVisibility => TrialBalanceRows.Count == 0 ? Visibility.Collapsed : Visibility.Visible;
    public Visibility TrialBalanceEmptyVisibility => TrialBalanceRows.Count == 0 ? Visibility.Visible : Visibility.Collapsed;

    public async Task InitializeAsync(Guid companyId)
    {
        _companyId = companyId;
        await LoadMetadataAsync();

        if (SelectedFiscalYear is null)
            SelectedFiscalYear = AvailableFiscalYears.FirstOrDefault(x => x.IsActive) ?? AvailableFiscalYears.FirstOrDefault();

        await LoadAsync();
    }

    public async Task LoadAsync()
    {
        if (_companyId == Guid.Empty)
            return;

        IsBusy = true;
        ErrorMessage = null;

        try
        {
            NormalizeDates();
            await LoadMetadataAsync();
            await LoadTrialBalanceCoreAsync();
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
            ClearTrialBalance();
        }
        finally
        {
            IsBusy = false;
        }
    }

    public void SetTodayRange()
    {
        var today = DateTime.Today;
        SelectedFiscalYear = null;
        FromDate = new DateTimeOffset(today);
        ToDate = new DateTimeOffset(today);
    }

    public void SetThisMonthRange()
    {
        var today = DateTime.Today;
        SelectedFiscalYear = null;
        FromDate = new DateTimeOffset(new DateTime(today.Year, today.Month, 1));
        ToDate = new DateTimeOffset(today);
    }

    public void SetThisYearRange()
    {
        var today = DateTime.Today;
        var preferredYear = AvailableFiscalYears.FirstOrDefault(x => x.IsActive)
            ?? AvailableFiscalYears.FirstOrDefault(x => x.StartDate.Year == today.Year || x.EndDate.Year == today.Year);

        if (preferredYear is not null)
        {
            SelectedFiscalYear = preferredYear;
            return;
        }

        SelectedFiscalYear = null;
        FromDate = new DateTimeOffset(new DateTime(today.Year, 1, 1));
        ToDate = new DateTimeOffset(new DateTime(today.Year, 12, 31));
    }

    private async Task LoadMetadataAsync()
    {
        var selectedId = SelectedFiscalYear?.Id;
        var selectedLabel = SelectedFiscalYear?.DisplayText;
        var years = await _financialYearQuery.GetSelectableYearsAsync(_companyId);
        var lockInfo = await _query.GetJournalPeriodLockAsync(_companyId);

        AvailableFiscalYears.Clear();
        foreach (var year in years)
            AvailableFiscalYears.Add(new FinancialYearOptionVm(
                year.Id,
                year.DisplayText,
                year.StartDate,
                year.EndDate,
                year.IsActive,
                year.IsDerivedFallback));

        SelectedFiscalYear = AvailableFiscalYears.FirstOrDefault(x => x.Id == selectedId)
            ?? AvailableFiscalYears.FirstOrDefault(x => selectedId is null && x.DisplayText == selectedLabel)
            ?? SelectedFiscalYear;

        _lockedThroughDate = lockInfo.LockedThroughDate;

        if (!CanUseLockedSnapshot && TrialBalanceViewModeKey == "locked")
            TrialBalanceViewModeKey = "selected";

        OnPropertyChanged(nameof(AvailableFiscalYears));
        OnPropertyChanged(nameof(CanUseLockedSnapshot));
        OnPropertyChanged(nameof(PeriodLockSummaryText));
        OnPropertyChanged(nameof(PeriodLockHintText));
    }

    private async Task LoadTrialBalanceCoreAsync()
    {
        var effectiveRange = ResolveEffectiveRange();
        var rows = await _query.GetTrialBalanceAsync(
            _companyId,
            effectiveRange.FromDate,
            effectiveRange.ToDate,
            IncludeZeroBalanceRows);

        TrialBalanceRows.Clear();
        foreach (var row in rows)
        {
            TrialBalanceRows.Add(new TrialBalanceRowVm(
                row.AccountId,
                row.Code,
                row.NameAr,
                row.Nature,
                row.OpeningDebit,
                row.OpeningCredit,
                row.PeriodDebit,
                row.PeriodCredit,
                row.ClosingDebit,
                row.ClosingCredit));
        }

        var openingDebitTotal = rows.Sum(x => x.OpeningDebit);
        var openingCreditTotal = rows.Sum(x => x.OpeningCredit);
        var periodDebitTotal = rows.Sum(x => x.PeriodDebit);
        var periodCreditTotal = rows.Sum(x => x.PeriodCredit);
        var closingDebitTotal = rows.Sum(x => x.ClosingDebit);
        var closingCreditTotal = rows.Sum(x => x.ClosingCredit);
        var difference = Math.Abs(closingDebitTotal - closingCreditTotal);

        TrialBalanceRowsCountText = rows.Count == 0 ? "بدون حسابات" : $"{rows.Count} حساب";
        TrialBalanceRangeText = $"النطاق المعتمد: {effectiveRange.FromDate:yyyy-MM-dd} إلى {effectiveRange.ToDate:yyyy-MM-dd}";
        TrialBalanceOpeningTotalsText = $"مدين {openingDebitTotal:N2} | دائن {openingCreditTotal:N2}";
        TrialBalancePeriodTotalsText = $"مدين {periodDebitTotal:N2} | دائن {periodCreditTotal:N2}";
        TrialBalanceClosingTotalsText = $"مدين {closingDebitTotal:N2} | دائن {closingCreditTotal:N2}";
        TrialBalanceSummaryText = rows.Count == 0
            ? "لا توجد حسابات أو حركات مطابقة للنطاق الحالي."
            : $"يعرض الميزان {rows.Count} حساباً مع فصل الرصيد الافتتاحي عن حركة الفترة والرصيد الختامي.";
        TrialBalancePeriodStatusText = BuildPeriodStatusText(effectiveRange);

        if (rows.Count == 0)
        {
            SetBalanceState(isBalanced: true, "لا توجد بيانات", "جرّب توسيع الفترة أو تفعيل الأرصدة الصفرية.", "0.00");
        }
        else
        {
            SetBalanceState(
                isBalanced: difference == 0m,
                difference == 0m ? "الميزان متوازن" : "يوجد فرق يحتاج مراجعة",
                difference == 0m
                    ? "إجمالي الرصيد الختامي المدين يساوي إجمالي الرصيد الختامي الدائن."
                    : "الفرق يجب أن يساوي صفراً. راجع القيود المرحلة أو نطاق العرض المختار.",
                difference.ToString("N2"));
        }

        NotifyState();
    }

    private TrialBalanceEffectiveRange ResolveEffectiveRange()
    {
        var selectedFromDate = DateOnly.FromDateTime(FromDate.Date);
        var selectedToDate = DateOnly.FromDateTime(ToDate.Date);

        if (selectedFromDate > selectedToDate)
            (selectedFromDate, selectedToDate) = (selectedToDate, selectedFromDate);

        if (TrialBalanceViewModeKey != "locked" || _lockedThroughDate is not DateOnly lockedThroughDate)
            return new TrialBalanceEffectiveRange(selectedFromDate, selectedToDate, false, false);

        var effectiveToDate = selectedToDate <= lockedThroughDate ? selectedToDate : lockedThroughDate;
        var startAdjusted = false;
        var endAdjusted = effectiveToDate != selectedToDate;
        var effectiveFromDate = selectedFromDate;

        if (effectiveFromDate > effectiveToDate)
        {
            effectiveFromDate = SelectedFiscalYear?.StartDate ?? new DateOnly(effectiveToDate.Year, 1, 1);
            startAdjusted = true;
        }

        return new TrialBalanceEffectiveRange(effectiveFromDate, effectiveToDate, startAdjusted, endAdjusted);
    }

    private string BuildPeriodStatusText(TrialBalanceEffectiveRange range)
    {
        if (_lockedThroughDate is not DateOnly lockedThroughDate)
            return "يعتمد العرض على الفترة المحددة مباشرة لأن الشركة لم تُغلق أي فترة بعد.";

        if (TrialBalanceViewModeKey != "locked")
        {
            if (range.ToDate <= lockedThroughDate)
                return $"الفترة الحالية تقع بالكامل داخل نطاق مغلق حتى {lockedThroughDate:yyyy-MM-dd}.";

            if (range.FromDate <= lockedThroughDate)
                return $"الفترة تمتد بعد آخر إقفال. آخر إقفال مرحل حتى {lockedThroughDate:yyyy-MM-dd}.";

            return $"الفترة الحالية ما زالت مفتوحة. آخر إقفال مرحل حتى {lockedThroughDate:yyyy-MM-dd}.";
        }

        if (range.StartAdjusted)
            return $"بدأ العرض من {range.FromDate:yyyy-MM-dd} لأن الفترة المختارة تبدأ بعد آخر إقفال ({lockedThroughDate:yyyy-MM-dd}).";

        if (range.EndAdjusted)
            return $"تم تثبيت نهاية الميزان عند آخر إقفال مرحل في {lockedThroughDate:yyyy-MM-dd}.";

        return $"العرض الحالي مجمّد حتى آخر إقفال مرحل في {lockedThroughDate:yyyy-MM-dd}.";
    }

    private void SetBalanceState(bool isBalanced, string title, string hint, string differenceText)
    {
        BalanceStateText = title;
        BalanceHintText = hint;
        BalanceDifferenceText = differenceText;
        BalanceAccentBrush = JournalActivityBarVm.FromHex(isBalanced ? "#047857" : "#B91C1C");
        BalanceBackgroundBrush = JournalActivityBarVm.FromHex(isBalanced ? "#ECFDF5" : "#FEF2F2");
        BalanceBorderBrush = JournalActivityBarVm.FromHex(isBalanced ? "#A7F3D0" : "#FECACA");
    }

    private void ClearTrialBalance()
    {
        TrialBalanceRows.Clear();
        TrialBalanceSummaryText = "سيظهر ميزان المراجعة للفترة المختارة هنا.";
        TrialBalanceRowsCountText = "0 حساب";
        TrialBalanceRangeText = "النطاق المعتمد سيظهر هنا بعد التحميل.";
        TrialBalancePeriodStatusText = "يعتمد العرض حالياً على الفترة المحددة.";
        TrialBalanceOpeningTotalsText = "مدين 0.00 | دائن 0.00";
        TrialBalancePeriodTotalsText = "مدين 0.00 | دائن 0.00";
        TrialBalanceClosingTotalsText = "مدين 0.00 | دائن 0.00";
        SetBalanceState(isBalanced: true, "متوازن", "الفرق الحالي يساوي صفراً.", "0.00");
        NotifyState();
    }

    private void NormalizeDates()
    {
        if (FromDate.Date <= ToDate.Date)
            return;

        var swap = FromDate;
        FromDate = ToDate;
        ToDate = swap;
    }

    private void NotifyState()
    {
        OnPropertyChanged(nameof(TrialBalanceSummaryText));
        OnPropertyChanged(nameof(TrialBalanceRowsCountText));
        OnPropertyChanged(nameof(TrialBalanceRangeText));
        OnPropertyChanged(nameof(TrialBalancePeriodStatusText));
        OnPropertyChanged(nameof(TrialBalanceOpeningTotalsText));
        OnPropertyChanged(nameof(TrialBalancePeriodTotalsText));
        OnPropertyChanged(nameof(TrialBalanceClosingTotalsText));
        OnPropertyChanged(nameof(BalanceStateText));
        OnPropertyChanged(nameof(BalanceHintText));
        OnPropertyChanged(nameof(BalanceDifferenceText));
        OnPropertyChanged(nameof(BalanceDifferenceLabelText));
        OnPropertyChanged(nameof(BalanceAccentBrush));
        OnPropertyChanged(nameof(BalanceBackgroundBrush));
        OnPropertyChanged(nameof(BalanceBorderBrush));
        OnPropertyChanged(nameof(TrialBalanceVisibility));
        OnPropertyChanged(nameof(TrialBalanceEmptyVisibility));
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

    private sealed record TrialBalanceEffectiveRange(
        DateOnly FromDate,
        DateOnly ToDate,
        bool StartAdjusted,
        bool EndAdjusted);
}
