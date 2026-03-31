using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Promix.Financials.Application.Features.Journals.Commands;
using Promix.Financials.Application.Features.Journals.Queries;
using Promix.Financials.Application.Features.Parties.Queries;
using Promix.Financials.Domain.Enums;
using Promix.Financials.UI.Services.Journals;
using Promix.Financials.UI.ViewModels.Journals.Models;
using Promix.Financials.UI.ViewModels.Parties.Models;

namespace Promix.Financials.UI.ViewModels.Journals;

public sealed class SimpleVoucherEditorViewModel : INotifyPropertyChanged
{
    private readonly Guid _companyId;
    private readonly JournalEntryType _type;
    private readonly string _baseTitle;
    private readonly string _baseSubtitle;
    private readonly IJournalEntriesQuery _query;
    private readonly IPartyQuery _partyQuery;
    private readonly IJournalQuickDefaultsStore? _quickDefaultsStore;
    private Guid? _entryId;
    private string? _entryNumber;
    private JournalEntryStatus _status = JournalEntryStatus.Draft;
    private VoucherEditorMode _mode;
    private readonly bool _canManage;
    private Guid? _selectedCashAccountId;
    private string? _selectedCurrencyCode;
    private double _exchangeRate = 1;
    private DateTimeOffset _entryDate = DateTimeOffset.Now;
    private string _referenceNo = string.Empty;
    private string _description = string.Empty;
    private string _previewNoteText;
    private string? _previewErrorMessage;
    private int _previewRequestVersion;

    public SimpleVoucherEditorViewModel(
        Guid companyId,
        JournalEntryType type,
        string title,
        string subtitle,
        IEnumerable<JournalAccountOptionVm> accounts,
        IEnumerable<JournalCurrencyOptionVm> currencies,
        IEnumerable<PartyOptionVm> parties,
        IJournalEntriesQuery query,
        IPartyQuery partyQuery,
        IJournalQuickDefaultsStore? quickDefaultsStore = null,
        JournalEntryDetailDto? detail = null,
        bool canManage = false)
    {
        _companyId = companyId;
        _type = type;
        _baseTitle = title;
        _baseSubtitle = subtitle;
        _query = query;
        _partyQuery = partyQuery;
        _quickDefaultsStore = quickDefaultsStore;
        _canManage = canManage;
        _previewNoteText = type == JournalEntryType.ReceiptVoucher
            ? "أدخل حساب النقدية وأضف الحسابات المقابلة لعرض القيد الناتج."
            : "أدخل حساب النقدية وأضف الحسابات المصروف عليها لعرض القيد الناتج.";

        AccountOptions = new ObservableCollection<JournalAccountOptionVm>(accounts.OrderBy(x => x.Code));
        CashAccountOptions = new ObservableCollection<JournalAccountOptionVm>(
            AccountOptions
                .Where(x => x.IsCashLike)
                .OrderBy(x => ResolveCashRank(x))
                .ThenBy(x => x.Code));
        CounterpartyAccountOptions = new ObservableCollection<JournalAccountOptionVm>(
            AccountOptions
                .Where(x => !x.IsCashLike && !x.IsLegacyPartyLinkedAccount)
                .OrderBy(x => x.Code));
        GeneralCounterpartyAccountOptions = new ObservableCollection<JournalAccountOptionVm>(
            CounterpartyAccountOptions
                .Where(x => !x.IsPartyControlAccount)
                .OrderBy(x => x.Code));
        CurrencyOptions = new ObservableCollection<JournalCurrencyOptionVm>(
            currencies
                .OrderByDescending(x => x.IsBaseCurrency)
                .ThenBy(x => x.CurrencyCode));
        PartyOptions = new ObservableCollection<PartyOptionVm>(
            parties
                .Where(x => x.IsActive && x.SupportsVoucherType(type))
                .OrderBy(x => x.Code));
        Lines = new ObservableCollection<VoucherCounterpartyLineEditorVm>();
        PostingPreviewLines = new ObservableCollection<VoucherPostingPreviewLineVm>();
        BalancePreviewCards = new ObservableCollection<VoucherBalancePreviewCardVm>();

        Lines.CollectionChanged += Lines_CollectionChanged;

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

        if (Lines.Count == 0)
            AddLine();

        QueuePreviewRefresh();
    }

    public ObservableCollection<JournalAccountOptionVm> AccountOptions { get; }
    public ObservableCollection<JournalAccountOptionVm> CashAccountOptions { get; }
    public ObservableCollection<JournalAccountOptionVm> CounterpartyAccountOptions { get; }
    public ObservableCollection<JournalAccountOptionVm> GeneralCounterpartyAccountOptions { get; }
    public ObservableCollection<JournalCurrencyOptionVm> CurrencyOptions { get; }
    public ObservableCollection<PartyOptionVm> PartyOptions { get; }
    public ObservableCollection<VoucherCounterpartyLineEditorVm> Lines { get; }
    public ObservableCollection<VoucherPostingPreviewLineVm> PostingPreviewLines { get; }
    public ObservableCollection<VoucherBalancePreviewCardVm> BalancePreviewCards { get; }

