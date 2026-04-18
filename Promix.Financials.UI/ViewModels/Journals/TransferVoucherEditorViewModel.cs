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
using Promix.Financials.UI.ViewModels.Parties.Models;

namespace Promix.Financials.UI.ViewModels.Journals;

public sealed partial class TransferVoucherEditorViewModel : INotifyPropertyChanged
{
    private readonly Guid _companyId;
    private readonly IJournalEntriesQuery _query;
    private readonly IJournalQuickDefaultsStore? _quickDefaultsStore;
    private readonly TransferVoucherRulesService _rules;
    private readonly bool _canManage;
    private Guid? _entryId;
    private string? _entryNumber;
    private JournalEntryStatus _status = JournalEntryStatus.Draft;
    private VoucherEditorMode _mode;
    private string? _selectedCurrencyCode;
    private double _exchangeRate = 1;
    private DateTimeOffset _entryDate = DateTimeOffset.Now;
    private string _referenceNo = string.Empty;
    private string _description = string.Empty;
    private double _amount;
    private string _previewNoteText = "حدّد جهة المصدر وجهة الاستلام وأدخل مبلغ التحويل لعرض الأثر المحاسبي.";
    private string? _previewErrorMessage;
    private int _previewRequestVersion;
    private bool _isRefreshingEndpointState;
    private TransferSettlementMode _settlementMode = TransferSettlementMode.None;

    public TransferVoucherEditorViewModel(
        Guid companyId,
        IEnumerable<JournalAccountOptionVm> accounts,
        IEnumerable<JournalCurrencyOptionVm> currencies,
        IEnumerable<PartyOptionVm> parties,
        IJournalEntriesQuery query,
        IJournalQuickDefaultsStore? quickDefaultsStore = null,
        JournalEntryDetailDto? detail = null,
        bool canManage = false)
    {
        _companyId = companyId;
        _query = query;
        _quickDefaultsStore = quickDefaultsStore;
        _canManage = canManage;
        _rules = new TransferVoucherRulesService(accounts, parties);

        AccountOptions = new ObservableCollection<JournalAccountOptionVm>(accounts.OrderBy(x => x.Code));
        GeneralTransferAccountOptions = new ObservableCollection<JournalAccountOptionVm>(_rules.GeneralAccountOptions);
        PartyOptions = new ObservableCollection<PartyOptionVm>(_rules.PartyOptions);
        CurrencyOptions = new ObservableCollection<JournalCurrencyOptionVm>(
            currencies
                .OrderByDescending(x => x.IsBaseCurrency)
                .ThenBy(x => x.CurrencyCode));
        PostingPreviewLines = new ObservableCollection<VoucherPostingPreviewLineVm>();
        BalancePreviewCards = new ObservableCollection<VoucherBalancePreviewCardVm>();
        SourceEndpoint = new TransferEndpointEditorVm();
        TargetEndpoint = new TransferEndpointEditorVm();
        SourceEndpoint.PropertyChanged += Endpoint_PropertyChanged;
        TargetEndpoint.PropertyChanged += Endpoint_PropertyChanged;

        if (detail is not null)
        {
            LoadExistingEntry(detail);
            SetMode(VoucherEditorMode.View);
        }
        else
        {
            ApplyCreateDefaults();
            SetMode(VoucherEditorMode.Create);
        }

        RefreshEndpointState(SourceEndpoint);
        RefreshEndpointState(TargetEndpoint);
        QueuePreviewRefresh();
    }

    public ObservableCollection<JournalAccountOptionVm> AccountOptions { get; }
    public ObservableCollection<JournalAccountOptionVm> GeneralTransferAccountOptions { get; }
    public ObservableCollection<PartyOptionVm> PartyOptions { get; }
    public ObservableCollection<JournalCurrencyOptionVm> CurrencyOptions { get; }
    public ObservableCollection<VoucherPostingPreviewLineVm> PostingPreviewLines { get; }
    public ObservableCollection<VoucherBalancePreviewCardVm> BalancePreviewCards { get; }
    public TransferEndpointEditorVm SourceEndpoint { get; }
    public TransferEndpointEditorVm TargetEndpoint { get; }

