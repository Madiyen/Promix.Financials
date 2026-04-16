using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using Microsoft.UI.Xaml.Media;
using Promix.Financials.Application.Features.Journals.Commands;
using Promix.Financials.Domain.Enums;
using Promix.Financials.UI.ViewModels.Journals.Models;
using Promix.Financials.UI.ViewModels.Parties.Models;

namespace Promix.Financials.UI.ViewModels.Journals;

public sealed class JournalEntryEditorViewModel : INotifyPropertyChanged
{
    public ObservableCollection<JournalAccountOptionVm> AccountOptions { get; }
    public ObservableCollection<JournalCurrencyOptionVm> CurrencyOptions { get; }
    public ObservableCollection<PartyOptionVm> PartyOptions { get; }
    public ObservableCollection<JournalEntryLineEditorVm> Lines { get; } = new();

    private readonly JournalEntryType _entryType;
    private DateTimeOffset _entryDate = DateTimeOffset.Now;
    private string _referenceNo = string.Empty;
    private string _description = string.Empty;
    private string? _selectedCurrencyCode;
    private double _exchangeRate = 1;

    public JournalEntryEditorViewModel(
        IEnumerable<JournalAccountOptionVm> accountOptions,
        IEnumerable<JournalCurrencyOptionVm>? currencyOptions,
        IEnumerable<PartyOptionVm> partyOptions,
        JournalEntryType entryType,
        string title,
        string subtitle,
        string noteText)
    {
        AccountOptions = new ObservableCollection<JournalAccountOptionVm>(accountOptions);
        CurrencyOptions = new ObservableCollection<JournalCurrencyOptionVm>(
            (currencyOptions ?? Enumerable.Empty<JournalCurrencyOptionVm>())
                .OrderByDescending(x => x.IsBaseCurrency)
                .ThenBy(x => x.CurrencyCode));
        PartyOptions = new ObservableCollection<PartyOptionVm>(partyOptions.Where(x => x.IsActive).OrderBy(x => x.Code));
        _entryType = entryType;
        Title = title;
        Subtitle = subtitle;
        NoteText = noteText;

        var initialCurrency = CurrencyOptions.FirstOrDefault(x => x.IsBaseCurrency) ?? CurrencyOptions.FirstOrDefault();
        _selectedCurrencyCode = initialCurrency?.CurrencyCode;
        _exchangeRate = initialCurrency is null
            ? 1
            : Convert.ToDouble(initialCurrency.IsBaseCurrency ? 1m : initialCurrency.ExchangeRate);

        AddLine();
        AddLine();
    }

    public string Title { get; }
    public string Subtitle { get; }
    public string NoteText { get; }
    private Guid? ReceivableControlAccountId => AccountOptions.FirstOrDefault(x => x.IsReceivableControl)?.Id;
    private Guid? PayableControlAccountId => AccountOptions.FirstOrDefault(x => x.IsPayableControl)?.Id;
    public string EntryNumberPreviewText => $"{GetEntryPrefix(_entryType)}-AUTO";
    public string EntryTypeText => _entryType switch
    {
        JournalEntryType.OpeningEntry => "قيد افتتاحي",
        _ => "قيد يومية"
    };
    public JournalCurrencyOptionVm? SelectedCurrency => CurrencyOptions.FirstOrDefault(x => x.CurrencyCode == SelectedCurrencyCode);
    public string BaseCurrencyCode => CurrencyOptions.FirstOrDefault(x => x.IsBaseCurrency)?.CurrencyCode ?? "الأساسية";
    public bool CanEditExchangeRate => SelectedCurrency is not { IsBaseCurrency: true };

    public DateTimeOffset EntryDate
    {
        get => _entryDate;
        set
        {
            if (_entryDate == value) return;
            _entryDate = value;
            OnPropertyChanged();
        }
    }

    public string? SelectedCurrencyCode
    {
        get => _selectedCurrencyCode;
        set
        {
            if (_selectedCurrencyCode == value) return;
            _selectedCurrencyCode = value;

            var selected = SelectedCurrency;
            if (selected is not null)
                _exchangeRate = Convert.ToDouble(selected.IsBaseCurrency ? 1m : selected.ExchangeRate);

            OnPropertyChanged();
            NotifyCurrencyState();
        }
    }

    public double ExchangeRate
    {
        get => _exchangeRate;
        set
        {
            var normalized = value <= 0 ? 0 : Math.Round(value, 8);
            if (Math.Abs(_exchangeRate - normalized) < 0.00000001) return;
            _exchangeRate = normalized;
            OnPropertyChanged();
            NotifyCurrencyState();
        }
    }

    public string ReferenceNo
    {
        get => _referenceNo;
        set
        {
            if (_referenceNo == value) return;
            _referenceNo = value;
            OnPropertyChanged();
        }
    }