    public string Title => string.IsNullOrWhiteSpace(_entryNumber) ? _baseTitle : $"{_baseTitle} · {_entryNumber}";
    public string Subtitle => _mode switch
    {
        VoucherEditorMode.View when _status == JournalEntryStatus.Posted => "عرض السند المرحل كما تم حفظه، ويمكن للمدير فقط فتح وضع التعديل.",
        VoucherEditorMode.View => "عرض السند المحفوظ مع تفاصيله والقيد الناتج.",
        VoucherEditorMode.EditPosted => "عدّل بيانات السند المرحل بحذر، وسيبقى مرحلًا بعد حفظ التغييرات.",
        VoucherEditorMode.EditDraft => "حدّث المسودة الحالية ثم احفظها أو رحّلها مباشرة من نفس الشاشة.",
        _ => _baseSubtitle
    };
    public string EntryMetaText => _entryId is null
        ? "سند جديد"
        : _status == JournalEntryStatus.Posted
            ? "مرحل"
            : "مسودة";
    public string EffectBadgeText => _type == JournalEntryType.ReceiptVoucher ? "يزيد النقدية" : "يخفض النقدية";
    public string EffectSummaryText => _type == JournalEntryType.ReceiptVoucher
        ? "سيُسجل الحساب النقدي مدينًا بإجمالي السطور، وتقابلها حسابات دائنة متعددة."
        : "سيُسجل الحساب النقدي دائنًا بإجمالي السطور، وتقابله حسابات مدينة متعددة.";
    public string CounterpartySectionTitle => _type == JournalEntryType.ReceiptVoucher
        ? "تفاصيل الحسابات المقابلة"
        : "تفاصيل الصرف على الحسابات";
    public string CounterpartySectionHint => _type == JournalEntryType.ReceiptVoucher
        ? "أضف سطرًا لكل حساب يمثل مصدر القبض أو الطرف المقابل."
        : "أضف سطرًا لكل حساب تريد الصرف عليه ضمن نفس السند.";
    public string CashAccountGuideText => GetSelectedCashAccount() is { } cashAccount
        ? $"الحساب الحالي: {cashAccount.DisplayText}. سيتأثر هذا الحساب بإجمالي السند."
        : "اختر الصندوق أو البنك أو الحساب النقدي الذي ستدخل عليه الحركة.";
    public string DescriptionGuideText => "البيان العام يظهر على رأس السند، بينما يمكنك تخصيص وصف مختصر لكل سطر عند الحاجة.";
    public string TotalAmountLabel => _type == JournalEntryType.ReceiptVoucher ? "إجمالي المقبوض" : "إجمالي المصروف";
    public string EquivalentLabel => $"المكافئ ({BaseCurrencyCode})";
    public string BaseCurrencyCode => CurrencyOptions.FirstOrDefault(x => x.IsBaseCurrency)?.CurrencyCode ?? "الأساسية";
    public string TotalAmountText => TotalAmount.ToString("N2");
    public string EquivalentAmountText => EquivalentAmount.ToString("N2");
    public string TotalLinesText => $"عدد السطور الصالحة: {GetActiveLines().Count}";
    public string FooterHintText => _mode switch
    {
        VoucherEditorMode.View when _canManage => "افتح وضع التعديل أو الحذف من هذا الشريط. غير المدير يعرض السند فقط.",
        VoucherEditorMode.View => "وضع عرض فقط. لا يمكن تعديل هذا السند من هذه الجلسة.",
        VoucherEditorMode.EditPosted => "Ctrl+S لحفظ التعديلات على السند المرحل.",
        _ => "Ctrl+S للحفظ كمسودة  •  Ctrl+Enter للحفظ والترحيل  •  Ctrl+Shift+N لإضافة سطر",
    };
    public string CloseButtonText => _mode == VoucherEditorMode.View ? "إغلاق" : "إلغاء";
    public string SaveDraftButtonText => _entryId is null ? "حفظ كمسودة" : "حفظ";
    public string SaveAndPostButtonText => "حفظ وترحيل";
    public string SaveChangesButtonText => "حفظ التعديلات";
    public string EditButtonText => _status == JournalEntryStatus.Posted ? "تعديل السند" : "تعديل المسودة";
    public string DeleteButtonText => "حذف السند";
    public bool IsEditing => _mode != VoucherEditorMode.View;
    public bool IsExistingEntry => _entryId is Guid;
    public bool CanManage => _canManage && IsExistingEntry;
    public bool CanEditExchangeRate => SelectedCurrency is not { IsBaseCurrency: true } && IsEditing;
    public Visibility EditControlsVisibility => IsEditing ? Visibility.Visible : Visibility.Collapsed;
    public Visibility ViewManageButtonsVisibility => CanManage && _mode == VoucherEditorMode.View ? Visibility.Visible : Visibility.Collapsed;
    public Visibility DeleteButtonVisibility => CanManage ? Visibility.Visible : Visibility.Collapsed;
    public Visibility SaveDraftButtonVisibility => _mode is VoucherEditorMode.Create or VoucherEditorMode.EditDraft ? Visibility.Visible : Visibility.Collapsed;
    public Visibility SaveAndPostButtonVisibility => _mode is VoucherEditorMode.Create or VoucherEditorMode.EditDraft ? Visibility.Visible : Visibility.Collapsed;
    public Visibility SaveChangesButtonVisibility => _mode == VoucherEditorMode.EditPosted ? Visibility.Visible : Visibility.Collapsed;
    public Visibility AddLineButtonVisibility => IsEditing ? Visibility.Visible : Visibility.Collapsed;
    public Visibility PreviewErrorVisibility => string.IsNullOrWhiteSpace(PreviewErrorMessage) ? Visibility.Collapsed : Visibility.Visible;
    public Visibility PostingPreviewVisibility => PostingPreviewLines.Count > 0 ? Visibility.Visible : Visibility.Collapsed;
    public Visibility PostingPreviewPlaceholderVisibility => PostingPreviewLines.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
    public Visibility BalancePreviewVisibility => BalancePreviewCards.Count > 0 ? Visibility.Visible : Visibility.Collapsed;
    public Visibility BalancePreviewPlaceholderVisibility => BalancePreviewCards.Count == 0 ? Visibility.Visible : Visibility.Collapsed;

