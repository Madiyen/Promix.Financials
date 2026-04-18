using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Promix.Financials.Application.Features.Journals.Commands;
using Promix.Financials.Application.Features.Journals.Queries;
using Promix.Financials.Application.Features.Journals.Services;
using Promix.Financials.Domain.Enums;
using Promix.Financials.UI.ViewModels.Journals.Models;

namespace Promix.Financials.UI.ViewModels.Journals;

public sealed class JournalEntriesViewModel : INotifyPropertyChanged
{
    private readonly IJournalEntriesQuery _query;
    private readonly CreateJournalEntryService _createService;
    private readonly CreateDailyCashClosingService _cashClosingService;
    private readonly PostJournalEntryService _postService;
    private readonly UpdateJournalEntryService _updateService;
    private readonly DeleteJournalEntryService _deleteService;
    private readonly List<JournalEntryRowVm> _allEntries = new();
    private Guid _companyId;
    private JournalEntryRowVm? _selectedEntry;
    private bool _isBusy;
    private string? _errorMessage;
    private string? _successMessage;
    private DateTimeOffset? _lastRefreshedAt;
    private string _searchText = string.Empty;
    private string _typeFilterKey = "all";
    private string _statusFilterKey = "all";
    private string _periodFilterKey = "all";
    private DateOnly? _lockedThroughDate;

    public JournalEntriesViewModel(
        IJournalEntriesQuery query,
        CreateJournalEntryService createService,
        CreateDailyCashClosingService cashClosingService,
        PostJournalEntryService postService,
        UpdateJournalEntryService updateService,
        DeleteJournalEntryService deleteService)
    {
        _query = query;
        _createService = createService;
        _cashClosingService = cashClosingService;
        _postService = postService;
        _updateService = updateService;
        _deleteService = deleteService;
    }

    public ObservableCollection<JournalEntryRowVm> Entries { get; } = new();
    public ObservableCollection<JournalAccountOptionVm> AccountOptions { get; } = new();
    public ObservableCollection<JournalCurrencyOptionVm> CurrencyOptions { get; } = new();
    public ObservableCollection<JournalActivityBarVm> ActivityBars { get; } = new();

