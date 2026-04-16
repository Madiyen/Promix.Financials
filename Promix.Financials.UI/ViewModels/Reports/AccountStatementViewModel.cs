using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Promix.Financials.Application.Features.FinancialYears.Queries;
using Promix.Financials.Application.Features.Journals.Queries;
using Promix.Financials.Domain.Enums;
using Promix.Financials.UI.ViewModels.Journals.Models;
using Promix.Financials.UI.ViewModels.Reports.Models;

namespace Promix.Financials.UI.ViewModels.Reports;

public sealed class AccountStatementViewModel : INotifyPropertyChanged
{
    private readonly IJournalEntriesQuery _query;
    private readonly IFinancialYearQuery _financialYearQuery;
    private Guid _companyId;
    private Guid? _selectedAccountId;
    private bool _isBusy;
    private string? _errorMessage;
    private string _accountTitleText = "اختر حساباً لعرض كشف الحساب.";
    private string _periodSummaryText = "لا توجد بيانات معروضة بعد.";
    private string _movementsSectionHintText = "كل صف يمثل أثر السند المرحل على الحساب المختار خلال الفترة المحددة.";
    private DateTimeOffset _fromDate;
    private DateTimeOffset _toDate;
    private FinancialYearOptionVm? _selectedFiscalYear;
    private bool _includeZeroBalanceRows;
    private string _trialBalanceViewModeKey = "selected";
    private DateOnly? _lockedThroughDate;

    public AccountStatementViewModel(IJournalEntriesQuery query, IFinancialYearQuery financialYearQuery)
    {
        _query = query;
        _financialYearQuery = financialYearQuery;

        var today = DateTime.Today;
        _fromDate = new DateTimeOffset(new DateTime(today.Year, today.Month, 1));
        _toDate = new DateTimeOffset(today);
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public ObservableCollection<JournalAccountOptionVm> AccountOptions { get; } = new();
    public ObservableCollection<AccountStatementRowVm> Movements { get; } = new();
    public ObservableCollection<CashMovementDayVm> CashMovementDays { get; } = new();
    public ObservableCollection<FinancialYearOptionVm> AvailableFiscalYears { get; } = new();
    public ObservableCollection<TrialBalanceRowVm> TrialBalanceRows { get; } = new();

    public Guid? SelectedAccountId
    {
        get => _selectedAccountId;
        set
        {
            if (_selectedAccountId == value) return;
            _selectedAccountId = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(CanLoad));
            OnPropertyChanged(nameof(CanLaunchVoucher));
            OnPropertyChanged(nameof(VoucherLaunchHintText));
        }
    }