    public string Title => string.IsNullOrWhiteSpace(_entryNumber) ? "تحويل بين الحسابات" : $"تحويل بين الحسابات · {_entryNumber}";
    public string Subtitle => _mode switch
    {
        VoucherEditorMode.View when _status == JournalEntryStatus.Posted => "عرض السند المرحل كما تم حفظه. السندات المرحلة للعرض فقط ولا يمكن تعديلها أو حذفها.",
        VoucherEditorMode.View => "عرض السند المحفوظ مع القيد الناتج وأثره على الجهتين.",
        VoucherEditorMode.EditPosted => "عدّل بيانات السند المرحل بحذر، وسيبقى مرحلًا بعد حفظ التغييرات.",
        VoucherEditorMode.EditDraft => "حدّث مسودة التحويل الحالية ثم احفظها أو رحّلها.",
        _ => "نقل رصيد بين طرفين أو بين حسابين عامين أو بين طرف وحساب عام من شاشة واحدة."
    };
    public string EntryMetaText => _entryId is null
        ? "سند جديد"
        : _status == JournalEntryStatus.Posted
            ? "مرحل"
            : "مسودة";
    public string EffectBadgeText => "تحويل عام";
    public string EffectSummaryText => "التحويل ينقل الأثر من الجهة المصدر إلى الجهة المستلمة دون تغيير إجمالي قيمة القيد.";
    public string DescriptionGuideText => "اكتب سبب النقل بشكل مختصر، مثل تحويل رصيد بين عميلين أو نقل مبلغ من طرف إلى حساب عام.";
    public string SourceGuideText => BuildEndpointGuideText(SourceEndpoint, "جهة المصدر", isSource: true);
    public string TargetGuideText => BuildEndpointGuideText(TargetEndpoint, "الجهة المستلمة", isSource: false);
    public JournalCurrencyOptionVm? SelectedCurrency => CurrencyOptions.FirstOrDefault(x => x.CurrencyCode == SelectedCurrencyCode);
    public string BaseCurrencyCode => CurrencyOptions.FirstOrDefault(x => x.IsBaseCurrency)?.CurrencyCode ?? "الأساسية";
    public string EquivalentLabel => $"المكافئ ({BaseCurrencyCode})";
    public string EquivalentAmountText => EquivalentAmount.ToString("N2");
    public string SettlementModeHintText => BuildSettlementHintText();
    public string FooterHintText => _mode switch
    {
        VoucherEditorMode.View when _status == JournalEntryStatus.Posted => "السند المرحل للعرض فقط حفاظًا على سلامة الأرصدة. يمكن تعديل المسودات أو حذفها فقط.",
        VoucherEditorMode.View when _canManage => "يمكنك تعديل هذه المسودة أو حذفها من هذا الشريط.",
        VoucherEditorMode.View => "وضع عرض فقط. لا يمكن تعديل هذا السند من هذه الجلسة.",
        VoucherEditorMode.EditPosted => "Ctrl+S لحفظ التعديلات على السند المرحل.",
        _ => "Ctrl+S للحفظ كمسودة  •  Ctrl+Enter للحفظ والترحيل",
    };
    public string CloseButtonText => _mode == VoucherEditorMode.View ? "إغلاق" : "إلغاء";
    public string SaveDraftButtonText => _entryId is null ? "حفظ كمسودة" : "حفظ";
    public string SaveAndPostButtonText => "حفظ وترحيل";
    public string SaveChangesButtonText => "حفظ التعديلات";
    public string EditButtonText => _status == JournalEntryStatus.Posted ? "تعديل السند" : "تعديل المسودة";
    public string DeleteButtonText => "حذف السند";
    public bool IsEditing => _mode != VoucherEditorMode.View;
    public bool CanManage => _canManage && _entryId is Guid;
    public bool CanEditEntry => CanManage && _status != JournalEntryStatus.Posted;
    public bool CanDeleteEntry => CanManage && _status != JournalEntryStatus.Posted;
    public bool CanEditExchangeRate => SelectedCurrency is not { IsBaseCurrency: true } && IsEditing;
    public double EquivalentAmount => Math.Round(Amount * ExchangeRate, 2);
    public Visibility EditControlsVisibility => IsEditing ? Visibility.Visible : Visibility.Collapsed;
    public Visibility ViewManageButtonsVisibility => CanEditEntry && _mode == VoucherEditorMode.View ? Visibility.Visible : Visibility.Collapsed;
    public Visibility DeleteButtonVisibility => CanDeleteEntry ? Visibility.Visible : Visibility.Collapsed;
    public Visibility SaveDraftButtonVisibility => _mode is VoucherEditorMode.Create or VoucherEditorMode.EditDraft ? Visibility.Visible : Visibility.Collapsed;
    public Visibility SaveAndPostButtonVisibility => _mode is VoucherEditorMode.Create or VoucherEditorMode.EditDraft ? Visibility.Visible : Visibility.Collapsed;
    public Visibility SaveChangesButtonVisibility => Visibility.Collapsed;
    public Visibility PreviewErrorVisibility => string.IsNullOrWhiteSpace(PreviewErrorMessage) ? Visibility.Collapsed : Visibility.Visible;
    public Visibility PostingPreviewVisibility => PostingPreviewLines.Count > 0 ? Visibility.Visible : Visibility.Collapsed;
    public Visibility PostingPreviewPlaceholderVisibility => PostingPreviewLines.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
    public Visibility BalancePreviewVisibility => BalancePreviewCards.Count > 0 ? Visibility.Visible : Visibility.Collapsed;
    public Visibility BalancePreviewPlaceholderVisibility => BalancePreviewCards.Count == 0 ? Visibility.Visible : Visibility.Collapsed;

