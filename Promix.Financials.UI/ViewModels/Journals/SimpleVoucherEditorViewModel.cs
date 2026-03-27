using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Promix.Financials.Application.Features.Journals.Commands;
using Promix.Financials.Application.Features.Journals.Queries;
using Promix.Financials.Domain.Enums;
using Promix.Financials.UI.ViewModels.Journals.Models;

namespace Promix.Financials.UI.ViewModels.Journals;

public sealed class SimpleVoucherEditorViewModel : INotifyPropertyChanged
{
    private static readonly string DebitLineText = "مدين";
    private static readonly string CreditLineText = "دائن";

    private readonly Guid _companyId;
    private readonly JournalEntryType _type;
    private readonly IJournalEntriesQuery _query;
    private Guid? _selectedCashAccountId;
    private Guid? _selectedCounterpartyAccountId;
    private string? _selectedCurrencyCode;
    private double _exchangeRate = 1;
    private DateTimeOffset _entryDate = DateTimeOffset.Now;
    private string _referenceNo = string.Empty;
    private string _description = string.Empty;
    private string _partyName = string.Empty;
    private double _amount;
    private string _previewNoteText = "اختر الحساب النقدي والحساب المقابل وأدخل المبلغ لعرض القيد الناتج وأثره على الرصيد.";
    private string? _previewErrorMessage;
    private int _previewRequestVersion;

    public SimpleVoucherEditorViewModel(
        Guid companyId,
        JournalEntryType type,
        string title,
        string subtitle,
        IEnumerable<JournalAccountOptionVm> accounts,
        IEnumerable<JournalCurrencyOptionVm> currencies,
        IJournalEntriesQuery query)
    {
        _companyId = companyId;
        _type = type;
        _query = query;
        Title = title;
        Subtitle = subtitle;
        AccountOptions = new ObservableCollection<JournalAccountOptionVm>(accounts.OrderBy(x => x.Code));
        CashAccountOptions = new ObservableCollection<JournalAccountOptionVm>(
            AccountOptions
                .Where(x => x.IsCashLike)
                .OrderBy(x => ResolveCashRank(x))
                .ThenBy(x => x.Code));
        CounterpartyAccountOptions = new ObservableCollection<JournalAccountOptionVm>(
            AccountOptions
                .Where(x => !x.IsCashLike)
                .OrderBy(x => ResolveCounterpartyRank(x))
                .ThenBy(x => x.Code));
        CurrencyOptions = new ObservableCollection<JournalCurrencyOptionVm>(
            currencies
                .OrderByDescending(x => x.IsBaseCurrency)
                .ThenBy(x => x.CurrencyCode));
        PostingPreviewLines = new ObservableCollection<VoucherPostingPreviewLineVm>();
        BalancePreviewCards = new ObservableCollection<VoucherBalancePreviewCardVm>();

        _selectedCashAccountId = JournalAccountDefaultsResolver.ResolvePreferredCashAccount(AccountOptions);

        var initialCurrency = CurrencyOptions.FirstOrDefault(x => x.IsBaseCurrency) ?? CurrencyOptions.FirstOrDefault();
        if (initialCurrency is not null)
        {
            _selectedCurrencyCode = initialCurrency.CurrencyCode;
            _exchangeRate = Convert.ToDouble(initialCurrency.IsBaseCurrency ? 1m : initialCurrency.ExchangeRate);
        }

        QueuePreviewRefresh();
    }

    public ObservableCollection<JournalAccountOptionVm> AccountOptions { get; }
    public ObservableCollection<JournalAccountOptionVm> CashAccountOptions { get; }
    public ObservableCollection<JournalAccountOptionVm> CounterpartyAccountOptions { get; }
    public ObservableCollection<JournalCurrencyOptionVm> CurrencyOptions { get; }
    public ObservableCollection<VoucherPostingPreviewLineVm> PostingPreviewLines { get; }
    public ObservableCollection<VoucherBalancePreviewCardVm> BalancePreviewCards { get; }