    public JournalCurrencyOptionVm? SelectedCurrency => CurrencyOptions.FirstOrDefault(x => x.CurrencyCode == SelectedCurrencyCode);
    private Guid? ReceivableControlAccountId => AccountOptions.FirstOrDefault(x => x.IsReceivableControl)?.Id;
    private Guid? PayableControlAccountId => AccountOptions.FirstOrDefault(x => x.IsPayableControl)?.Id;

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
            OnPropertyChanged(nameof(TotalAmountText));
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
            OnPropertyChanged(nameof(TotalAmountText));
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

    public double TotalAmount => Math.Round(GetActiveLines().Sum(x => x.Amount), SelectedCurrency?.DecimalPlaces ?? 2);
    public double EquivalentAmount => Math.Round(TotalAmount * ExchangeRate, 2);

    public void ApplyAccountStatementDefaults(Guid accountId)
    {
        if (accountId == Guid.Empty || IsExistingEntry)
            return;

        if (CashAccountOptions.Any(x => x.Id == accountId))
        {
            SelectedCashAccountId = accountId;
            return;
        }

        var firstLine = Lines.FirstOrDefault() ?? AddLine();
        firstLine.SelectedAccountId = JournalAccountDefaultsResolver.ResolveExistingAccount(CounterpartyAccountOptions, accountId)
            ?? firstLine.SelectedAccountId;
    }

    public VoucherCounterpartyLineEditorVm AddLine()
    {
        var line = new VoucherCounterpartyLineEditorVm
        {
            SelectedAccountId = ResolveDefaultCounterpartyAccount(),
            Amount = 0
        };

        Lines.Add(line);
        return line;
    }

    public void RemoveLine(VoucherCounterpartyLineEditorVm? line)
    {
        if (line is null)
            return;

        if (Lines.Count == 1)
        {
            line.SelectedAccountId = null;
            line.SelectedPartyId = null;
            line.PartyName = string.Empty;
            line.Description = string.Empty;
            line.Amount = 0;
            line.OpenItems.Clear();
            line.OpenItemsSummaryText = string.Empty;
            QueuePreviewRefresh();
            return;
        }

        Lines.Remove(line);
    }

    public void BeginEdit()
    {
        if (!CanManage)
            return;

        SetMode(_status == JournalEntryStatus.Posted ? VoucherEditorMode.EditPosted : VoucherEditorMode.EditDraft);
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

        if (!TryValidateVoucher(out var cashAccount, out var currency, out var lines, out error))
            return false;

        command = new CreateJournalEntryCommand(
            companyId,
            DateOnly.FromDateTime(EntryDate.Date),
            _type,
            ReferenceNo,
            NormalizeHeaderDescription(),
            currency.CurrencyCode,
            Convert.ToDecimal(ExchangeRate),
            Convert.ToDecimal(TotalAmount),
            postNow,
            BuildCommandLines(cashAccount.Id, lines));

        RememberQuickDefaults(lines.FirstOrDefault()?.AccountId);
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

        if (!TryValidateVoucher(out var cashAccount, out var currency, out var lines, out error))
            return false;

        command = new UpdateJournalEntryCommand(
            companyId,
            entryId,
            DateOnly.FromDateTime(EntryDate.Date),
            ReferenceNo,
            NormalizeHeaderDescription(),
            currency.CurrencyCode,
            Convert.ToDecimal(ExchangeRate),
            Convert.ToDecimal(TotalAmount),
            postNow,
            BuildCommandLines(cashAccount.Id, lines));

        RememberQuickDefaults(lines.FirstOrDefault()?.AccountId);
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

        command = new DeleteJournalEntryCommand(companyId, entryId);
        return true;
    }

