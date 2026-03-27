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
using Promix.Financials.UI.Services.Journals;
using Promix.Financials.UI.ViewModels.Journals.Models;

namespace Promix.Financials.UI.ViewModels.Journals;

public sealed class TransferVoucherEditorViewModel : INotifyPropertyChanged
{
    private readonly Guid _companyId;
    private readonly IJournalEntriesQuery _query;
    private readonly IJournalQuickDefaultsStore? _quickDefaultsStore;
    private Guid? _selectedSourceAccountId;
    private Guid? _selectedTargetAccountId;
    private string? _selectedCurrencyCode;
    private double _exchangeRate = 1;
    private DateTimeOffset _entryDate = DateTimeOffset.Now;
    private string _referenceNo = string.Empty;
    private string _description = string.Empty;
    private double _amount;
    private string _previewNoteText = "اختر حساب المصدر والحساب المستلم وأدخل مبلغ التحويل لعرض أثر العملية.";
    private string? _previewErrorMessage;
    private int _previewRequestVersion;

    public TransferVoucherEditorViewModel(
        Guid companyId,
        IEnumerable<JournalAccountOptionVm> accounts,
        IEnumerable<JournalCurrencyOptionVm> currencies,
        IJournalEntriesQuery query,
        IJournalQuickDefaultsStore? quickDefaultsStore = null)
    {
        _companyId = companyId;
        _query = query;
        _quickDefaultsStore = quickDefaultsStore;
        AccountOptions = new ObservableCollection<JournalAccountOptionVm>(accounts.OrderBy(x => x.Code));
        TransferAccountOptions = new ObservableCollection<JournalAccountOptionVm>(
            AccountOptions
                .Where(x => x.IsCashLike)
                .OrderBy(x => ResolveTransferRank(x))
                .ThenBy(x => x.Code));
        CurrencyOptions = new ObservableCollection<JournalCurrencyOptionVm>(
            currencies
                .OrderByDescending(x => x.IsBaseCurrency)
                .ThenBy(x => x.CurrencyCode));
        PostingPreviewLines = new ObservableCollection<VoucherPostingPreviewLineVm>();
        BalancePreviewCards = new ObservableCollection<VoucherBalancePreviewCardVm>();

        var savedDefaults = _quickDefaultsStore?.Load(JournalEntryType.TransferVoucher)
            ?? new JournalQuickDefaults(null, null, null, null, null);

        _selectedSourceAccountId = JournalAccountDefaultsResolver.ResolveExistingAccount(TransferAccountOptions, savedDefaults.SourceAccountId)
            ?? JournalAccountDefaultsResolver.ResolvePreferredCashAccount(AccountOptions);
        _selectedTargetAccountId = JournalAccountDefaultsResolver.ResolveExistingAccount(TransferAccountOptions, savedDefaults.TargetAccountId)
            ?? JournalAccountDefaultsResolver.ResolveMainTreasuryAccount(AccountOptions);

        if (_selectedSourceAccountId == _selectedTargetAccountId)
            _selectedTargetAccountId = AccountOptions.FirstOrDefault(x => x.Id != _selectedSourceAccountId)?.Id;

        var initialCurrency = CurrencyOptions.FirstOrDefault(x =>
                !string.IsNullOrWhiteSpace(savedDefaults.CurrencyCode)
                && string.Equals(x.CurrencyCode, savedDefaults.CurrencyCode, StringComparison.OrdinalIgnoreCase))
            ?? CurrencyOptions.FirstOrDefault(x => x.IsBaseCurrency)
            ?? CurrencyOptions.FirstOrDefault();
        if (initialCurrency is not null)
        {
            _selectedCurrencyCode = initialCurrency.CurrencyCode;
            _exchangeRate = Convert.ToDouble(initialCurrency.IsBaseCurrency ? 1m : initialCurrency.ExchangeRate);
        }

        QueuePreviewRefresh();
    }

    public ObservableCollection<JournalAccountOptionVm> AccountOptions { get; }
    public ObservableCollection<JournalAccountOptionVm> TransferAccountOptions { get; }
    public ObservableCollection<JournalCurrencyOptionVm> CurrencyOptions { get; }
    public ObservableCollection<VoucherPostingPreviewLineVm> PostingPreviewLines { get; }
    public ObservableCollection<VoucherBalancePreviewCardVm> BalancePreviewCards { get; }
    public string EffectBadgeText => "نقل نقدية";
    public string EffectSummaryText => "التحويل يحافظ على إجمالي الرصيد النقدي لكنه ينقل الأثر من الحساب المصدر إلى الحساب المستلم.";
    public string SourceGuideText => GetSelectedSourceAccount() is { } source
        ? $"المصدر الحالي: {source.DisplayText}. سيخرج الرصيد من هذا الحساب عند الترحيل."
        : "سيظهر الصندوق افتراضياً إن كان موجوداً، ويمكنك تغييره إلى أي حساب نقدي آخر.";
    public string TargetGuideText => GetSelectedTargetAccount() is { } target
        ? $"الحساب المستلم الحالي: {target.DisplayText}. هذا هو الطرف الذي سيصبح مديناً بعد التحويل."
        : "ستظهر الخزينة الرئيسية أو الأموال الجاهزة تلقائياً إن كانت معرفة، ويمكنك تغييرها.";
    public string DescriptionGuideText => "اكتب سبب التحويل بشكل مختصر، مثل نقل رصيد من الصندوق إلى الأموال الجاهزة أو إلى المصرف.";
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

