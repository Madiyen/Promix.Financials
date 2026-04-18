using Microsoft.UI.Xaml;
using Promix.Financials.Application.Features.Journals.Queries;
using Promix.Financials.Domain.Enums;
using Promix.Financials.UI.ViewModels.Journals.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace Promix.Financials.UI.ViewModels.Dashboard;

public sealed class DashboardViewModel : INotifyPropertyChanged
{
    private readonly IJournalEntriesQuery _query;
    private readonly List<JournalEntryRowVm> _allEntries = new();
    private Guid _companyId;
    private bool _isBusy;
    private decimal _weeklyCashInflow;
    private decimal _weeklyCashOutflow;

    public DashboardViewModel(IJournalEntriesQuery query)
    {
        _query = query;
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public ObservableCollection<JournalEntryRowVm> RecentEntries { get; } = new();
    public ObservableCollection<JournalActivityBarVm> ActivityBars { get; } = new();

    public bool IsBusy
    {
        get => _isBusy;
        private set
        {
            if (_isBusy == value) return;
            _isBusy = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(LoadingVisibility));
        }
    }

    public string TotalEntriesText => _allEntries.Count.ToString("N0");
    public string PostedEntriesText => _allEntries.Count(x => x.Status == JournalEntryStatus.Posted).ToString("N0");
    public string DraftEntriesText => _allEntries.Count(x => x.Status == JournalEntryStatus.Draft).ToString("N0");
    public string CurrentDateText => DateTime.Now.ToString("dddd d MMMM yyyy", new CultureInfo("ar-SA"));
    public string TodayNetMovementText => GetTodayPostedNetMovement().ToString("N0");
    public string TodayMovementHintText => GetTodayPostedCount() == 0
        ? "لا توجد حركة مرحلة اليوم"
        : $"{GetTodayPostedCount()} حركة مرحلة اليوم";
    public string WeeklyCashInflowText => _weeklyCashInflow.ToString("N0");
    public string WeeklyCashOutflowText => _weeklyCashOutflow.ToString("N0");

    public int ReceiptShare => GetShare(x => x.Type == JournalEntryType.ReceiptVoucher);
    public int PaymentShare => GetShare(x => x.Type == JournalEntryType.PaymentVoucher);
    public int DailyShare => GetShare(x => x.Type == JournalEntryType.DailyJournal);
    public int OtherShare => GetShare(x =>
        x.Type != JournalEntryType.ReceiptVoucher &&
        x.Type != JournalEntryType.PaymentVoucher &&
        x.Type != JournalEntryType.DailyJournal);

    public string ReceiptShareText => $"{ReceiptShare}%";
    public string PaymentShareText => $"{PaymentShare}%";
    public string DailyShareText => $"{DailyShare}%";
    public string OtherShareText => $"{OtherShare}%";

    public Visibility RecentEntriesVisibility => RecentEntries.Count == 0 ? Visibility.Collapsed : Visibility.Visible;
    public Visibility EmptyEntriesVisibility => RecentEntries.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
    public Visibility LoadingVisibility => IsBusy ? Visibility.Visible : Visibility.Collapsed;

    public async Task InitializeAsync(Guid companyId)
    {
        _companyId = companyId;
        await RefreshAsync();
    }

    public async Task RefreshAsync()
    {
        if (_companyId == Guid.Empty)
        {
            return;
        }

        IsBusy = true;
        try
        {
            var entries = await _query.GetEntriesAsync(_companyId);
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

            RecentEntries.Clear();
            foreach (var entry in _allEntries
                .OrderByDescending(x => x.EntryDate)
                .ThenByDescending(x => x.CreatedAtUtc)
                .Take(6))
            {
                RecentEntries.Add(entry);
            }

            RebuildActivityBars(cashMovements, trendStart);
            RaiseDashboardPropertyChanges();
        }
        finally
        {
            IsBusy = false;
        }
    }

    private void RaiseDashboardPropertyChanges()
    {
        OnPropertyChanged(nameof(TotalEntriesText));
        OnPropertyChanged(nameof(PostedEntriesText));
        OnPropertyChanged(nameof(DraftEntriesText));
        OnPropertyChanged(nameof(CurrentDateText));
        OnPropertyChanged(nameof(TodayNetMovementText));
        OnPropertyChanged(nameof(TodayMovementHintText));
        OnPropertyChanged(nameof(WeeklyCashInflowText));
        OnPropertyChanged(nameof(WeeklyCashOutflowText));
        OnPropertyChanged(nameof(ReceiptShare));
        OnPropertyChanged(nameof(PaymentShare));
        OnPropertyChanged(nameof(DailyShare));
        OnPropertyChanged(nameof(OtherShare));
        OnPropertyChanged(nameof(ReceiptShareText));
        OnPropertyChanged(nameof(PaymentShareText));
        OnPropertyChanged(nameof(DailyShareText));
        OnPropertyChanged(nameof(OtherShareText));
        OnPropertyChanged(nameof(RecentEntriesVisibility));
        OnPropertyChanged(nameof(EmptyEntriesVisibility));
    }

    private decimal GetTodayPostedNetMovement()
        => _allEntries
            .Where(x => x.EntryDate == DateOnly.FromDateTime(DateTime.Today) && x.Status == JournalEntryStatus.Posted)
            .Sum(x => x.SignedMovementAmount);

    private int GetTodayPostedCount()
        => _allEntries.Count(x => x.EntryDate == DateOnly.FromDateTime(DateTime.Today) && x.Status == JournalEntryStatus.Posted);

    private int GetShare(Func<JournalEntryRowVm, bool> predicate)
    {
        if (_allEntries.Count == 0)
        {
            return 0;
        }

        return (int)Math.Round((_allEntries.Count(predicate) / (double)_allEntries.Count) * 100, MidpointRounding.AwayFromZero);
    }

    private void RebuildActivityBars(IReadOnlyList<JournalCashMovementDto> cashMovements, DateOnly start)
    {
        ActivityBars.Clear();
        _weeklyCashInflow = cashMovements.Where(x => x.NetMovement > 0m).Sum(x => x.NetMovement);
        _weeklyCashOutflow = cashMovements.Where(x => x.NetMovement < 0m).Sum(x => Math.Abs(x.NetMovement));

        var grouped = cashMovements.ToDictionary(x => x.EntryDate, x => x.NetMovement);
        var days = Enumerable.Range(0, 7).Select(offset => start.AddDays(offset)).ToList();
        var max = days
            .Select(day => grouped.TryGetValue(day, out var amount) ? Math.Abs(amount) : 0m)
            .DefaultIfEmpty(0m)
            .Max();

        foreach (var day in days)
        {
            var amount = grouped.TryGetValue(day, out var value) ? value : 0m;
            var barHeight = max <= 0 ? 18 : 18 + (double)(Math.Abs(amount) / max) * 92;
            var fill = amount > 0
                ? JournalActivityBarVm.FromHex("#16A34A")
                : amount < 0
                    ? JournalActivityBarVm.FromHex("#DC2626")
                    : JournalActivityBarVm.FromHex("#CBD5E1");

            ActivityBars.Add(new JournalActivityBarVm(
                day.ToString("dd"),
                amount > 0 ? $"+{amount:N0}" : amount < 0 ? amount.ToString("N0") : "0",
                Math.Round(barHeight, 1),
                fill));
        }
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}