    private void ApplyCreateDefaults()
    {
        var savedDefaults = _quickDefaultsStore?.Load(_type)
            ?? new JournalQuickDefaults(null, null, null, null, null);

        _selectedCashAccountId = JournalAccountDefaultsResolver.ResolveExistingAccount(CashAccountOptions, savedDefaults.CashAccountId)
            ?? JournalAccountDefaultsResolver.ResolvePreferredCashAccount(CashAccountOptions)
            ?? JournalAccountDefaultsResolver.ResolvePreferredCashAccount(AccountOptions);

        ApplyCurrencyDefaults(savedDefaults.CurrencyCode);
        AddLine();

        var firstLine = Lines.First();
        firstLine.SelectedAccountId = JournalAccountDefaultsResolver.ResolveExistingAccount(GeneralCounterpartyAccountOptions, savedDefaults.CounterpartyAccountId)
            ?? ResolveDefaultCounterpartyAccount();
    }

    private void LoadExistingEntry(JournalEntryDetailDto detail)
    {
        _entryId = detail.Id;
        _entryNumber = detail.EntryNumber;
        _status = (JournalEntryStatus)detail.Status;
        _entryDate = new DateTimeOffset(detail.EntryDate.ToDateTime(TimeOnly.MinValue));
        _referenceNo = detail.ReferenceNo ?? string.Empty;
        _description = detail.Description ?? string.Empty;
        ApplyCurrencyDefaults(detail.CurrencyCode);
        _selectedCurrencyCode = detail.CurrencyCode;
        _exchangeRate = Convert.ToDouble(detail.ExchangeRate <= 0 ? 1m : detail.ExchangeRate);

        OnPropertyChanged(nameof(SelectedCurrencyCode));
        OnPropertyChanged(nameof(ExchangeRate));
        OnPropertyChanged(nameof(SelectedCurrency));
        OnPropertyChanged(nameof(CanEditExchangeRate));

        PopulateLinesFromDetail(detail);
        NotifyHeaderStateChanged();
    }

    private void PopulateLinesFromDetail(JournalEntryDetailDto detail)
    {
        var cashSideIsDebit = _type == JournalEntryType.ReceiptVoucher;
        var cashCandidates = detail.Lines
            .Where(line => GetLineSideAmount(line, cashSideIsDebit) > 0)
            .OrderByDescending(line => GetAccount(line.AccountId)?.IsCashLike == true)
            .ThenByDescending(line => GetLineSideAmount(line, cashSideIsDebit))
            .ToList();

        var cashLine = cashCandidates.FirstOrDefault();
        _selectedCashAccountId = cashLine?.AccountId
            ?? CashAccountOptions.FirstOrDefault()?.Id;

        var counterpartyLines = detail.Lines
            .Where(line => cashLine is null || line != cashLine)
            .Where(line => GetLineSideAmount(line, !cashSideIsDebit) > 0)
            .ToList();

        Lines.Clear();

        var exchangeRate = detail.ExchangeRate <= 0 ? 1m : detail.ExchangeRate;
        var currencyDecimals = SelectedCurrency?.DecimalPlaces ?? (byte)2;
        foreach (var line in counterpartyLines)
        {
            var amount = decimal.Round(GetLineSideAmount(line, !cashSideIsDebit) / exchangeRate, currencyDecimals, MidpointRounding.AwayFromZero);
            var editorLine = new VoucherCounterpartyLineEditorVm
            {
                EntryKind = line.PartyId is Guid ? VoucherCounterpartyEntryKind.Party : VoucherCounterpartyEntryKind.GeneralAccount,
                SelectedAccountId = line.AccountId,
                SelectedPartyId = line.PartyId,
                PartyName = line.PartyName ?? string.Empty,
                Description = line.Description ?? string.Empty,
                Amount = Convert.ToDouble(amount)
            };
            if (editorLine.EntryKind == VoucherCounterpartyEntryKind.Party)
                editorLine.AutomaticPartyAccountText = BuildAutomaticPartyAccountText(line.AccountId);
            Lines.Add(editorLine);
        }

        if (Lines.Count == 0)
            AddLine();

        var currentTotal = Lines.Sum(x => Convert.ToDecimal(x.Amount));
        var delta = decimal.Round(detail.CurrencyAmount - currentTotal, currencyDecimals, MidpointRounding.AwayFromZero);
        if (delta != 0m)
            Lines[0].Amount = Math.Max(0, Lines[0].Amount + Convert.ToDouble(delta));
    }