    public string Title { get; }
    public string Subtitle { get; }
    public string EffectBadgeText => _type == JournalEntryType.ReceiptVoucher ? "يزيد النقدية" : "يخفض النقدية";
    public string EffectSummaryText => _type == JournalEntryType.ReceiptVoucher
        ? "القيد الناتج سيجعل الحساب النقدي مديناً ويثبت الطرف المقابل دائناً بالقيمة نفسها."
        : "القيد الناتج سيجعل الحساب المقابل مديناً ويخفض الحساب النقدي كطرف دائن.";
    public string CounterpartyLabel => "الحساب المقابل";
    public string CounterpartyPlaceholder => _type == JournalEntryType.ReceiptVoucher
        ? "مثال: حساب عميل أو إيراد أو ذمم"
        : "مثال: حساب مصروف أو مورد أو سلفة";
    public string CashAccountGuideText => GetSelectedCashAccount() is { } cashAccount
        ? $"الافتراضي الحالي: {cashAccount.DisplayText}. يمكنك تغييره إلى الأموال الجاهزة أو أي حساب نقدي آخر."
        : "سيظهر الصندوق تلقائياً إن كان موجوداً، ويمكنك تغييره إلى أي صندوق أو خزينة أو مصرف.";
    public string CounterpartyGuideText => _type == JournalEntryType.ReceiptVoucher
        ? "اختر الحساب الذي يمثل العميل أو الإيراد أو الذمم المثبتة مقابل هذا القبض."
        : "اختر الحساب الذي يمثل المصروف أو المورد أو السلفة المثبتة مقابل هذا الصرف.";
    public string DescriptionGuideText => _type == JournalEntryType.ReceiptVoucher
        ? "اكتب شرحاً قصيراً يساعدك لاحقاً في اليومية وكشف الحساب، مثل دفعة إشراف أو تحصيل مستحق."
        : "اكتب شرحاً واضحاً لسبب الصرف، مثل أجور عمال أو دفعة مورد أو مصروف مشروع.";
    public string PartyLabel => _type == JournalEntryType.ReceiptVoucher ? "الجهة المقبوض منها" : "الجهة المصروف لها";
    public string HintText => _type == JournalEntryType.ReceiptVoucher
        ? "سند القبض يزيد رصيد الحساب النقدي ويقابله طرف دائن في الحساب المقابل."
        : "سند الصرف يخفض رصيد الحساب النقدي ويقابله طرف مدين في الحساب المقابل.";
    public JournalCurrencyOptionVm? SelectedCurrency => CurrencyOptions.FirstOrDefault(x => x.CurrencyCode == SelectedCurrencyCode);
    public string BaseCurrencyCode => CurrencyOptions.FirstOrDefault(x => x.IsBaseCurrency)?.CurrencyCode ?? "الأساسية";
    public string EquivalentLabel => $"المكافئ ({BaseCurrencyCode})";
    public bool CanEditExchangeRate => SelectedCurrency is not { IsBaseCurrency: true };
    public double EquivalentAmount => Math.Round(Amount * ExchangeRate, 2);
    public string PreviewNoteText
    {
        get => _previewNoteText;
        private set
        {
            if (_previewNoteText == value) return;
            _previewNoteText = value;
            OnPropertyChanged();
        }
    }