    public JournalEntryRowVm? SelectedEntry
    {
        get => _selectedEntry;
        set
        {
            if (_selectedEntry == value) return;
            _selectedEntry = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(CanPostSelected));
            OnPropertyChanged(nameof(SelectedSummaryText));
            OnPropertyChanged(nameof(SelectedEntryVisibility));
            OnPropertyChanged(nameof(PreviewPlaceholderVisibility));
            OnPropertyChanged(nameof(SelectedEntryTitle));
            OnPropertyChanged(nameof(SelectedEntryCreatedAtText));
            OnPropertyChanged(nameof(SelectedEntryCurrencyText));
            OnPropertyChanged(nameof(SelectedEntryExchangeRateText));
            OnPropertyChanged(nameof(SelectedEntryPostedAtText));
            OnPropertyChanged(nameof(SelectedEntryModifiedAtText));
            OnPropertyChanged(nameof(SelectedEntryAuditText));
            OnPropertyChanged(nameof(SelectedEntryReferenceText));
            OnPropertyChanged(nameof(SelectedEntryDescriptionText));
            OnPropertyChanged(nameof(SelectedEntryStatusHintText));
            OnPropertyChanged(nameof(SelectedEntryMovementText));
            OnPropertyChanged(nameof(SelectedEntryBalanceText));
            OnPropertyChanged(nameof(SelectedEntryDebitCreditText));
            OnPropertyChanged(nameof(SelectedEntryLineSummaryText));
            OnPropertyChanged(nameof(SelectedEntryActionText));
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
        }
    }

    public string SearchText
    {
        get => _searchText;
        set
        {
            var normalized = value?.Trim() ?? string.Empty;
            if (_searchText == normalized) return;
            _searchText = normalized;
            OnPropertyChanged();
            ApplyFilters();
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
        }
    }

    public string? SuccessMessage
    {
        get => _successMessage;
        private set
        {
            if (_successMessage == value) return;
            _successMessage = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(HasSuccess));
        }
    }

    public bool HasError => !string.IsNullOrWhiteSpace(ErrorMessage);
    public bool HasSuccess => !string.IsNullOrWhiteSpace(SuccessMessage);
    public bool CanPostSelected => SelectedEntry is { IsDraft: true };
    public bool HasActiveFilters => !string.IsNullOrWhiteSpace(SearchText)
        || _typeFilterKey != "all"
        || _statusFilterKey != "all"
        || _periodFilterKey != "all";

    public string TotalEntriesText => _allEntries.Count.ToString();
    public string PostedEntriesText => _allEntries.Count(x => x.Status == JournalEntryStatus.Posted).ToString();
    public string DraftEntriesText => _allEntries.Count(x => x.Status == JournalEntryStatus.Draft).ToString();
    public string MovementVolumeText => _allEntries.Sum(x => x.TotalDebit).ToString("N0");
    public string TodayReceiptVolumeText => GetTodayEntries()
        .Where(x => x.Type == JournalEntryType.ReceiptVoucher && x.Status == JournalEntryStatus.Posted)
        .Sum(x => x.TotalDebit)
        .ToString("N0");
    public string TodayPaymentVolumeText => GetTodayEntries()
        .Where(x => x.Type == JournalEntryType.PaymentVoucher && x.Status == JournalEntryStatus.Posted)
        .Sum(x => x.TotalDebit)
        .ToString("N0");
    public string TodayNetMovementText => GetTodayEntries()
        .Where(x => x.Status == JournalEntryStatus.Posted)
        .Sum(x => x.SignedMovementAmount)
        .ToString("N0");
    public string LastRefreshedText => _lastRefreshedAt is null ? "آخر تحديث: الآن" : $"آخر تحديث: {_lastRefreshedAt.Value:HH:mm}";
    public string QuickEntryShortcutsText => "Ctrl+S للحفظ كمسودة • Ctrl+Enter للحفظ والترحيل • Ctrl+Shift+N لإضافة سطر جديد";
    public string PeriodLockSummaryText => _lockedThroughDate is DateOnly lockedThroughDate
        ? $"الفترة المحاسبية مقفلة حتى {lockedThroughDate:yyyy-MM-dd}"
        : "الفترة المحاسبية مفتوحة";
    public string PeriodLockHintText => _lockedThroughDate is DateOnly lockedThroughDate
        ? $"أي سند بتاريخ {lockedThroughDate:yyyy-MM-dd} أو قبله سيُمنع من الإنشاء أو الترحيل. استخدم إقفال الصندوق اليومي فقط بعد اكتمال مراجعة اليوم."
        : "يمكنك العمل على التواريخ الحالية بحرية، وعند نهاية اليوم يمكنك قفل الفترة من خلال إقفال الصندوق اليومي.";
    public string FilterSummaryText => _allEntries.Count == 0
        ? "0"
        : Entries.Count == _allEntries.Count && !HasActiveFilters
            ? $"{_allEntries.Count}"
            : $"{Entries.Count} / {_allEntries.Count}";
    public string ActiveFiltersText => HasActiveFilters
        ? $"{GetTypeFilterLabel()} · {GetStatusFilterLabel()}"
        : "الكل";
    public string SelectedSummaryText => SelectedEntry is null
        ? "اختر سنداً من القائمة لعرض تفاصيله وحالته."
        : $"{SelectedEntry.TypeText} · {SelectedEntry.EntryNumber} · {SelectedEntry.TotalDebitText}";
    public Visibility EmptyStateVisibility => Entries.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
    public Visibility EntriesVisibility => Entries.Count == 0 ? Visibility.Collapsed : Visibility.Visible;
    public Visibility SelectedEntryVisibility => SelectedEntry is null ? Visibility.Collapsed : Visibility.Visible;
    public Visibility PreviewPlaceholderVisibility => SelectedEntry is null ? Visibility.Visible : Visibility.Collapsed;
    public string SelectedEntryTitle => SelectedEntry is null ? "حدد سنداً للمراجعة" : $"{SelectedEntry.TypeText} · {SelectedEntry.EntryNumber}";
    public string SelectedEntryCreatedAtText => SelectedEntry?.CreatedAtText ?? "—";
    public string SelectedEntryCurrencyText => SelectedEntry?.CurrencySummaryText ?? "—";
    public string SelectedEntryExchangeRateText => SelectedEntry?.ExchangeRateText ?? "—";
    public string SelectedEntryPostedAtText => SelectedEntry?.PostedAtText ?? "لم يرحل بعد";
    public string SelectedEntryModifiedAtText => SelectedEntry?.ModifiedAtText ?? "بدون تعديل";
    public string SelectedEntryAuditText => SelectedEntry?.AuditSummaryText ?? "لا توجد بيانات تتبع بعد.";
    public string SelectedEntryReferenceText => SelectedEntry?.ReferenceDisplay ?? "بدون رقم مرجعي";
    public string SelectedEntryDescriptionText => SelectedEntry?.Description ?? "اختر أي سند من القائمة وسيظهر وصفه وأثره المحاسبي هنا.";
    public string SelectedEntryStatusHintText => SelectedEntry?.StatusNoteText ?? "اختر سنداً من القائمة لعرض حالته وما إذا كان يمكن ترحيله.";
    public string SelectedEntryMovementText => SelectedEntry?.MovementText ?? "لا يوجد أثر معروض";
    public string SelectedEntryBalanceText => SelectedEntry?.BalanceText ?? "—";
    public string SelectedEntryDebitCreditText => SelectedEntry is null
        ? "—"
        : $"{SelectedEntry.TotalDebitText} مدين · {SelectedEntry.TotalCreditText} دائن";
    public string SelectedEntryLineSummaryText => SelectedEntry?.LineCountText ?? "—";
    public string SelectedEntryActionText => SelectedEntry is null
        ? "اختر سنداً لعرض الخيارات المتاحة."
        : SelectedEntry.IsDraft
            ? "يمكن ترحيل هذا السند الآن بعد مراجعته."
            : "السند مرحل للعرض فقط ولا يمكن تعديله أو حذفه.";

    public async Task InitializeAsync(Guid companyId)
    {
        _companyId = companyId;
        await LoadAsync();
    }

    public async Task RefreshAsync()
    {
        if (_companyId == Guid.Empty)
            return;

        await LoadAsync();
    }

    public async Task EnsureEntryDetailsLoadedAsync(JournalEntryRowVm? entry)
    {
        if (_companyId == Guid.Empty || entry is null || entry.HasLoadedDetails || entry.IsDetailsLoading)
            return;

        entry.BeginDetailsLoading();

        try
        {
            var detail = await _query.GetEntryDetailAsync(_companyId, entry.Id);
            var details = detail?.Lines.Select(MapLineDetail).ToList() ?? [];
            entry.SetDetails(details);
        }
        catch
        {
            entry.SetDetails([]);
        }
    }

    public async Task<bool> CreateAsync(CreateJournalEntryCommand command)
    {
        try
        {
            ErrorMessage = null;
            SuccessMessage = null;
            var entryId = await _createService.CreateAsync(command);
            SuccessMessage = command.PostNow
                ? "تم حفظ السند وترحيله بنجاح."
                : "تم حفظ السند كمسودة بنجاح.";
            await LoadAsync(entryId);
            return true;
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
            return false;
        }
    }

    public async Task<bool> UpdateAsync(UpdateJournalEntryCommand command)
    {
        try
        {
            ErrorMessage = null;
            SuccessMessage = null;
            await _updateService.UpdateAsync(command);
            SuccessMessage = command.PostNow
                ? "تم حفظ التعديلات وترحيل السند."
                : "تم حفظ التعديلات على السند.";
            await LoadAsync(command.EntryId);
            return true;
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
            return false;
        }
    }

    public async Task<bool> DeleteAsync(DeleteJournalEntryCommand command)
    {
        try
        {
            ErrorMessage = null;
            SuccessMessage = null;
            await _deleteService.DeleteAsync(command);
            SuccessMessage = "تم حذف السند منطقيًا من القوائم التشغيلية.";
            await LoadAsync();
            return true;
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
            return false;
        }
    }

    public async Task<bool> PostSelectedAsync()
    {
        if (SelectedEntry is null || _companyId == Guid.Empty)
            return false;

        try
        {
            ErrorMessage = null;
            SuccessMessage = null;
            await _postService.PostAsync(new PostJournalEntryCommand(_companyId, SelectedEntry.Id));
            SuccessMessage = $"تم ترحيل السند {SelectedEntry.EntryNumber}.";
            await LoadAsync(SelectedEntry.Id);
            return true;
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
            return false;
        }
    }

    public async Task<bool> CreateCashClosingAsync(CreateDailyCashClosingCommand command)
    {
        try
        {
            ErrorMessage = null;
            SuccessMessage = null;
            await _cashClosingService.CreateAsync(command);
            SuccessMessage = command.LockThroughEntryDate
                ? "تم إنشاء سند إقفال الصندوق اليومي وترحيله وإقفال الفترة حتى هذا التاريخ."
                : "تم إنشاء سند إقفال الصندوق اليومي وترحيله بنجاح.";
            await LoadAsync();
            return true;
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
            return false;
        }
    }

    public void SetTypeFilter(string? typeKey)
    {
        var normalized = string.IsNullOrWhiteSpace(typeKey) ? "all" : typeKey.Trim().ToLowerInvariant();
        if (_typeFilterKey == normalized) return;
        _typeFilterKey = normalized;
        ApplyFilters();
    }

    public void SetStatusFilter(string? statusKey)
    {
        var normalized = string.IsNullOrWhiteSpace(statusKey) ? "all" : statusKey.Trim().ToLowerInvariant();
        if (_statusFilterKey == normalized) return;
        _statusFilterKey = normalized;
        ApplyFilters();
    }

    public void SetPeriodFilter(string? periodKey)
    {
        var normalized = string.IsNullOrWhiteSpace(periodKey) ? "all" : periodKey.Trim().ToLowerInvariant();
        if (_periodFilterKey == normalized) return;
        _periodFilterKey = normalized;
        ApplyFilters();
    }

    public void ClearFilters()
    {
        _searchText = string.Empty;
        _typeFilterKey = "all";
        _statusFilterKey = "all";
        _periodFilterKey = "all";
        OnPropertyChanged(nameof(SearchText));
        ApplyFilters();
    }

    private async Task LoadAsync(Guid? preferredSelectionId = null)
    {
        IsBusy = true;
        ErrorMessage = null;

        try
        {
            var entries = await _query.GetEntriesAsync(_companyId);
            var accounts = await _query.GetPostingAccountsAsync(_companyId);
            var currencies = await _query.GetActiveCurrenciesAsync(_companyId);
            var periodLock = await _query.GetJournalPeriodLockAsync(_companyId);
            var trendStart = DateOnly.FromDateTime(DateTime.Today.AddDays(-6));
            var trendEnd = DateOnly.FromDateTime(DateTime.Today);
            var cashMovements = await _query.GetCashMovementSeriesAsync(_companyId, trendStart, trendEnd);

            _allEntries.Clear();
            foreach (var entry in entries)
            {
                _allEntries.Add(new JournalEntryRowVm(
                    entry.Id,
                    entry.EntryNumber,
                    entry.EntryDate,
                    (JournalEntryType)entry.Type,
                    (JournalEntryStatus)entry.Status,
                    entry.ReferenceNo,
                    entry.Description,
                    entry.CurrencyCode,
                    entry.ExchangeRate,
                    entry.CurrencyAmount,
                    entry.TotalDebit,
                    entry.TotalCredit,
                    entry.LineCount,
                    entry.CreatedAtUtc,
                    entry.PostedAtUtc,
                    entry.ModifiedAtUtc));
            }

            AccountOptions.Clear();
            foreach (var account in accounts)
                AccountOptions.Add(new JournalAccountOptionVm(account.Id, account.Code, account.NameAr, account.Nature, account.SystemRole, account.IsLegacyPartyLinkedAccount));

            CurrencyOptions.Clear();
            foreach (var currency in currencies)
            {
                CurrencyOptions.Add(new JournalCurrencyOptionVm(
                    currency.CurrencyCode,
                    currency.NameAr,
                    currency.NameEn,
                    currency.Symbol,
                    currency.DecimalPlaces,
                    currency.ExchangeRate,
                    currency.IsBaseCurrency));
            }

            RebuildActivityBars(cashMovements, trendStart);
            _lockedThroughDate = periodLock.LockedThroughDate;
            _lastRefreshedAt = DateTimeOffset.Now;
            ApplyFilters(preferredSelectionId ?? SelectedEntry?.Id);
            await EnsureEntryDetailsLoadedAsync(SelectedEntry);

            OnPropertyChanged(nameof(LastRefreshedText));
            OnPropertyChanged(nameof(PeriodLockSummaryText));
            OnPropertyChanged(nameof(PeriodLockHintText));
            OnPropertyChanged(nameof(QuickEntryShortcutsText));
            OnPropertyChanged(nameof(TotalEntriesText));
            OnPropertyChanged(nameof(PostedEntriesText));
            OnPropertyChanged(nameof(DraftEntriesText));
            OnPropertyChanged(nameof(MovementVolumeText));
            OnPropertyChanged(nameof(TodayReceiptVolumeText));
            OnPropertyChanged(nameof(TodayPaymentVolumeText));
            OnPropertyChanged(nameof(TodayNetMovementText));
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }
        finally
        {
            IsBusy = false;
        }
    }

    private void ApplyFilters(Guid? preferredSelectionId = null)
    {
        IEnumerable<JournalEntryRowVm> filtered = _allEntries;

        if (!string.IsNullOrWhiteSpace(SearchText))
            filtered = filtered.Where(x => x.MatchesSearch(SearchText));

        filtered = _typeFilterKey switch
        {
            "receipt" => filtered.Where(x => x.Type == JournalEntryType.ReceiptVoucher),
            "payment" => filtered.Where(x => x.Type == JournalEntryType.PaymentVoucher),
            "transfer" => filtered.Where(x => x.Type == JournalEntryType.TransferVoucher),
            "daily" => filtered.Where(x => x.Type == JournalEntryType.DailyJournal),
            "opening" => filtered.Where(x => x.Type == JournalEntryType.OpeningEntry),
            "closing" => filtered.Where(x => x.Type == JournalEntryType.DailyCashClosing),
            "adjustment" => filtered.Where(x => x.Type == JournalEntryType.Adjustment),
            _ => filtered
        };

        filtered = _statusFilterKey switch
        {
            "posted" => filtered.Where(x => x.Status == JournalEntryStatus.Posted),
            "draft" => filtered.Where(x => x.Status == JournalEntryStatus.Draft),
            _ => filtered
        };

        var today = DateOnly.FromDateTime(DateTime.Today);
        filtered = _periodFilterKey switch
        {
            "today" => filtered.Where(x => x.EntryDate == today),
            "week" => filtered.Where(x => x.EntryDate >= today.AddDays(-6)),
            "month" => filtered.Where(x => x.EntryDate.Year == today.Year && x.EntryDate.Month == today.Month),
            _ => filtered
        };

        var result = filtered
            .OrderByDescending(x => x.EntryDate)
            .ThenByDescending(x => x.CreatedAtUtc)
            .ToList();

        Entries.Clear();
        foreach (var entry in result)
            Entries.Add(entry);

        var nextSelection = result.FirstOrDefault(x => preferredSelectionId is not null && x.Id == preferredSelectionId)
            ?? result.FirstOrDefault();

        SelectedEntry = nextSelection;

        OnPropertyChanged(nameof(FilterSummaryText));
        OnPropertyChanged(nameof(ActiveFiltersText));
        OnPropertyChanged(nameof(HasActiveFilters));
        OnPropertyChanged(nameof(EmptyStateVisibility));
        OnPropertyChanged(nameof(EntriesVisibility));
    }

    private void RebuildActivityBars(IReadOnlyList<JournalCashMovementDto> cashMovements, DateOnly start)
    {
        ActivityBars.Clear();

        var grouped = cashMovements.ToDictionary(x => x.EntryDate, x => x.NetMovement);

        var days = Enumerable.Range(0, 7)
            .Select(offset => start.AddDays(offset))
            .ToList();

        var max = days
            .Select(day => grouped.TryGetValue(day, out var amount) ? Math.Abs(amount) : 0m)
            .DefaultIfEmpty(0m)
            .Max();

        foreach (var day in days)
        {
            var amount = grouped.TryGetValue(day, out var value) ? value : 0m;
            var barHeight = max <= 0 ? 16 : 16 + (double)(Math.Abs(amount) / max) * 70;
            var valueText = amount > 0 ? $"+{amount:N0}" : amount < 0 ? amount.ToString("N0") : "0";
            var fill = amount > 0
                ? JournalActivityBarVm.FromHex("#16A34A")
                : amount < 0
                    ? JournalActivityBarVm.FromHex("#DC2626")
                    : JournalActivityBarVm.FromHex("#CBD5E1");

            ActivityBars.Add(new JournalActivityBarVm(
                day.ToString("dd"),
                valueText,
                Math.Round(barHeight, 1),
                fill));
        }
    }

    private IEnumerable<JournalEntryRowVm> GetTodayEntries()
    {
        var today = DateOnly.FromDateTime(DateTime.Today);
        return _allEntries.Where(x => x.EntryDate == today);
    }

    private string GetTypeFilterLabel() => _typeFilterKey switch
    {
        "receipt" => "سند قبض",
        "payment" => "سند صرف",
        "transfer" => "تحويل",
        "daily" => "قيد يومية",
        "opening" => "قيد افتتاحي",
        "closing" => "إقفال صندوق",
        "adjustment" => "قيد تسوية",
        _ => "كل الأنواع"
    };

    private string GetStatusFilterLabel() => _statusFilterKey switch
    {
        "posted" => "مرحل",
        "draft" => "مسودة",
        _ => "كل الحالات"
    };

    private string GetPeriodFilterLabel() => _periodFilterKey switch
    {
        "today" => "اليوم",
        "week" => "آخر 7 أيام",
        "month" => "هذا الشهر",
        _ => "كل الفترات"
    };

    private JournalEntryLineDetailVm MapLineDetail(JournalEntryDetailLineDto line)
    {
        var accountName = AccountOptions.FirstOrDefault(x => x.Id == line.AccountId)?.DisplayText ?? "حساب غير معروف";
        var description = string.IsNullOrWhiteSpace(line.PartyName)
            ? (line.Description ?? string.Empty)
            : string.IsNullOrWhiteSpace(line.Description)
                ? line.PartyName!
                : $"{line.PartyName} • {line.Description}";

        return new JournalEntryLineDetailVm(accountName, line.Debit, line.Credit, description);
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}