    public DateTimeOffset FromDate
    {
        get => _fromDate;
        set
        {
            if (_fromDate == value) return;
            _fromDate = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(TrialBalanceRangeText));
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
            OnPropertyChanged(nameof(TrialBalanceRangeText));
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
            OnPropertyChanged(nameof(TrialBalanceViewModeText));
            OnPropertyChanged(nameof(CanUseLockedSnapshot));
        }
    }

    public bool IsBusy
    {
        get => _isBusy;
        private set
        {
            if (_isBusy == value) return;
            _isBusy = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(CanLoad));
            OnPropertyChanged(nameof(CanLaunchVoucher));
            OnPropertyChanged(nameof(CanLoadTrialBalance));
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
    public bool CanLoad => SelectedAccountId is not null && !IsBusy;
    public bool CanLoadTrialBalance => !IsBusy;
    public bool CanLaunchVoucher => SelectedAccountId is not null && !IsBusy;
    public bool CanUseLockedSnapshot => _lockedThroughDate is not null;
    public string TrialBalanceViewModeText => TrialBalanceViewModeKey == "locked" ? "حتى آخر إقفال" : "الفترة المحددة";
    public string TrialBalanceRangeText => $"الفترة المختارة: {FromDate:yyyy-MM-dd} إلى {ToDate:yyyy-MM-dd}";
    public string PeriodLockSummaryText => _lockedThroughDate is DateOnly lockedThroughDate
        ? $"آخر إقفال مرحل حتى {lockedThroughDate:yyyy-MM-dd}"
        : "لا يوجد إقفال مرحل بعد";
    public string PeriodLockHintText => _lockedThroughDate is DateOnly lockedThroughDate
        ? $"يمكنك عرض الميزان على الفترة الحالية أو تجميده حتى آخر إقفال ({lockedThroughDate:yyyy-MM-dd})."
        : "الميزان يعرض الفترة المختارة فقط لأن الشركة لم تُقفل أي فترة بعد.";

    public string VoucherLaunchHintText
    {
        get
        {
            if (SelectedAccountOption is null)
                return "اختر حساباً أولاً. بعد ذلك يمكنك إنشاء سند قبض أو صرف مع تعبئة الحساب المختار تلقائياً بالطرف المناسب.";

            if (SelectedAccountOption.IsCashLike)
                return $"سيُستخدم الحساب {SelectedAccountOption.DisplayText} كحساب نقدي في السند، ويمكنك اختيار الطرف المقابل حسب العملية.";

            return $"سيُستخدم الحساب {SelectedAccountOption.DisplayText} كحساب مقابل في السند، وسيُختار الحساب النقدي افتراضياً من الصندوق أو الأموال الجاهزة أو البنك.";
        }
    }

    public string AccountTitleText
    {
        get => _accountTitleText;
        private set
        {
            if (_accountTitleText == value) return;
            _accountTitleText = value;
            OnPropertyChanged();
        }
    }

    public string PeriodSummaryText
    {
        get => _periodSummaryText;
        private set
        {
            if (_periodSummaryText == value) return;
            _periodSummaryText = value;
            OnPropertyChanged();
        }
    }

    public string MovementsSectionHintText
    {
        get => _movementsSectionHintText;
        private set
        {
            if (_movementsSectionHintText == value) return;
            _movementsSectionHintText = value;
            OnPropertyChanged();
        }
    }

    public string OpeningBalanceText { get; private set; } = "—";
    public string PeriodDebitText { get; private set; } = "0.00";
    public string PeriodCreditText { get; private set; } = "0.00";
    public string ClosingBalanceText { get; private set; } = "—";
    public string RowsCountText { get; private set; } = "0 حركة";
    public string AccountNatureText { get; private set; } = "—";
    public string NetPeriodMovementText { get; private set; } = "0.00";
    public string PostedEntriesCountText { get; private set; } = "0 سند";
    public string CashMovementSummaryText { get; private set; } = "سيظهر ملخص النقدية للفترة نفسها هنا.";
    public string CashInflowText { get; private set; } = "0.00";
    public string CashOutflowText { get; private set; } = "0.00";
    public string PeakCashInflowText { get; private set; } = "—";
    public string PeakCashOutflowText { get; private set; } = "—";
    public string CashActiveDaysText { get; private set; } = "بدون نشاط نقدي";
    public string TrialBalanceSummaryText { get; private set; } = "سيظهر ميزان المراجعة للفترة المختارة هنا.";
    public string TrialBalanceRowsCountText { get; private set; } = "0 حساب";
    public string TrialBalanceDebitTotalText { get; private set; } = "0.00";
    public string TrialBalanceCreditTotalText { get; private set; } = "0.00";
    public string TrialBalancePeriodStatusText { get; private set; } = "يعتمد العرض حالياً على الفترة المحددة.";
    public string AccountNatureLabelText => $"طبيعة الحساب: {AccountNatureText}";
    public string NetPeriodMovementLabelText => $"صافي الحركة: {NetPeriodMovementText}";
    public string PeriodNetSummaryText => $"صافي الفترة: {NetPeriodMovementText}";
    public Visibility EmptyStateVisibility => Movements.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
    public Visibility MovementsVisibility => Movements.Count == 0 ? Visibility.Collapsed : Visibility.Visible;
    public Visibility CashMovementVisibility => CashMovementDays.Count == 0 ? Visibility.Collapsed : Visibility.Visible;
    public Visibility CashMovementEmptyVisibility => CashMovementDays.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
    public Visibility TrialBalanceVisibility => TrialBalanceRows.Count == 0 ? Visibility.Collapsed : Visibility.Visible;
    public Visibility TrialBalanceEmptyVisibility => TrialBalanceRows.Count == 0 ? Visibility.Visible : Visibility.Collapsed;

    public async Task InitializeAsync(Guid companyId)
    {
        _companyId = companyId;
        await LoadAccountsAsync();
        await LoadReportMetadataAsync();
        await LoadStatementPageAsync();
    }

    public Task LoadStatementPageAsync() => LoadInternalAsync(loadStatement: true, loadTrialBalance: false);
    public Task LoadStatementAsync() => LoadInternalAsync(loadStatement: true, loadTrialBalance: false);
    public Task LoadTrialBalanceAsync() => LoadInternalAsync(loadStatement: false, loadTrialBalance: true);
    public Task LoadAllAsync() => LoadInternalAsync(loadStatement: true, loadTrialBalance: true);

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

    public async Task OpenAccountStatementFromTrialBalanceAsync(Guid accountId)
    {
        SelectedAccountId = accountId;
        await LoadStatementAsync();
    }

    private async Task LoadInternalAsync(bool loadStatement, bool loadTrialBalance)
    {
        if (_companyId == Guid.Empty)
            return;

        IsBusy = true;
        ErrorMessage = null;

        try
        {
            NormalizeDates();
            await LoadReportMetadataAsync();

            if (loadStatement)
            {
                await LoadStatementCoreAsync();
            }

            if (loadTrialBalance)
                await LoadTrialBalanceCoreAsync();
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;

            if (loadStatement)
                ClearStatement("تعذر تحميل كشف الحساب.");

            if (loadTrialBalance)
                ClearTrialBalance();
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task LoadAccountsAsync()
    {
        AccountOptions.Clear();

        var accounts = await _query.GetPostingAccountsAsync(_companyId);
        foreach (var account in accounts)
            AccountOptions.Add(new JournalAccountOptionVm(account.Id, account.Code, account.NameAr, account.Nature, account.SystemRole, account.IsLegacyPartyLinkedAccount));

        SelectedAccountId = AccountOptions.FirstOrDefault(x => x.IsCashLike)?.Id
            ?? AccountOptions.FirstOrDefault()?.Id;
    }

    private async Task LoadReportMetadataAsync()
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

        OnPropertyChanged(nameof(AvailableFiscalYears));
        OnPropertyChanged(nameof(CanUseLockedSnapshot));
        OnPropertyChanged(nameof(TrialBalanceViewModeText));
        OnPropertyChanged(nameof(PeriodLockSummaryText));
        OnPropertyChanged(nameof(PeriodLockHintText));
    }

    private async Task LoadStatementCoreAsync()
    {
        if (SelectedAccountId is null)
        {
            ClearStatement("اختر حساباً لعرض كشف الحساب.");
            return;
        }

        var statement = await _query.GetAccountStatementAsync(
            _companyId,
            SelectedAccountId.Value,
            DateOnly.FromDateTime(FromDate.Date),
            DateOnly.FromDateTime(ToDate.Date));

        if (statement is null)
        {
            ClearStatement("تعذر تحميل كشف الحساب المحدد.");
            ErrorMessage = "تعذر العثور على بيانات الحساب المطلوب.";
            return;
        }

        AccountTitleText = $"{statement.Code} - {statement.NameAr}";
        PeriodSummaryText = $"من {statement.FromDate:yyyy-MM-dd} إلى {statement.ToDate:yyyy-MM-dd}";
        MovementsSectionHintText = statement.Movements.Count == 0
            ? "لا توجد حركات مرحلة خلال الفترة المحددة؛ يعرض الجدول رصيد أول المدة فقط."
            : "كل صف يمثل أثر السند المرحل على الحساب المختار خلال الفترة المحددة.";

        var openingSigned = GetSignedBalance(statement.OpeningDebit, statement.OpeningCredit, statement.Nature);
        var runningSigned = openingSigned;
        var periodDebit = 0m;
        var periodCredit = 0m;

        Movements.Clear();
        Movements.Add(new AccountStatementRowVm(
            Guid.Empty,
            string.Empty,
            statement.FromDate,
            "رصيد أول المدة",
            null,
            "الرصيد المرحل قبل بداية الفترة",
            0m,
            0m,
            FormatBalance(openingSigned, statement.Nature),
            true));

        foreach (var movement in statement.Movements)
        {
            periodDebit += movement.Debit;
            periodCredit += movement.Credit;
            runningSigned += GetSignedBalance(movement.Debit, movement.Credit, statement.Nature);

            Movements.Add(new AccountStatementRowVm(
                movement.EntryId,
                movement.EntryNumber,
                movement.EntryDate,
                MapEntryType(movement.Type),
                movement.ReferenceNo,
                movement.Description,
                movement.Debit,
                movement.Credit,
                FormatBalance(runningSigned, statement.Nature)));
        }

        OpeningBalanceText = FormatBalance(openingSigned, statement.Nature);
        PeriodDebitText = periodDebit.ToString("N2");
        PeriodCreditText = periodCredit.ToString("N2");
        ClosingBalanceText = FormatBalance(runningSigned, statement.Nature);
        AccountNatureText = statement.Nature == AccountNature.Debit ? "مدينة" : "دائنة";
        NetPeriodMovementText = FormatBalance(GetSignedBalance(periodDebit, periodCredit, statement.Nature), statement.Nature);
        PostedEntriesCountText = $"{statement.Movements.Count} سند مرحل";
        RowsCountText = statement.Movements.Count == 0
            ? "رصيد افتتاحي فقط"
            : $"{statement.Movements.Count} حركة";
        NotifyStatementState();
    }

    private async Task LoadTrialBalanceCoreAsync()
    {
        var effectiveFromDate = DateOnly.FromDateTime(FromDate.Date);
        var effectiveToDate = ResolveTrialBalanceToDate();

        var rows = await _query.GetTrialBalanceAsync(
            _companyId,
            effectiveFromDate,
            effectiveToDate,
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

        TrialBalanceDebitTotalText = rows.Sum(x => x.ClosingDebit).ToString("N2");
        TrialBalanceCreditTotalText = rows.Sum(x => x.ClosingCredit).ToString("N2");
        TrialBalanceRowsCountText = rows.Count == 0 ? "بدون حسابات" : $"{rows.Count} حساب";
        TrialBalanceSummaryText = rows.Count == 0
            ? "لا توجد أرصدة أو حركات مطابقة للفترة المختارة."
            : $"يعرض الميزان {rows.Count} حساب مع تجميع الرصيد الافتتاحي وحركة الفترة والرصيد الختامي.";
        TrialBalancePeriodStatusText = BuildTrialBalancePeriodStatusText(effectiveToDate);
        NotifyTrialBalanceState();
    }

    private async Task LoadCashMovementAsync()
    {
        var series = await _query.GetCashMovementSeriesAsync(
            _companyId,
            DateOnly.FromDateTime(FromDate.Date),
            DateOnly.FromDateTime(ToDate.Date));

        CashMovementDays.Clear();
        foreach (var movement in series.TakeLast(8))
            CashMovementDays.Add(new CashMovementDayVm(movement.EntryDate, movement.NetMovement));

        var totalInflow = series
            .Where(x => x.NetMovement > 0m)
            .Sum(x => x.NetMovement);

        var totalOutflow = series
            .Where(x => x.NetMovement < 0m)
            .Sum(x => Math.Abs(x.NetMovement));

        var strongestInflow = series
            .Where(x => x.NetMovement > 0m)
            .OrderByDescending(x => x.NetMovement)
            .FirstOrDefault();

        var strongestOutflow = series
            .Where(x => x.NetMovement < 0m)
            .OrderBy(x => x.NetMovement)
            .FirstOrDefault();

        CashInflowText = totalInflow.ToString("N2");
        CashOutflowText = totalOutflow.ToString("N2");
        PeakCashInflowText = strongestInflow is null
            ? "—"
            : $"{strongestInflow.EntryDate:yyyy-MM-dd} | {strongestInflow.NetMovement:N2}";
        PeakCashOutflowText = strongestOutflow is null
            ? "—"
            : $"{strongestOutflow.EntryDate:yyyy-MM-dd} | {Math.Abs(strongestOutflow.NetMovement):N2}";
        CashActiveDaysText = series.Count == 0 ? "بدون نشاط نقدي" : $"{series.Count} يوم نشط";
        CashMovementSummaryText = series.Count switch
        {
            0 => "لا توجد حركة نقدية مرحلة خلال الفترة المحددة.",
            <= 8 => "ملخص النقدية خلال نفس الفترة المحددة في كشف الحساب.",
            _ => $"يعرض الشريط آخر 8 أيام نقدية من أصل {series.Count} يوم خلال الفترة المحددة."
        };

        NotifyCashMovementState();
    }

    private DateOnly ResolveTrialBalanceToDate()
    {
        var selectedToDate = DateOnly.FromDateTime(ToDate.Date);

        if (TrialBalanceViewModeKey != "locked" || _lockedThroughDate is not DateOnly lockedThroughDate)
            return selectedToDate;

        return selectedToDate <= lockedThroughDate ? selectedToDate : lockedThroughDate;
    }

    private string BuildTrialBalancePeriodStatusText(DateOnly effectiveToDate)
    {
        if (_lockedThroughDate is not DateOnly lockedThroughDate)
            return "يعتمد العرض حالياً على الفترة المحددة فقط لأن الشركة لم تُغلق أي فترة بعد.";

        if (TrialBalanceViewModeKey == "locked")
        {
            var label = effectiveToDate == lockedThroughDate ? "حتى آخر إقفال مرحل" : "حتى نهاية الفترة المحددة لأنها أقدم من آخر إقفال";
            return $"عرض {label}: {effectiveToDate:yyyy-MM-dd}.";
        }

        var selectedFromDate = DateOnly.FromDateTime(FromDate.Date);
        if (effectiveToDate <= lockedThroughDate)
            return $"الفترة المحددة تقع بالكامل داخل فترة مغلقة حتى {lockedThroughDate:yyyy-MM-dd}.";

        if (selectedFromDate <= lockedThroughDate)
            return $"الفترة المحددة تمتد بعد آخر إقفال. المقفل حتى {lockedThroughDate:yyyy-MM-dd}.";

        return $"الفترة المحددة ما زالت مفتوحة. آخر إقفال مرحل حتى {lockedThroughDate:yyyy-MM-dd}.";
    }

    private void ClearStatement(string message)
    {
        AccountTitleText = message;
        PeriodSummaryText = "لا توجد بيانات معروضة بعد.";
        MovementsSectionHintText = "كل صف يمثل أثر السند المرحل على الحساب المختار خلال الفترة المحددة.";
        Movements.Clear();
        OpeningBalanceText = "—";
        PeriodDebitText = "0.00";
        PeriodCreditText = "0.00";
        ClosingBalanceText = "—";
        AccountNatureText = "—";
        NetPeriodMovementText = "0.00";
        PostedEntriesCountText = "0 سند";
        RowsCountText = "0 حركة";
        ClearCashMovement();
        NotifyStatementState();
    }

    private void ClearTrialBalance()
    {
        TrialBalanceRows.Clear();
        TrialBalanceSummaryText = "سيظهر ميزان المراجعة للفترة المختارة هنا.";
        TrialBalanceRowsCountText = "0 حساب";
        TrialBalanceDebitTotalText = "0.00";
        TrialBalanceCreditTotalText = "0.00";
        TrialBalancePeriodStatusText = "يعتمد العرض حالياً على الفترة المحددة.";
        NotifyTrialBalanceState();
    }

    private void ClearCashMovement()
    {
        CashMovementDays.Clear();
        CashMovementSummaryText = "سيظهر ملخص النقدية للفترة نفسها هنا.";
        CashInflowText = "0.00";
        CashOutflowText = "0.00";
        PeakCashInflowText = "—";
        PeakCashOutflowText = "—";
        CashActiveDaysText = "بدون نشاط نقدي";
        NotifyCashMovementState();
    }

    private void NormalizeDates()
    {
        if (FromDate.Date <= ToDate.Date)
            return;

        var swap = FromDate;
        FromDate = ToDate;
        ToDate = swap;
    }

    private void NotifyStatementState()
    {
        OnPropertyChanged(nameof(OpeningBalanceText));
        OnPropertyChanged(nameof(PeriodDebitText));
        OnPropertyChanged(nameof(PeriodCreditText));
        OnPropertyChanged(nameof(ClosingBalanceText));
        OnPropertyChanged(nameof(AccountNatureText));
        OnPropertyChanged(nameof(NetPeriodMovementText));
        OnPropertyChanged(nameof(PostedEntriesCountText));
        OnPropertyChanged(nameof(MovementsSectionHintText));
        OnPropertyChanged(nameof(AccountNatureLabelText));
        OnPropertyChanged(nameof(NetPeriodMovementLabelText));
        OnPropertyChanged(nameof(PeriodNetSummaryText));
        OnPropertyChanged(nameof(RowsCountText));
        OnPropertyChanged(nameof(EmptyStateVisibility));
        OnPropertyChanged(nameof(MovementsVisibility));
        OnPropertyChanged(nameof(VoucherLaunchHintText));
    }

    private void NotifyCashMovementState()
    {
        OnPropertyChanged(nameof(CashMovementSummaryText));
        OnPropertyChanged(nameof(CashInflowText));
        OnPropertyChanged(nameof(CashOutflowText));
        OnPropertyChanged(nameof(PeakCashInflowText));
        OnPropertyChanged(nameof(PeakCashOutflowText));
        OnPropertyChanged(nameof(CashActiveDaysText));
        OnPropertyChanged(nameof(CashMovementVisibility));
        OnPropertyChanged(nameof(CashMovementEmptyVisibility));
    }

    private void NotifyTrialBalanceState()
    {
        OnPropertyChanged(nameof(TrialBalanceSummaryText));
        OnPropertyChanged(nameof(TrialBalanceRowsCountText));
        OnPropertyChanged(nameof(TrialBalanceDebitTotalText));
        OnPropertyChanged(nameof(TrialBalanceCreditTotalText));
        OnPropertyChanged(nameof(TrialBalancePeriodStatusText));
        OnPropertyChanged(nameof(TrialBalanceRangeText));
        OnPropertyChanged(nameof(TrialBalanceVisibility));
        OnPropertyChanged(nameof(TrialBalanceEmptyVisibility));
    }

    private JournalAccountOptionVm? SelectedAccountOption
        => SelectedAccountId is Guid accountId
            ? AccountOptions.FirstOrDefault(x => x.Id == accountId)
            : null;

    private static decimal GetSignedBalance(decimal debit, decimal credit, AccountNature nature)
        => nature == AccountNature.Debit
            ? debit - credit
            : credit - debit;

    private static string FormatBalance(decimal signedBalance, AccountNature nature)
    {
        if (signedBalance == 0m)
            return "0.00";

        var label = nature == AccountNature.Debit
            ? signedBalance >= 0m ? "مدين" : "دائن"
            : signedBalance >= 0m ? "دائن" : "مدين";

        return $"{Math.Abs(signedBalance):N2} {label}";
    }

    private static string MapEntryType(int type)
        => (JournalEntryType)type switch
        {
            JournalEntryType.DailyJournal => "قيد يومية",
            JournalEntryType.ReceiptVoucher => "سند قبض",
            JournalEntryType.PaymentVoucher => "سند صرف",
            JournalEntryType.Adjustment => "قيد تسوية",
            JournalEntryType.OpeningEntry => "قيد افتتاحي",
            JournalEntryType.TransferVoucher => "تحويل",
            JournalEntryType.DailyCashClosing => "إقفال صندوق",
            _ => "سند"
        };

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}