    public string? PreviewErrorMessage
    {
        get => _previewErrorMessage;
        private set
        {
            if (_previewErrorMessage == value) return;
            _previewErrorMessage = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(PreviewErrorVisibility));
        }
    }

    public Visibility PreviewErrorVisibility => string.IsNullOrWhiteSpace(PreviewErrorMessage) ? Visibility.Collapsed : Visibility.Visible;
    public Visibility PostingPreviewVisibility => PostingPreviewLines.Count > 0 ? Visibility.Visible : Visibility.Collapsed;
    public Visibility PostingPreviewPlaceholderVisibility => PostingPreviewLines.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
    public Visibility BalancePreviewVisibility => BalancePreviewCards.Count > 0 ? Visibility.Visible : Visibility.Collapsed;
    public Visibility BalancePreviewPlaceholderVisibility => BalancePreviewCards.Count == 0 ? Visibility.Visible : Visibility.Collapsed;

    public Guid? SelectedCashAccountId
    {
        get => _selectedCashAccountId;
        set
        {
            if (_selectedCashAccountId == value) return;
            _selectedCashAccountId = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(CashAccountGuideText));
            QueuePreviewRefresh();
        }
    }

    public Guid? SelectedCounterpartyAccountId
    {
        get => _selectedCounterpartyAccountId;
        set
        {
            if (_selectedCounterpartyAccountId == value) return;
            _selectedCounterpartyAccountId = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(CounterpartyGuideText));
            QueuePreviewRefresh();
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
            OnPropertyChanged(nameof(SelectedCurrency));
            OnPropertyChanged(nameof(CanEditExchangeRate));
            OnPropertyChanged(nameof(EquivalentLabel));
            OnPropertyChanged(nameof(EquivalentAmount));
            OnPropertyChanged(nameof(ExchangeRate));
            QueuePreviewRefresh();
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
            OnPropertyChanged(nameof(EquivalentAmount));
            QueuePreviewRefresh();
        }
    }

    public DateTimeOffset EntryDate
    {
        get => _entryDate;
        set
        {
            if (_entryDate == value) return;
            _entryDate = value;
            OnPropertyChanged();
            QueuePreviewRefresh();
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

    public string PartyName
    {
        get => _partyName;
        set
        {
            if (_partyName == value) return;
            _partyName = value;
            OnPropertyChanged();
        }
    }

    public double Amount
    {
        get => _amount;
        set
        {
            var decimals = SelectedCurrency?.DecimalPlaces ?? (byte)2;
            var normalized = value < 0 ? 0 : Math.Round(value, decimals);
            if (Math.Abs(_amount - normalized) < 0.0001) return;
            _amount = normalized;
            OnPropertyChanged();
            OnPropertyChanged(nameof(EquivalentAmount));
            QueuePreviewRefresh();
        }
    }

    public bool TryBuildCommand(Guid companyId, bool postNow, out CreateJournalEntryCommand? command, out string error)
    {
        command = null;
        error = string.Empty;

        var cashAccount = GetSelectedCashAccount();
        if (cashAccount is null)
        {
            error = "اختر الحساب النقدي أو الصندوق.";
            return false;
        }

        if (!cashAccount.IsCashLike)
        {
            error = "الحساب النقدي يجب أن يكون صندوقاً أو خزينة أو مصرفاً.";
            return false;
        }

        var counterparty = GetSelectedCounterpartyAccount();
        if (counterparty is null)
        {
            error = "اختر الحساب المقابل.";
            return false;
        }

        if (counterparty.IsCashLike)
        {
            error = "الحساب المقابل في سند القبض أو الصرف لا يجب أن يكون حساباً نقدياً. استخدم تحويل بين الحسابات لهذه الحالة.";
            return false;
        }

        if (SelectedCashAccountId == SelectedCounterpartyAccountId)
        {
            error = "لا يمكن أن يكون الحساب النقدي هو نفسه الحساب المقابل.";
            return false;
        }

        if (SelectedCurrency is null)
        {
            error = "اختر العملة.";
            return false;
        }

        if (Amount <= 0)
        {
            error = "أدخل مبلغاً أكبر من صفر.";
            return false;
        }

        if (ExchangeRate <= 0)
        {
            error = "أدخل سعر صرف صحيحاً.";
            return false;
        }

        var description = BuildDescription();
        var amount = Convert.ToDecimal(EquivalentAmount);
        if (amount <= 0)
        {
            error = "المبلغ المكافئ يجب أن يكون أكبر من صفر.";
            return false;
        }

        var lines = _type == JournalEntryType.ReceiptVoucher
            ? new[]
            {
                new CreateJournalEntryLineCommand(cashAccount.Id, amount, 0m, "الطرف النقدي"),
                new CreateJournalEntryLineCommand(counterparty.Id, 0m, amount, "الحساب المقابل")
            }
            : new[]
            {
                new CreateJournalEntryLineCommand(counterparty.Id, amount, 0m, "الحساب المصروف عليه"),
                new CreateJournalEntryLineCommand(cashAccount.Id, 0m, amount, "الحساب النقدي")
            };

        command = new CreateJournalEntryCommand(
            companyId,
            DateOnly.FromDateTime(EntryDate.Date),
            _type,
            ReferenceNo,
            description,
            SelectedCurrency.CurrencyCode,
            Convert.ToDecimal(ExchangeRate),
            Convert.ToDecimal(Amount),
            postNow,
            lines);

        return true;
    }

    private string BuildDescription()
    {
        var segments = new List<string>();
        if (!string.IsNullOrWhiteSpace(PartyName))
            segments.Add(PartyName.Trim());

        if (!string.IsNullOrWhiteSpace(Description))
            segments.Add(Description.Trim());

        if (segments.Count == 0)
            segments.Add(_type == JournalEntryType.ReceiptVoucher ? "سند قبض" : "سند صرف");

        return string.Join(" - ", segments);
    }

    private void QueuePreviewRefresh()
    {
        RebuildPostingPreview();
        var previewVersion = Interlocked.Increment(ref _previewRequestVersion);
        _ = RefreshBalancePreviewAsync(previewVersion);
    }

    private async Task RefreshBalancePreviewAsync(int previewVersion)
    {
        try
        {
            PreviewErrorMessage = null;
            await Task.Delay(120);

            if (previewVersion != _previewRequestVersion)
                return;

            var cashAccount = GetSelectedCashAccount();
            var counterparty = GetSelectedCounterpartyAccount();
            if (cashAccount is null || counterparty is null || Amount <= 0 || ExchangeRate <= 0)
            {
                BalancePreviewCards.Clear();
                NotifyPreviewCollectionState();
                PreviewNoteText = "اختر الحسابين وأدخل مبلغاً أكبر من صفر لعرض الرصيد قبل/بعد.";
                return;
            }

            var balances = await _query.GetPostedAccountBalancesAsync(
                _companyId,
                new[] { cashAccount.Id, counterparty.Id },
                DateOnly.FromDateTime(EntryDate.Date));

            if (previewVersion != _previewRequestVersion)
                return;

            RebuildBalancePreview(cashAccount, counterparty, balances);
        }
        catch (Exception ex)
        {
            if (previewVersion != _previewRequestVersion)
                return;

            BalancePreviewCards.Clear();
            NotifyPreviewCollectionState();
            PreviewErrorMessage = ex.Message;
        }
    }

    private void RebuildPostingPreview()
    {
        PostingPreviewLines.Clear();

        var cashAccount = GetSelectedCashAccount();
        var counterparty = GetSelectedCounterpartyAccount();
        var amount = Convert.ToDecimal(EquivalentAmount);

        if (cashAccount is null || counterparty is null || amount <= 0)
        {
            PreviewNoteText = "اختر الحسابين وأدخل مبلغاً أكبر من صفر لعرض القيد الناتج.";
            NotifyPreviewCollectionState();
            return;
        }

        if (_type == JournalEntryType.ReceiptVoucher)
        {
            PostingPreviewLines.Add(CreatePostingLine(DebitLineText, cashAccount.DisplayText, amount));
            PostingPreviewLines.Add(CreatePostingLine(CreditLineText, counterparty.DisplayText, amount));
            PreviewNoteText = "سند القبض الناتج: الحساب النقدي مدين، والحساب المقابل دائن.";
        }
        else
        {
            PostingPreviewLines.Add(CreatePostingLine(DebitLineText, counterparty.DisplayText, amount));
            PostingPreviewLines.Add(CreatePostingLine(CreditLineText, cashAccount.DisplayText, amount));
            PreviewNoteText = "سند الصرف الناتج: الحساب المقابل مدين، والحساب النقدي دائن.";
        }

        NotifyPreviewCollectionState();
    }

    private void RebuildBalancePreview(
        JournalAccountOptionVm cashAccount,
        JournalAccountOptionVm counterparty,
        IReadOnlyList<JournalAccountBalanceDto> balances)
    {
        BalancePreviewCards.Clear();

        var byId = balances.ToDictionary(x => x.AccountId);
        var amount = Convert.ToDecimal(EquivalentAmount);

        var cashBalance = byId.TryGetValue(cashAccount.Id, out var cashValue)
            ? cashValue
            : new JournalAccountBalanceDto(cashAccount.Id, cashAccount.Code, cashAccount.NameAr, cashAccount.Nature, 0m, 0m);

        var counterpartyBalance = byId.TryGetValue(counterparty.Id, out var counterpartyValue)
            ? counterpartyValue
            : new JournalAccountBalanceDto(counterparty.Id, counterparty.Code, counterparty.NameAr, counterparty.Nature, 0m, 0m);

        if (_type == JournalEntryType.ReceiptVoucher)
        {
            BalancePreviewCards.Add(CreateBalanceCard("الحساب النقدي", cashAccount.DisplayText, cashBalance, amount, 0m));
            BalancePreviewCards.Add(CreateBalanceCard("الحساب المقابل", counterparty.DisplayText, counterpartyBalance, 0m, amount));
        }
        else
        {
            BalancePreviewCards.Add(CreateBalanceCard("الحساب المقابل", counterparty.DisplayText, counterpartyBalance, amount, 0m));
            BalancePreviewCards.Add(CreateBalanceCard("الحساب النقدي", cashAccount.DisplayText, cashBalance, 0m, amount));
        }

        NotifyPreviewCollectionState();
    }

    private JournalAccountOptionVm? GetSelectedCashAccount()
        => AccountOptions.FirstOrDefault(x => x.Id == SelectedCashAccountId);

    private JournalAccountOptionVm? GetSelectedCounterpartyAccount()
        => AccountOptions.FirstOrDefault(x => x.Id == SelectedCounterpartyAccountId);

    private static VoucherPostingPreviewLineVm CreatePostingLine(string sideText, string accountText, decimal amount)
    {
        var isDebit = sideText == DebitLineText;
        return new VoucherPostingPreviewLineVm(
            sideText,
            accountText,
            amount.ToString("N2"),
            JournalActivityBarVm.FromHex(isDebit ? "#1D4ED8" : "#16A34A"),
            JournalActivityBarVm.FromHex(isDebit ? "#DBEAFE" : "#DCFCE7"));
    }

    private static VoucherBalancePreviewCardVm CreateBalanceCard(
        string label,
        string accountText,
        JournalAccountBalanceDto balance,
        decimal debitDelta,
        decimal creditDelta)
    {
        var beforeSigned = ComputeSignedBalance(balance.Nature, balance.TotalDebit, balance.TotalCredit);
        var afterSigned = ComputeSignedBalance(balance.Nature, balance.TotalDebit + debitDelta, balance.TotalCredit + creditDelta);
        var isDebitMove = debitDelta > 0;
        var amount = debitDelta > 0 ? debitDelta : creditDelta;

        return new VoucherBalancePreviewCardVm(
            label,
            accountText,
            $"الحركة: {(isDebitMove ? "مدين" : "دائن")} {amount:N2}",
            $"الرصيد قبل: {FormatBalance(balance.Nature, beforeSigned)}",
            $"الرصيد بعد: {FormatBalance(balance.Nature, afterSigned)}",
            JournalActivityBarVm.FromHex(isDebitMove ? "#1D4ED8" : "#16A34A"),
            JournalActivityBarVm.FromHex(isDebitMove ? "#EFF6FF" : "#ECFDF5"));
    }

    private static decimal ComputeSignedBalance(AccountNature nature, decimal totalDebit, decimal totalCredit)
        => nature == AccountNature.Debit
            ? totalDebit - totalCredit
            : totalCredit - totalDebit;

    private static string FormatBalance(AccountNature nature, decimal signedBalance)
    {
        if (signedBalance == 0)
            return "0.00 متوازن";

        var side = nature == AccountNature.Debit
            ? (signedBalance >= 0 ? "مدين" : "دائن")
            : (signedBalance >= 0 ? "دائن" : "مدين");

        return $"{Math.Abs(signedBalance):N2} {side}";
    }

    private void NotifyPreviewCollectionState()
    {
        OnPropertyChanged(nameof(PostingPreviewVisibility));
        OnPropertyChanged(nameof(PostingPreviewPlaceholderVisibility));
        OnPropertyChanged(nameof(BalancePreviewVisibility));
        OnPropertyChanged(nameof(BalancePreviewPlaceholderVisibility));
    }

    private int ResolveCashRank(JournalAccountOptionVm account)
    {
        if (account.NameAr.Contains("الصندوق", StringComparison.CurrentCultureIgnoreCase))
            return 0;

        if (account.NameAr.Contains("الخزنة", StringComparison.CurrentCultureIgnoreCase)
            || account.NameAr.Contains("الخزينة", StringComparison.CurrentCultureIgnoreCase)
            || account.NameAr.Contains("الأموال الجاهزة", StringComparison.CurrentCultureIgnoreCase))
            return 1;

        if (account.NameAr.Contains("مصرف", StringComparison.CurrentCultureIgnoreCase)
            || account.NameAr.Contains("بنك", StringComparison.CurrentCultureIgnoreCase))
            return 2;

        return 10;
    }

    private int ResolveCounterpartyRank(JournalAccountOptionVm account)
    {
        if (_type == JournalEntryType.ReceiptVoucher)
        {
            if (account.NameAr.Contains("عميل", StringComparison.CurrentCultureIgnoreCase)
                || account.NameAr.Contains("زبون", StringComparison.CurrentCultureIgnoreCase)
                || account.NameAr.Contains("مدين", StringComparison.CurrentCultureIgnoreCase))
                return 0;

            if (account.NameAr.Contains("إيراد", StringComparison.CurrentCultureIgnoreCase)
                || account.NameAr.Contains("مبيعات", StringComparison.CurrentCultureIgnoreCase))
                return 1;

            return 10;
        }

        if (account.NameAr.Contains("مورد", StringComparison.CurrentCultureIgnoreCase)
            || account.NameAr.Contains("دائن", StringComparison.CurrentCultureIgnoreCase))
            return 0;

        if (account.NameAr.Contains("مصروف", StringComparison.CurrentCultureIgnoreCase)
            || account.NameAr.Contains("أجور", StringComparison.CurrentCultureIgnoreCase)
            || account.NameAr.Contains("رواتب", StringComparison.CurrentCultureIgnoreCase))
            return 1;

        if (account.NameAr.Contains("سلفة", StringComparison.CurrentCultureIgnoreCase))
            return 2;

        return 10;
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}