    private void ApplyCurrencyDefaults(string? preferredCurrencyCode)
    {
        var selected = CurrencyOptions.FirstOrDefault(x =>
                !string.IsNullOrWhiteSpace(preferredCurrencyCode)
                && string.Equals(x.CurrencyCode, preferredCurrencyCode, StringComparison.OrdinalIgnoreCase))
            ?? CurrencyOptions.FirstOrDefault(x => x.IsBaseCurrency)
            ?? CurrencyOptions.FirstOrDefault();

        if (selected is null)
            return;

        _selectedCurrencyCode = selected.CurrencyCode;
        _exchangeRate = Convert.ToDouble(selected.IsBaseCurrency ? 1m : selected.ExchangeRate);
    }

    private bool TryValidateVoucher(
        out JournalAccountOptionVm cashAccount,
        out JournalCurrencyOptionVm currency,
        out List<ValidatedVoucherLine> lines,
        out string error)
    {
        error = string.Empty;
        lines = new List<ValidatedVoucherLine>();

        cashAccount = GetSelectedCashAccount()!;
        if (cashAccount is null)
        {
            error = "اختر الحساب النقدي.";
            currency = null!;
            return false;
        }

        currency = SelectedCurrency!;
        if (currency is null)
        {
            error = "اختر العملة.";
            return false;
        }

        if (ExchangeRate <= 0)
        {
            error = "أدخل سعر صرف صحيحًا.";
            return false;
        }

        foreach (var line in Lines)
        {
            if (line.IsEmpty)
                continue;

            if (line.SelectedAccountId is not Guid accountId || accountId == Guid.Empty)
            {
                error = "اختر الحساب المقابل لكل سطر قبل الحفظ.";
                return false;
            }

            if (accountId == cashAccount.Id)
            {
                error = "لا يمكن استخدام الحساب النقدي نفسه كسطر مقابل.";
                return false;
            }

            var account = CounterpartyAccountOptions.FirstOrDefault(x => x.Id == accountId)
                ?? AccountOptions.FirstOrDefault(x => x.Id == accountId);
            if (account is null)
            {
                error = "أحد الحسابات المقابلة لم يعد متاحًا.";
                return false;
            }

            if (line.Amount <= 0)
            {
                error = "كل سطر يجب أن يحتوي مبلغًا أكبر من صفر.";
                return false;
            }

            var enteredAmount = decimal.Round(Convert.ToDecimal(line.Amount), currency.DecimalPlaces, MidpointRounding.AwayFromZero);
            var equivalentAmount = decimal.Round(enteredAmount * Convert.ToDecimal(ExchangeRate), 2, MidpointRounding.AwayFromZero);
            if (equivalentAmount <= 0)
            {
                error = "أحد السطور ينتج مبلغًا مكافئًا غير صالح.";
                return false;
            }

            var partyId = line.SelectedPartyId;
            string? partyName;
            if (partyId is Guid resolvedPartyId)
            {
                var party = PartyOptions.FirstOrDefault(x => x.Id == resolvedPartyId);
                if (party is null)
                {
                    error = "أحد الأطراف المختارة لم يعد متاحًا.";
                    return false;
                }

                var expectedAccountId = party.ResolveVoucherAccountId(_type, ReceivableControlAccountId, PayableControlAccountId);
                if (expectedAccountId != accountId)
                {
                    error = "عند اختيار طرف، سيُستخدم الحساب المرتبط بهذا الطرف تلقائياً ولا يمكن استبداله من هذا السطر.";
                    return false;
                }

                partyName = party.NameAr;
            }
            else
            {
                if (account.IsPartyControlAccount)
                {
                    error = "لا يمكن استخدام حسابات ضبط العملاء أو الموردين بدون اختيار طرف.";
                    return false;
                }

                if (account.IsLegacyPartyLinkedAccount)
                {
                    error = "هذا حساب ذمة قديم مرتبط بطرف. استخدم الطرف مباشرة أو اختر حساباً عاماً آخر.";
                    return false;
                }

                partyName = NormalizeSmallText(line.PartyName);
            }

            lines.Add(new ValidatedVoucherLine(
                account.Id,
                account.DisplayText,
                equivalentAmount,
                partyId,
                partyName,
                NormalizeSmallText(line.Description)));
        }

        if (lines.Count == 0)
        {
            error = "أضف سطرًا واحدًا على الأقل داخل السند.";
            return false;
        }

        return true;
    }

    private IReadOnlyList<CreateJournalEntryLineCommand> BuildCommandLines(Guid cashAccountId, IReadOnlyList<ValidatedVoucherLine> lines)
    {
        var commands = new List<CreateJournalEntryLineCommand>(lines.Count + 1);
        var totalEquivalent = lines.Sum(x => x.EquivalentAmount);

        if (_type == JournalEntryType.ReceiptVoucher)
        {
            commands.Add(new CreateJournalEntryLineCommand(cashAccountId, totalEquivalent, 0m, "الحساب النقدي"));
            commands.AddRange(lines.Select(line =>
                new CreateJournalEntryLineCommand(line.AccountId, 0m, line.EquivalentAmount, line.Description, line.PartyName, line.PartyId)));
        }
        else
        {
            commands.AddRange(lines.Select(line =>
                new CreateJournalEntryLineCommand(line.AccountId, line.EquivalentAmount, 0m, line.Description, line.PartyName, line.PartyId)));
            commands.Add(new CreateJournalEntryLineCommand(cashAccountId, 0m, totalEquivalent, "الحساب النقدي"));
        }

        return commands;
    }