    public Guid? SelectedSourceAccountId
    {
        get => _selectedSourceAccountId;
        set
        {
            if (_selectedSourceAccountId == value) return;
            _selectedSourceAccountId = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(SourceGuideText));
            QueuePreviewRefresh();
        }
    }

    public Guid? SelectedTargetAccountId
    {
        get => _selectedTargetAccountId;
        set
        {
            if (_selectedTargetAccountId == value) return;
            _selectedTargetAccountId = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(TargetGuideText));
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

        var source = GetSelectedSourceAccount();
        var target = GetSelectedTargetAccount();

        if (source is null || target is null)
        {
            error = "اختر حساب المصدر والحساب المحوّل إليه.";
            return false;
        }

        if (!source.IsCashLike || !target.IsCashLike)
        {
            error = "التحويل بين الحسابات مخصص للحسابات النقدية والخزائن والمصارف فقط.";
            return false;
        }

        if (source.Id == target.Id)
        {
            error = "يجب أن يختلف حساب المصدر عن الحساب المحوّل إليه.";
            return false;
        }

        if (SelectedCurrency is null)
        {
            error = "اختر العملة.";
            return false;
        }

        if (Amount <= 0)
        {
            error = "أدخل مبلغ التحويل.";
            return false;
        }

        if (ExchangeRate <= 0)
        {
            error = "أدخل سعر صرف صحيحاً.";
            return false;
        }

        var amount = Convert.ToDecimal(EquivalentAmount);
        if (amount <= 0)
        {
            error = "المبلغ المكافئ يجب أن يكون أكبر من صفر.";
            return false;
        }

        command = new CreateJournalEntryCommand(
            companyId,
            DateOnly.FromDateTime(EntryDate.Date),
            JournalEntryType.TransferVoucher,
            ReferenceNo,
            string.IsNullOrWhiteSpace(Description) ? "تحويل بين الحسابات" : Description.Trim(),
            SelectedCurrency.CurrencyCode,
            Convert.ToDecimal(ExchangeRate),
            Convert.ToDecimal(Amount),
            postNow,
            new[]
            {
                new CreateJournalEntryLineCommand(target.Id, amount, 0m, "الحساب المستلم"),
                new CreateJournalEntryLineCommand(source.Id, 0m, amount, "الحساب المحوِّل")
            });

        RememberQuickDefaults();

        return true;
    }

    private void RememberQuickDefaults()
    {
        _quickDefaultsStore?.Save(
            JournalEntryType.TransferVoucher,
            new JournalQuickDefaults(
                null,
                null,
                SelectedSourceAccountId,
                SelectedTargetAccountId,
                SelectedCurrency?.CurrencyCode));
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

            var source = GetSelectedSourceAccount();
            var target = GetSelectedTargetAccount();
            if (source is null || target is null || Amount <= 0 || ExchangeRate <= 0)
            {
                BalancePreviewCards.Clear();
                NotifyPreviewCollectionState();
                PreviewNoteText = "اختر حساب المصدر والحساب المستلم وأدخل مبلغ التحويل.";
                return;
            }

            var balances = await _query.GetPostedAccountBalancesAsync(
                _companyId,
                new[] { source.Id, target.Id },
                DateOnly.FromDateTime(EntryDate.Date));

            if (previewVersion != _previewRequestVersion)
                return;

            RebuildBalancePreview(source, target, balances);
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

        var source = GetSelectedSourceAccount();
        var target = GetSelectedTargetAccount();
        var amount = Convert.ToDecimal(EquivalentAmount);
        if (source is null || target is null || amount <= 0)
        {
            PreviewNoteText = "اختر حساب المصدر والحساب المستلم وأدخل مبلغ التحويل.";
            NotifyPreviewCollectionState();
            return;
        }

        PostingPreviewLines.Add(CreatePostingLine("مدين", target.DisplayText, amount));
        PostingPreviewLines.Add(CreatePostingLine("دائن", source.DisplayText, amount));
        PreviewNoteText = "التحويل الناتج: الحساب المستلم مدين، والحساب المحوِّل دائن.";
        NotifyPreviewCollectionState();
    }

    private void RebuildBalancePreview(
        JournalAccountOptionVm source,
        JournalAccountOptionVm target,
        IReadOnlyList<JournalAccountBalanceDto> balances)
    {
        BalancePreviewCards.Clear();

        var byId = balances.ToDictionary(x => x.AccountId);
        var amount = Convert.ToDecimal(EquivalentAmount);

        var sourceBalance = byId.TryGetValue(source.Id, out var sourceValue)
            ? sourceValue
            : new JournalAccountBalanceDto(source.Id, source.Code, source.NameAr, source.Nature, 0m, 0m);

        var targetBalance = byId.TryGetValue(target.Id, out var targetValue)
            ? targetValue
            : new JournalAccountBalanceDto(target.Id, target.Code, target.NameAr, target.Nature, 0m, 0m);

        BalancePreviewCards.Add(CreateBalanceCard("من حساب", source.DisplayText, sourceBalance, 0m, amount));
        BalancePreviewCards.Add(CreateBalanceCard("إلى حساب", target.DisplayText, targetBalance, amount, 0m));
        NotifyPreviewCollectionState();
    }

    private JournalAccountOptionVm? GetSelectedSourceAccount()
        => AccountOptions.FirstOrDefault(x => x.Id == SelectedSourceAccountId);

    private JournalAccountOptionVm? GetSelectedTargetAccount()
        => AccountOptions.FirstOrDefault(x => x.Id == SelectedTargetAccountId);

    private static VoucherPostingPreviewLineVm CreatePostingLine(string sideText, string accountText, decimal amount)
    {
        var isDebit = sideText == "مدين";
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

    private static int ResolveTransferRank(JournalAccountOptionVm account)
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

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}