    public string Description
    {
        get => _description;
        set
        {
            if (_description == value) return;
            _description = value;
            OnPropertyChanged();
        }
    }

    public double TotalDebit => Lines.Sum(x => x.Debit);
    public double TotalCredit => Lines.Sum(x => x.Credit);
    public double Difference => Math.Round(TotalDebit - TotalCredit, 2);
    public bool IsBalanced => Math.Abs(Difference) < 0.009;
    public string TotalDebitText => TotalDebit.ToString("N2");
    public string TotalCreditText => TotalCredit.ToString("N2");
    public string DifferenceText => Math.Abs(Difference).ToString("N2");
    public string CurrencySummaryText => SelectedCurrency?.DisplayText ?? BaseCurrencyCode;
    public string BalanceStateText => IsBalanced ? "القيد متوازن" : "القيد غير متوازن";
    public string BalanceBarText => IsBalanced ? "القيد متوازن وجاهز للترحيل" : $"يوجد فرق بقيمة {DifferenceText}";
    public string BalanceHintText => IsBalanced ? "إجمالي المدين يساوي إجمالي الدائن." : "عدّل أحد الأسطر حتى يصبح الفرق صفراً.";
    public Brush BalanceStateBrush => JournalActivityBarVm.FromHex(Math.Abs(Difference) < 0.009 ? "#16A34A" : "#DC2626");

    public void AddLine()
    {
        var line = new JournalEntryLineEditorVm();
        line.PropertyChanged += OnLineChanged;
        Lines.Add(line);
        ApplyLineRules(line);
        NotifyTotals();
    }

    public void RemoveLine(JournalEntryLineEditorVm line)
    {
        if (Lines.Count <= 2)
            return;

        line.PropertyChanged -= OnLineChanged;
        Lines.Remove(line);
        NotifyTotals();
    }

    public bool TryBuildCommand(Guid companyId, bool postNow, out CreateJournalEntryCommand? command, out string error)
    {
        command = null;
        error = string.Empty;

        var usableLines = Lines.Where(x => !x.IsEmpty).ToList();
        if (usableLines.Count < 2)
        {
            error = "أضف سطرين على الأقل في السند.";
            return false;
        }

        foreach (var line in usableLines)
        {
            if (line.SelectedAccountId is null)
            {
                error = "كل سطر يجب أن يحتوي على حساب.";
                return false;
            }

            if (line.SelectedPartyId is Guid partyId)
            {
                var party = PartyOptions.FirstOrDefault(x => x.Id == partyId);
                if (party is null)
                {
                    error = "أحد الأطراف المختارة لم يعد متاحًا.";
                    return false;
                }

                if (!party.MatchesAccount(line.SelectedAccountId, ReceivableControlAccountId, PayableControlAccountId))
                {
                    error = "عند اختيار طرف يجب استخدام الحساب المرتبط به أو حساب الضبط المناسب لو كان هذا الطرف من سجل قديم على دفتر فرعي.";
                    return false;
                }
            }
            else if (line.SelectedAccountId is Guid selectedAccountId)
            {
                var account = AccountOptions.FirstOrDefault(x => x.Id == selectedAccountId);
                if (account?.IsPartyControlAccount == true)
                {
                    error = "لا يمكن التقييد على حسابات ضبط العملاء أو الموردين بدون اختيار طرف.";
                    return false;
                }

                if (account?.IsLegacyPartyLinkedAccount == true)
                {
                    error = "هذا الحساب مرتبط بطرف. اختر الطرف نفسه أو استخدم حساباً عاماً آخر.";
                    return false;
                }
            }

            var hasDebit = line.Debit > 0;
            var hasCredit = line.Credit > 0;
            if (hasDebit == hasCredit)
            {
                error = "كل سطر يجب أن يحتوي مدين أو دائن فقط.";
                return false;
            }
        }

        if (SelectedCurrency is null && CurrencyOptions.Count > 0)
        {
            error = "اختر العملة المستخدمة في القيد.";
            return false;
        }

        if (SelectedCurrency is not null && ExchangeRate <= 0)
        {
            error = "سعر الصرف يجب أن يكون أكبر من صفر.";
            return false;
        }

        if (TotalDebit <= 0 || TotalCredit <= 0)
        {
            error = "يجب أن يحتوي السند على قيم مدينة ودائنة.";
            return false;
        }

        if (Math.Abs(Difference) >= 0.009)
        {
            error = "السند غير متوازن. يجب أن يتساوى الإجمالي المدين مع الدائن.";
            return false;
        }

        command = new CreateJournalEntryCommand(
            CompanyId: companyId,
            EntryDate: DateOnly.FromDateTime(EntryDate.Date),
            Type: _entryType,
            ReferenceNo: ReferenceNo,
            Description: Description,
            CurrencyCode: SelectedCurrency?.CurrencyCode,
            ExchangeRate: SelectedCurrency is null ? null : Convert.ToDecimal(Math.Max(ExchangeRate, 0.00000001)),
            CurrencyAmount: SelectedCurrency is null ? null : ResolveCurrencyAmount(),
            PostNow: postNow,
            Lines: usableLines
                .Select(x => new CreateJournalEntryLineCommand(
                    x.SelectedAccountId!.Value,
                    Convert.ToDecimal(x.Debit),
                    Convert.ToDecimal(x.Credit),
                    x.Description,
                    NormalizeSmallText(x.PartyName),
                    x.SelectedPartyId))
                .ToList());

        return true;
    }