    private void RememberQuickDefaults(Guid? firstCounterpartyId)
    {
        _quickDefaultsStore?.Save(
            _type,
            new JournalQuickDefaults(
                SelectedCashAccountId,
                firstCounterpartyId,
                null,
                null,
                SelectedCurrency?.CurrencyCode));
    }

    private void QueuePreviewRefresh()
    {
        RebuildPostingPreview();
        foreach (var line in Lines)
            _ = RefreshLineOpenItemsAsync(line);
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

            if (!TryValidateVoucher(out var cashAccount, out _, out var lines, out _))
            {
                BalancePreviewCards.Clear();
                NotifyPreviewCollectionState();
                return;
            }

            var accountIds = new[] { cashAccount.Id }
                .Concat(lines.Select(x => x.AccountId))
                .Distinct()
                .ToList();

            var balances = await _query.GetPostedAccountBalancesAsync(
                _companyId,
                accountIds,
                DateOnly.FromDateTime(EntryDate.Date));

            if (previewVersion != _previewRequestVersion)
                return;

            RebuildBalancePreview(cashAccount, lines, balances);
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

        if (!TryValidateVoucher(out var cashAccount, out _, out var lines, out _))
        {
            PreviewNoteText = _type == JournalEntryType.ReceiptVoucher
                ? "أدخل بيانات القبض وأضف السطور المقابلة لعرض القيد الناتج."
                : "أدخل بيانات الصرف وأضف السطور المقابلة لعرض القيد الناتج.";
            NotifyPreviewCollectionState();
            return;
        }

        var totalEquivalent = lines.Sum(x => x.EquivalentAmount);
        if (_type == JournalEntryType.ReceiptVoucher)
        {
            PostingPreviewLines.Add(CreatePostingLine("مدين", cashAccount.DisplayText, totalEquivalent));
            foreach (var line in lines)
                PostingPreviewLines.Add(CreatePostingLine("دائن", line.AccountText, line.EquivalentAmount));
        }
        else
        {
            foreach (var line in lines)
                PostingPreviewLines.Add(CreatePostingLine("مدين", line.AccountText, line.EquivalentAmount));
            PostingPreviewLines.Add(CreatePostingLine("دائن", cashAccount.DisplayText, totalEquivalent));
        }

        PreviewNoteText = $"القيد الناتج يحتوي {lines.Count} {(lines.Count == 1 ? "سطر مقابل" : "سطور مقابلة")} وسطرًا نقديًا واحدًا.";
        NotifyPreviewCollectionState();
    }

    private void RebuildBalancePreview(
        JournalAccountOptionVm cashAccount,
        IReadOnlyList<ValidatedVoucherLine> lines,
        IReadOnlyList<JournalAccountBalanceDto> balances)
    {
        BalancePreviewCards.Clear();

        var byId = balances.ToDictionary(x => x.AccountId);
        var cashBalance = byId.TryGetValue(cashAccount.Id, out var cashValue)
            ? cashValue
            : new JournalAccountBalanceDto(cashAccount.Id, cashAccount.Code, cashAccount.NameAr, cashAccount.Nature, 0m, 0m);

        var totalEquivalent = lines.Sum(x => x.EquivalentAmount);
        BalancePreviewCards.Add(_type == JournalEntryType.ReceiptVoucher
            ? CreateBalanceCard("الحساب النقدي", cashAccount.DisplayText, cashBalance, totalEquivalent, 0m)
            : CreateBalanceCard("الحساب النقدي", cashAccount.DisplayText, cashBalance, 0m, totalEquivalent));

        foreach (var line in lines)
        {
            var lineBalance = byId.TryGetValue(line.AccountId, out var accountValue)
                ? accountValue
                : new JournalAccountBalanceDto(line.AccountId, string.Empty, line.AccountText, AccountNature.Credit, 0m, 0m);

            BalancePreviewCards.Add(_type == JournalEntryType.ReceiptVoucher
                ? CreateBalanceCard("حساب مقابل", line.AccountText, lineBalance, 0m, line.EquivalentAmount)
                : CreateBalanceCard("حساب مقابل", line.AccountText, lineBalance, line.EquivalentAmount, 0m));
        }

        NotifyPreviewCollectionState();
    }

    private JournalAccountOptionVm? GetSelectedCashAccount()
        => AccountOptions.FirstOrDefault(x => x.Id == SelectedCashAccountId);

    private Guid? ResolveDefaultCounterpartyAccount()
        => JournalAccountDefaultsResolver.ResolvePreferredCounterpartyAccount(GeneralCounterpartyAccountOptions, _type);

    private static decimal GetLineSideAmount(JournalEntryDetailLineDto line, bool debitSide)
        => debitSide ? line.Debit : line.Credit;