    public int SettlementModeIndex
    {
        get => SettlementMode == TransferSettlementMode.Automatic ? 1 : 0;
        set => SettlementMode = value == 1 ? TransferSettlementMode.Automatic : TransferSettlementMode.None;
    }

    public TransferSettlementMode SettlementMode
    {
        get => _settlementMode;
        set
        {
            if (_settlementMode == value) return;
            _settlementMode = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(SettlementModeIndex));
            OnPropertyChanged(nameof(SettlementModeHintText));
            QueuePreviewRefresh();
        }
    }

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
            OnPropertyChanged(nameof(EquivalentAmountText));
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
            OnPropertyChanged(nameof(EquivalentAmountText));
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
            OnPropertyChanged(nameof(EquivalentAmountText));
            QueuePreviewRefresh();
        }
    }

    public bool TryBuildCommand(Guid companyId, bool postNow, out CreateJournalEntryCommand? command, out string error)
    {
        command = null;
        error = string.Empty;

        if (_entryId is not null)
        {
            error = "هذا السند موجود مسبقًا ويجب حفظه من خلال وضع التعديل.";
            return false;
        }

        if (!TryValidateTransfer(out var source, out var target, out var currency, out var amount, out error))
            return false;

        command = new CreateJournalEntryCommand(
            companyId,
            DateOnly.FromDateTime(EntryDate.Date),
            JournalEntryType.TransferVoucher,
            ReferenceNo,
            string.IsNullOrWhiteSpace(Description) ? "تحويل بين الحسابات" : Description.Trim(),
            currency.CurrencyCode,
            Convert.ToDecimal(ExchangeRate),
            Convert.ToDecimal(Amount),
            postNow,
            new[]
            {
                new CreateJournalEntryLineCommand(
                    target.AccountId,
                    amount,
                    0m,
                    $"الجهة المستلمة: {target.DisplayText}",
                    target.PartyName,
                    target.PartyId),
                new CreateJournalEntryLineCommand(
                    source.AccountId,
                    0m,
                    amount,
                    $"جهة المصدر: {source.DisplayText}",
                    source.PartyName,
                    source.PartyId)
            },
            SettlementMode);

        RememberQuickDefaults();
        return true;
    }

    public bool TryBuildUpdateCommand(Guid companyId, bool postNow, out UpdateJournalEntryCommand? command, out string error)
    {
        command = null;
        error = string.Empty;

        if (_entryId is not Guid entryId)
        {
            error = "لا يمكن حفظ التعديلات قبل تحميل السند.";
            return false;
        }

        if (_status == JournalEntryStatus.Posted)
        {
            error = "السندات المرحلة للعرض فقط ولا يمكن تعديلها.";
            return false;
        }

        if (!TryValidateTransfer(out var source, out var target, out var currency, out var amount, out error))
            return false;

        command = new UpdateJournalEntryCommand(
            companyId,
            entryId,
            DateOnly.FromDateTime(EntryDate.Date),
            ReferenceNo,
            string.IsNullOrWhiteSpace(Description) ? "تحويل بين الحسابات" : Description.Trim(),
            currency.CurrencyCode,
            Convert.ToDecimal(ExchangeRate),
            Convert.ToDecimal(Amount),
            postNow,
            new[]
            {
                new CreateJournalEntryLineCommand(
                    target.AccountId,
                    amount,
                    0m,
                    $"الجهة المستلمة: {target.DisplayText}",
                    target.PartyName,
                    target.PartyId),
                new CreateJournalEntryLineCommand(
                    source.AccountId,
                    0m,
                    amount,
                    $"جهة المصدر: {source.DisplayText}",
                    source.PartyName,
                    source.PartyId)
            },
            SettlementMode);

        RememberQuickDefaults();
        return true;
    }

    public bool TryBuildDeleteCommand(Guid companyId, out DeleteJournalEntryCommand? command, out string error)
    {
        command = null;
        error = string.Empty;

        if (!CanManage || _entryId is not Guid entryId)
        {
            error = "حذف السند غير متاح في هذا الوضع.";
            return false;
        }

        if (_status == JournalEntryStatus.Posted)
        {
            error = "السندات المرحلة للعرض فقط ولا يمكن حذفها.";
            return false;
        }

        command = new DeleteJournalEntryCommand(companyId, entryId);
        return true;
    }

    public void BeginEdit()
    {
        if (!CanEditEntry)
            return;

        SetMode(VoucherEditorMode.EditDraft);
    }

    private bool TryValidateTransfer(
        out ResolvedTransferEndpoint source,
        out ResolvedTransferEndpoint target,
        out JournalCurrencyOptionVm currency,
        out decimal amount,
        out string error)
    {
        error = string.Empty;
        source = null!;
        target = null!;
        currency = SelectedCurrency!;
        amount = Convert.ToDecimal(EquivalentAmount);

        if (!_rules.TryResolveEndpoint(SourceEndpoint, "المصدر", out var resolvedSource, out error))
            return false;

        if (!_rules.TryResolveEndpoint(TargetEndpoint, "المستلم", out var resolvedTarget, out error))
            return false;

        source = resolvedSource!;
        target = resolvedTarget!;

        if (source.AccountId == target.AccountId
            && source.PartyId == target.PartyId
            && source.PartySide == target.PartySide)
        {
            error = "يجب أن تختلف جهة المصدر عن الجهة المستلمة فعلياً.";
            return false;
        }

        if (currency is null)
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

        if (amount <= 0)
        {
            error = "المبلغ المكافئ يجب أن يكون أكبر من صفر.";
            return false;
        }

        return true;
    }

    private void ApplyCreateDefaults()
    {
        var savedDefaults = _quickDefaultsStore?.Load(JournalEntryType.TransferVoucher)
            ?? new JournalQuickDefaults(null, null, null, null, null);

        var defaultSourceAccountId = JournalAccountDefaultsResolver.ResolveExistingAccount(GeneralTransferAccountOptions, savedDefaults.SourceAccountId)
            ?? JournalAccountDefaultsResolver.ResolvePreferredCashAccount(GeneralTransferAccountOptions);
        var defaultTargetAccountId = JournalAccountDefaultsResolver.ResolveExistingAccount(GeneralTransferAccountOptions, savedDefaults.TargetAccountId)
            ?? JournalAccountDefaultsResolver.ResolveMainTreasuryAccount(GeneralTransferAccountOptions);

        SourceEndpoint.Mode = savedDefaults.SourceEndpointMode ?? TransferEndpointMode.GeneralAccount;
        TargetEndpoint.Mode = savedDefaults.TargetEndpointMode ?? TransferEndpointMode.GeneralAccount;
        SourceEndpoint.SelectedAccountId = defaultSourceAccountId;
        TargetEndpoint.SelectedAccountId = defaultTargetAccountId;
        SourceEndpoint.SelectedPartyId = _rules.GetParty(savedDefaults.SourcePartyId)?.Id;
        TargetEndpoint.SelectedPartyId = _rules.GetParty(savedDefaults.TargetPartyId)?.Id;
        SourceEndpoint.SelectedPartySide = savedDefaults.SourcePartySide;
        TargetEndpoint.SelectedPartySide = savedDefaults.TargetPartySide;
        SettlementMode = savedDefaults.TransferSettlementMode ?? TransferSettlementMode.None;

        if (SourceEndpoint.Mode == TransferEndpointMode.Party && SourceEndpoint.SelectedPartyId is null)
            SourceEndpoint.Mode = TransferEndpointMode.GeneralAccount;

        if (TargetEndpoint.Mode == TransferEndpointMode.Party && TargetEndpoint.SelectedPartyId is null)
            TargetEndpoint.Mode = TransferEndpointMode.GeneralAccount;

        if (SourceEndpoint.Mode == TransferEndpointMode.GeneralAccount
            && TargetEndpoint.Mode == TransferEndpointMode.GeneralAccount
            && SourceEndpoint.SelectedAccountId == TargetEndpoint.SelectedAccountId)
        {
            TargetEndpoint.SelectedAccountId = GeneralTransferAccountOptions.FirstOrDefault(x => x.Id != SourceEndpoint.SelectedAccountId)?.Id;
        }

        ApplyCurrencyDefaults(savedDefaults.CurrencyCode);
    }

    private void LoadExistingEntry(JournalEntryDetailDto detail)
    {
        _entryId = detail.Id;
        _entryNumber = detail.EntryNumber;
        _status = (JournalEntryStatus)detail.Status;
        _entryDate = new DateTimeOffset(detail.EntryDate.ToDateTime(TimeOnly.MinValue));
        _referenceNo = detail.ReferenceNo ?? string.Empty;
        _description = detail.Description ?? string.Empty;
        _selectedCurrencyCode = detail.CurrencyCode;
        _exchangeRate = Convert.ToDouble(detail.ExchangeRate <= 0 ? 1m : detail.ExchangeRate);
        _amount = Convert.ToDouble(detail.CurrencyAmount);
        _settlementMode = detail.TransferSettlementMode ?? TransferSettlementMode.None;

        LoadEndpoint(SourceEndpoint, detail.Lines.FirstOrDefault(x => x.Credit > 0));
        LoadEndpoint(TargetEndpoint, detail.Lines.FirstOrDefault(x => x.Debit > 0));

        NotifyHeaderStateChanged();
        OnPropertyChanged(nameof(SelectedCurrency));
        OnPropertyChanged(nameof(CanEditExchangeRate));
        OnPropertyChanged(nameof(EquivalentAmount));
        OnPropertyChanged(nameof(EquivalentAmountText));
        OnPropertyChanged(nameof(SettlementMode));
        OnPropertyChanged(nameof(SettlementModeIndex));
        OnPropertyChanged(nameof(SettlementModeHintText));
        OnPropertyChanged(nameof(SourceGuideText));
        OnPropertyChanged(nameof(TargetGuideText));
    }

    private void LoadEndpoint(TransferEndpointEditorVm endpoint, JournalEntryDetailLineDto? line)
    {
        if (line is null)
            return;

        if (line.PartyId is Guid partyId)
        {
            endpoint.Mode = TransferEndpointMode.Party;
            endpoint.SelectedPartyId = partyId;
            var party = _rules.GetParty(partyId);
            endpoint.SelectedPartySide = party?.ResolveSideForAccount(line.AccountId, _rules.ReceivableControlAccountId, _rules.PayableControlAccountId)
                ?? party?.GetSingleAvailableSide();
        }
        else
        {
            endpoint.Mode = TransferEndpointMode.GeneralAccount;
            endpoint.SelectedAccountId = line.AccountId;
        }
    }

    private void ApplyCurrencyDefaults(string? preferredCurrencyCode)
    {
        var initialCurrency = CurrencyOptions.FirstOrDefault(x =>
                !string.IsNullOrWhiteSpace(preferredCurrencyCode)
                && string.Equals(x.CurrencyCode, preferredCurrencyCode, StringComparison.OrdinalIgnoreCase))
            ?? CurrencyOptions.FirstOrDefault(x => x.IsBaseCurrency)
            ?? CurrencyOptions.FirstOrDefault();
        if (initialCurrency is not null)
        {
            _selectedCurrencyCode = initialCurrency.CurrencyCode;
            _exchangeRate = Convert.ToDouble(initialCurrency.IsBaseCurrency ? 1m : initialCurrency.ExchangeRate);
        }
    }

    private void RememberQuickDefaults()
    {
        _quickDefaultsStore?.Save(
            JournalEntryType.TransferVoucher,
            new JournalQuickDefaults(
                null,
                null,
                SourceEndpoint.Mode == TransferEndpointMode.GeneralAccount ? SourceEndpoint.SelectedAccountId : null,
                TargetEndpoint.Mode == TransferEndpointMode.GeneralAccount ? TargetEndpoint.SelectedAccountId : null,
                SelectedCurrency?.CurrencyCode,
                SourceEndpoint.Mode,
                TargetEndpoint.Mode,
                SourceEndpoint.Mode == TransferEndpointMode.Party ? SourceEndpoint.SelectedPartyId : null,
                TargetEndpoint.Mode == TransferEndpointMode.Party ? TargetEndpoint.SelectedPartyId : null,
                SourceEndpoint.Mode == TransferEndpointMode.Party ? SourceEndpoint.SelectedPartySide : null,
                TargetEndpoint.Mode == TransferEndpointMode.Party ? TargetEndpoint.SelectedPartySide : null,
                SettlementMode));
    }

    private void Endpoint_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (sender is not TransferEndpointEditorVm endpoint)
            return;

        if (e.PropertyName is not (nameof(TransferEndpointEditorVm.Mode)
            or nameof(TransferEndpointEditorVm.SelectedAccountId)
            or nameof(TransferEndpointEditorVm.SelectedPartyId)
            or nameof(TransferEndpointEditorVm.SelectedPartySide)))
        {
            return;
        }

        RefreshEndpointState(endpoint);
        if (ReferenceEquals(endpoint, SourceEndpoint))
            OnPropertyChanged(nameof(SourceGuideText));
        else
            OnPropertyChanged(nameof(TargetGuideText));

        OnPropertyChanged(nameof(SettlementModeHintText));
        QueuePreviewRefresh();
    }

    private void RefreshEndpointState(TransferEndpointEditorVm endpoint)
    {
        if (_isRefreshingEndpointState)
            return;

        _isRefreshingEndpointState = true;
        try
        {
            if (endpoint.Mode == TransferEndpointMode.GeneralAccount)
            {
                endpoint.RequiresPartySide = false;
                endpoint.ResolvedAccountText = "في وضع الحساب العام يظهر الحساب المختار مباشرة من حقل الحساب.";
                return;
            }

            var party = _rules.GetParty(endpoint.SelectedPartyId);
            if (party is null)
            {
                endpoint.RequiresPartySide = false;
                endpoint.ResolvedAccountText = "اختر الطرف أولاً ليظهر الحساب المرتبط به تلقائياً.";
                return;
            }

            var normalizedSide = _rules.NormalizePartySide(party, endpoint.SelectedPartySide);
            endpoint.RequiresPartySide = !party.GetSingleAvailableSide().HasValue;
            if (endpoint.SelectedPartySide != normalizedSide)
                endpoint.SelectedPartySide = normalizedSide;

            if (endpoint.RequiresPartySide && normalizedSide is null)
            {
                endpoint.ResolvedAccountText = "حدد هل سيعامل هذا الطرف كعميل أم كمورد لعرض الحساب المرتبط.";
                return;
            }

            var resolvedAccountId = normalizedSide is null
                ? null
                : party.ResolveTransferAccountId(normalizedSide.Value, _rules.ReceivableControlAccountId, _rules.PayableControlAccountId);
            var resolvedAccount = _rules.GetAccount(resolvedAccountId);
            endpoint.ResolvedAccountText = resolvedAccount is null
                ? "تعذر تحديد الحساب المرتبط بهذا الطرف حالياً."
                : resolvedAccount.DisplayText;
        }
        finally
        {
            _isRefreshingEndpointState = false;
        }
    }

    private string BuildEndpointGuideText(TransferEndpointEditorVm endpoint, string label, bool isSource)
    {
        if (endpoint.Mode == TransferEndpointMode.GeneralAccount)
        {
            var account = _rules.GetAccount(endpoint.SelectedAccountId);
            return account is null
                ? $"{label}: اختر حساباً عاماً من الشجرة المحاسبية."
                : $"{label}: {account.DisplayText}. {(isSource ? "سيُقيد دائناً" : "سيُقيد مديناً")} عند ترحيل السند.";
        }

        if (!_rules.TryResolveEndpoint(endpoint, label, out var resolved, out _))
            return $"{label}: اختر الطرف ثم حدد جهته المحاسبية إن لزم ليظهر الحساب المرتبط.";

        return $"{label}: {resolved!.DisplayText}. {(isSource ? "سيخرج الرصيد من هذه الجهة" : "ستستقبل هذه الجهة الرصيد")} عند الترحيل.";
    }

    private string BuildSettlementHintText()
    {
        var containsParty = SourceEndpoint.Mode == TransferEndpointMode.Party || TargetEndpoint.Mode == TransferEndpointMode.Party;
        if (!containsParty)
            return "التسوية التلقائية غير مؤثرة هنا لأن التحويل لا يتضمن أطرافاً. سيُحفظ الخيار مع السند فقط.";

        return SettlementMode == TransferSettlementMode.Automatic
            ? "بعد الترحيل سيحاول النظام إعادة بناء تسويات البنود المفتوحة للأطراف المتأثرة تلقائياً."
            : "سيُرحّل القيد فقط دون محاولة تسوية البنود المفتوحة تلقائياً. يمكنك معالجة التسوية لاحقاً بشكل مستقل.";
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

            if (!TryResolvePreviewEndpoints(out var source, out var target))
            {
                BalancePreviewCards.Clear();
                NotifyPreviewCollectionState();
                return;
            }

            if (Amount <= 0 || ExchangeRate <= 0)
            {
                BalancePreviewCards.Clear();
                NotifyPreviewCollectionState();
                return;
            }

            var accountIds = new[] { source.AccountId, target.AccountId }.Distinct().ToArray();
            var balances = await _query.GetPostedAccountBalancesAsync(
                _companyId,
                accountIds,
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

    private bool TryResolvePreviewEndpoints(out ResolvedTransferEndpoint source, out ResolvedTransferEndpoint target)
    {
        source = null!;
        target = null!;

        if (!_rules.TryResolveEndpoint(SourceEndpoint, "المصدر", out var resolvedSource, out _))
        {
            PreviewNoteText = "حدّد جهة المصدر بشكل صالح لإظهار معاينة القيد.";
            NotifyPreviewCollectionState();
            return false;
        }

        if (!_rules.TryResolveEndpoint(TargetEndpoint, "المستلم", out var resolvedTarget, out _))
        {
            PreviewNoteText = "حدّد الجهة المستلمة بشكل صالح لإظهار معاينة القيد.";
            NotifyPreviewCollectionState();
            return false;
        }

        if (Amount <= 0)
        {
            PreviewNoteText = "أدخل مبلغ التحويل لعرض القيد الناتج والأثر على الرصيد.";
            NotifyPreviewCollectionState();
            return false;
        }

        source = resolvedSource!;
        target = resolvedTarget!;
        return true;
    }

    private void RebuildPostingPreview()
    {
        PostingPreviewLines.Clear();

        if (!TryResolvePreviewEndpoints(out var source, out var target))
            return;

        var amount = Convert.ToDecimal(EquivalentAmount);
        if (amount <= 0)
        {
            PreviewNoteText = "أدخل مبلغاً صالحاً لإظهار القيد الناتج.";
            NotifyPreviewCollectionState();
            return;
        }

        PostingPreviewLines.Add(CreatePostingLine("مدين", target.DisplayText, amount));
        PostingPreviewLines.Add(CreatePostingLine("دائن", source.DisplayText, amount));
        PreviewNoteText = SettlementMode == TransferSettlementMode.Automatic
            ? "القيد الناتج: الجهة المستلمة مدين، وجهة المصدر دائن. عند الترحيل سيعاد بناء التسويات تلقائياً إذا وُجدت أطراف."
            : "القيد الناتج: الجهة المستلمة مدين، وجهة المصدر دائن.";
        NotifyPreviewCollectionState();
    }

    private void RebuildBalancePreview(
        ResolvedTransferEndpoint source,
        ResolvedTransferEndpoint target,
        IReadOnlyList<JournalAccountBalanceDto> balances)
    {
        BalancePreviewCards.Clear();

        var byId = balances.ToDictionary(x => x.AccountId);
        var amount = Convert.ToDecimal(EquivalentAmount);

        var sourceBalance = byId.TryGetValue(source.AccountId, out var sourceValue)
            ? sourceValue
            : new JournalAccountBalanceDto(source.AccountId, string.Empty, source.AccountDisplayText, AccountNature.Debit, 0m, 0m);

        var targetBalance = byId.TryGetValue(target.AccountId, out var targetValue)
            ? targetValue
            : new JournalAccountBalanceDto(target.AccountId, string.Empty, target.AccountDisplayText, AccountNature.Debit, 0m, 0m);

        BalancePreviewCards.Add(CreateBalanceCard("من جهة", source.DisplayText, sourceBalance, 0m, amount));
        BalancePreviewCards.Add(CreateBalanceCard("إلى جهة", target.DisplayText, targetBalance, amount, 0m));
        NotifyPreviewCollectionState();
    }

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

    private void SetMode(VoucherEditorMode mode)
    {
        if (_mode == mode) return;
        _mode = mode;
        NotifyModeChanged();
    }

    private void NotifyModeChanged()
    {
        OnPropertyChanged(nameof(Subtitle));
        OnPropertyChanged(nameof(FooterHintText));
        OnPropertyChanged(nameof(CloseButtonText));
        OnPropertyChanged(nameof(IsEditing));
        OnPropertyChanged(nameof(CanManage));
        OnPropertyChanged(nameof(CanEditEntry));
        OnPropertyChanged(nameof(CanDeleteEntry));
        OnPropertyChanged(nameof(CanEditExchangeRate));
        OnPropertyChanged(nameof(EditControlsVisibility));
        OnPropertyChanged(nameof(ViewManageButtonsVisibility));
        OnPropertyChanged(nameof(DeleteButtonVisibility));
        OnPropertyChanged(nameof(SaveDraftButtonVisibility));
        OnPropertyChanged(nameof(SaveAndPostButtonVisibility));
        OnPropertyChanged(nameof(SaveChangesButtonVisibility));
    }

    private void NotifyHeaderStateChanged()
    {
        OnPropertyChanged(nameof(Title));
        OnPropertyChanged(nameof(Subtitle));
        OnPropertyChanged(nameof(EntryMetaText));
        OnPropertyChanged(nameof(EditButtonText));
        NotifyModeChanged();
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}