    private void OnLineChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(JournalEntryLineEditorVm.Debit)
            or nameof(JournalEntryLineEditorVm.Credit)
            or nameof(JournalEntryLineEditorVm.SelectedAccountId)
            or nameof(JournalEntryLineEditorVm.SelectedPartyId)
            or nameof(JournalEntryLineEditorVm.IsEmpty))
        {
            if (sender is JournalEntryLineEditorVm line)
            {
                ApplyLineRules(line);
            }

            NotifyTotals();
        }
    }

    private void ApplyLineRules(JournalEntryLineEditorVm line)
    {
        UpdatePartyRequirement(line);
        ApplyPartyDefaults(line);
        EnsurePartyStillMatches(line);
    }

    private void UpdatePartyRequirement(JournalEntryLineEditorVm line)
    {
        var account = line.SelectedAccountId is Guid accountId
            ? AccountOptions.FirstOrDefault(x => x.Id == accountId)
            : null;

        line.RequiresPartySelection = account?.IsPartyControlAccount == true || account?.IsLegacyPartyLinkedAccount == true;
    }

    private void ApplyPartyDefaults(JournalEntryLineEditorVm line)
    {
        if (line.SelectedPartyId is not Guid partyId)
            return;

        var party = PartyOptions.FirstOrDefault(x => x.Id == partyId);
        if (party is null)
            return;

        line.PartyName = party.NameAr;
        if (party.MatchesAccount(line.SelectedAccountId, ReceivableControlAccountId, PayableControlAccountId))
            return;

        var preferredAccountId = party.ResolveJournalAccountId(line.Debit >= line.Credit, ReceivableControlAccountId, PayableControlAccountId);
        if (preferredAccountId is Guid accountId)
            line.SelectedAccountId = accountId;
    }

    private void EnsurePartyStillMatches(JournalEntryLineEditorVm line)
    {
        if (line.SelectedPartyId is not Guid partyId)
            return;

        var party = PartyOptions.FirstOrDefault(x => x.Id == partyId);
        if (party is null)
            return;

        if (!party.MatchesAccount(line.SelectedAccountId, ReceivableControlAccountId, PayableControlAccountId))
            line.SelectedPartyId = null;
    }

    private static string? NormalizeSmallText(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private void NotifyTotals()
    {
        OnPropertyChanged(nameof(TotalDebit));
        OnPropertyChanged(nameof(TotalDebitText));
        OnPropertyChanged(nameof(TotalCredit));
        OnPropertyChanged(nameof(TotalCreditText));
        OnPropertyChanged(nameof(Difference));
        OnPropertyChanged(nameof(DifferenceText));
        OnPropertyChanged(nameof(IsBalanced));
        OnPropertyChanged(nameof(BalanceStateText));
        OnPropertyChanged(nameof(BalanceBarText));
        OnPropertyChanged(nameof(BalanceHintText));
        OnPropertyChanged(nameof(BalanceStateBrush));
    }

    private void NotifyCurrencyState()
    {
        OnPropertyChanged(nameof(SelectedCurrency));
        OnPropertyChanged(nameof(CanEditExchangeRate));
        OnPropertyChanged(nameof(CurrencySummaryText));
    }

    private decimal ResolveCurrencyAmount()
    {
        var currency = SelectedCurrency;
        if (currency is null)
            return 0;

        var baseAmount = Convert.ToDecimal(TotalDebit);
        if (currency.IsBaseCurrency)
            return decimal.Round(baseAmount, currency.DecimalPlaces, MidpointRounding.AwayFromZero);

        var rate = Convert.ToDecimal(Math.Max(ExchangeRate, 0.00000001));
        return decimal.Round(baseAmount / rate, currency.DecimalPlaces, MidpointRounding.AwayFromZero);
    }

    private static string GetEntryPrefix(JournalEntryType entryType) => entryType switch
    {
        JournalEntryType.OpeningEntry => "OPN",
        _ => "JV"
    };

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