    private JournalAccountOptionVm? GetAccount(Guid accountId)
        => AccountOptions.FirstOrDefault(x => x.Id == accountId);

    private string BuildAutomaticPartyAccountText(Guid accountId)
        => GetAccount(accountId) is { } account
            ? $"الحساب المرتبط: {account.DisplayText}"
            : "الحساب المرتبط محفوظ، لكنه غير متاح الآن في قائمة الحسابات.";

    private IReadOnlyList<VoucherCounterpartyLineEditorVm> GetActiveLines()
        => Lines.Where(x => !x.IsEmpty && x.Amount > 0).ToList();

    private string NormalizeHeaderDescription()
        => string.IsNullOrWhiteSpace(Description)
            ? (_type == JournalEntryType.ReceiptVoucher ? "سند قبض" : "سند صرف")
            : Description.Trim();

    private static string? NormalizeSmallText(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

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
        OnPropertyChanged(nameof(CanEditExchangeRate));
        OnPropertyChanged(nameof(IsEditing));
        OnPropertyChanged(nameof(CanManage));
        OnPropertyChanged(nameof(EditControlsVisibility));
        OnPropertyChanged(nameof(ViewManageButtonsVisibility));
        OnPropertyChanged(nameof(DeleteButtonVisibility));
        OnPropertyChanged(nameof(SaveDraftButtonVisibility));
        OnPropertyChanged(nameof(SaveAndPostButtonVisibility));
        OnPropertyChanged(nameof(SaveChangesButtonVisibility));
        OnPropertyChanged(nameof(AddLineButtonVisibility));
    }

    private void NotifyHeaderStateChanged()
    {
        OnPropertyChanged(nameof(Title));
        OnPropertyChanged(nameof(Subtitle));
        OnPropertyChanged(nameof(EntryMetaText));
        OnPropertyChanged(nameof(EditButtonText));
        NotifyModeChanged();
    }

    private void NotifyTotalsChanged()
    {
        OnPropertyChanged(nameof(TotalAmount));
        OnPropertyChanged(nameof(EquivalentAmount));
        OnPropertyChanged(nameof(TotalAmountText));
        OnPropertyChanged(nameof(EquivalentAmountText));
        OnPropertyChanged(nameof(TotalLinesText));
    }

    private void NotifyPreviewCollectionState()
    {
        OnPropertyChanged(nameof(PostingPreviewVisibility));
        OnPropertyChanged(nameof(PostingPreviewPlaceholderVisibility));
        OnPropertyChanged(nameof(BalancePreviewVisibility));
        OnPropertyChanged(nameof(BalancePreviewPlaceholderVisibility));
    }

    private void Lines_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.OldItems is not null)
        {
            foreach (VoucherCounterpartyLineEditorVm line in e.OldItems)
                line.PropertyChanged -= Line_PropertyChanged;
        }

        if (e.NewItems is not null)
        {
            foreach (VoucherCounterpartyLineEditorVm line in e.NewItems)
                line.PropertyChanged += Line_PropertyChanged;
        }

        NotifyTotalsChanged();
        QueuePreviewRefresh();
    }

    private void Line_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (sender is not VoucherCounterpartyLineEditorVm line)
            return;

        if (e.PropertyName == nameof(VoucherCounterpartyLineEditorVm.EntryKind))
            ApplyEntryKind(line);

        if (e.PropertyName == nameof(VoucherCounterpartyLineEditorVm.SelectedPartyId))
            ApplySelectedParty(line);

        if (e.PropertyName == nameof(VoucherCounterpartyLineEditorVm.SelectedAccountId))
            EnsureSelectedPartyMatchesAccount(line);

        var affectsVoucherCalculations = e.PropertyName is nameof(VoucherCounterpartyLineEditorVm.SelectedPartyId)
            or nameof(VoucherCounterpartyLineEditorVm.SelectedAccountId)
            or nameof(VoucherCounterpartyLineEditorVm.EntryKind)
            or nameof(VoucherCounterpartyLineEditorVm.Amount);

        if (!affectsVoucherCalculations)
            return;

        _ = RefreshLineOpenItemsAsync(line);
        NotifyTotalsChanged();
        QueuePreviewRefresh();
    }

    private void ApplySelectedParty(VoucherCounterpartyLineEditorVm line)
    {
        if (!line.IsPartyEntry)
            return;

        if (line.SelectedPartyId is not Guid partyId)
        {
            line.SelectedAccountId = null;
            line.AutomaticPartyAccountText = "سيظهر الحساب المرتبط بالطرف هنا بعد الاختيار.";
            line.OpenItems.Clear();
            line.OpenItemsSummaryText = string.Empty;
            return;
        }

        var party = PartyOptions.FirstOrDefault(x => x.Id == partyId);
        if (party is null)
            return;

        line.PartyName = party.NameAr;
        if (party.ResolveVoucherAccountId(_type, ReceivableControlAccountId, PayableControlAccountId) is Guid linkedAccountId)
        {
            line.SelectedAccountId = linkedAccountId;
            line.AutomaticPartyAccountText = BuildAutomaticPartyAccountText(linkedAccountId);
        }
        else
        {
            line.SelectedAccountId = null;
            line.AutomaticPartyAccountText = "تعذر تحديد الحساب المرتبط بهذا الطرف. راجع بيانات الطرف أولاً.";
        }
    }

    private void EnsureSelectedPartyMatchesAccount(VoucherCounterpartyLineEditorVm line)
    {
        if (!line.IsPartyEntry)
            return;

        if (line.SelectedPartyId is not Guid partyId)
            return;

        var party = PartyOptions.FirstOrDefault(x => x.Id == partyId);
        if (party is null)
            return;

        if (!party.MatchesAccount(line.SelectedAccountId, ReceivableControlAccountId, PayableControlAccountId))
        {
            if (party.ResolveVoucherAccountId(_type, ReceivableControlAccountId, PayableControlAccountId) is Guid expectedAccountId)
            {
                line.SelectedAccountId = expectedAccountId;
            }
            else
            {
                line.SelectedPartyId = null;
                line.SelectedAccountId = null;
                line.OpenItems.Clear();
                line.OpenItemsSummaryText = string.Empty;
            }
        }

        if (line.SelectedAccountId is Guid accountId)
            line.AutomaticPartyAccountText = BuildAutomaticPartyAccountText(accountId);
    }

    private void ApplyEntryKind(VoucherCounterpartyLineEditorVm line)
    {
        if (line.IsPartyEntry)
        {
            if (line.SelectedPartyId is Guid)
            {
                ApplySelectedParty(line);
            }
            else if (GetAccount(line.SelectedAccountId ?? Guid.Empty)?.IsLegacyPartyLinkedAccount != true)
            {
                line.SelectedAccountId = null;
            }

            line.AutomaticPartyAccountText = line.SelectedAccountId is Guid selectedAccountId
                ? BuildAutomaticPartyAccountText(selectedAccountId)
                : "سيظهر الحساب المرتبط بالطرف هنا بعد الاختيار.";

            return;
        }

        line.SelectedPartyId = null;
        line.PartyName = string.Empty;
        line.AutomaticPartyAccountText = "يمكنك اختيار حساب عام فقط في هذا الوضع.";
        line.OpenItems.Clear();
        line.OpenItemsSummaryText = string.Empty;

        if (GetAccount(line.SelectedAccountId ?? Guid.Empty)?.IsPartyControlAccount == true
            || GetAccount(line.SelectedAccountId ?? Guid.Empty)?.IsLegacyPartyLinkedAccount == true)
            line.SelectedAccountId = ResolveDefaultCounterpartyAccount();
    }

    private async Task RefreshLineOpenItemsAsync(VoucherCounterpartyLineEditorVm line)
    {
        line.OpenItems.Clear();
        line.OpenItemsSummaryText = string.Empty;

        if (line.SelectedPartyId is not Guid partyId || line.SelectedAccountId is not Guid accountId)
            return;

        var statement = await _partyQuery.GetStatementAsync(
            _companyId,
            partyId,
            new DateOnly(Math.Max(2000, EntryDate.Year - 5), 1, 1),
            DateOnly.FromDateTime(EntryDate.Date));

        if (statement is null)
            return;

        var relevantItems = statement.OpenItems
            .Where(x => x.AccountId == accountId)
            .Where(x => _type == JournalEntryType.ReceiptVoucher ? x.Debit > 0 : x.Credit > 0)
            .OrderBy(x => x.EntryDate)
            .ThenBy(x => x.EntryNumber)
            .ToList();

        if (relevantItems.Count == 0)
        {
            line.OpenItemsSummaryText = "لا توجد بنود مفتوحة لهذا الطرف على الحساب المختار.";
            return;
        }

        var remaining = decimal.Round(
            Convert.ToDecimal(Math.Max(0, line.Amount)) * Convert.ToDecimal(Math.Max(0.00000001, ExchangeRate)),
            2,
            MidpointRounding.AwayFromZero);
        var suggestedTotal = 0m;

        foreach (var item in relevantItems)
        {
            var suggested = remaining > 0 ? Math.Min(remaining, item.OpenAmount) : 0m;
            remaining -= suggested;
            suggestedTotal += suggested;

            line.OpenItems.Add(new PartyOpenItemPreviewVm(
                item.EntryNumber,
                item.EntryDate,
                item.AccountNameAr,
                item.OpenAmount,
                suggested,
                item.Description));
        }

        line.OpenItemsSummaryText = $"البنود المفتوحة: {relevantItems.Count} • المقترح تسويته: {suggestedTotal:N2}";
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

    private static int ResolveCashRank(JournalAccountOptionVm account)
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

    private sealed record ValidatedVoucherLine(
        Guid AccountId,
        string AccountText,
        decimal EquivalentAmount,
        Guid? PartyId,
        string? PartyName,
        string? Description);
}
