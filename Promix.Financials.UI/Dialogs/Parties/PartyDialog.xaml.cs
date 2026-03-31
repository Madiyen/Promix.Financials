using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Promix.Financials.Application.Abstractions;
using Promix.Financials.Application.Features.Parties.Commands;
using Promix.Financials.Domain.Enums;
using Promix.Financials.UI.ViewModels.Journals.Models;
using Promix.Financials.UI.ViewModels.Parties.Models;

namespace Promix.Financials.UI.Dialogs.Parties;

public sealed partial class PartyDialog : ContentDialog
{
    private readonly Guid _companyId;
    private readonly PartyRowVm? _existingParty;
    private readonly IServiceScope _scope;
    private readonly IAccountRepository _accountRepository;
    private readonly Dictionary<Guid, JournalAccountOptionVm> _accountOptionsById = [];
    private readonly PartyDialogDraftState? _initialDraft;
    private bool _isViewReady;

    public PartyDialog(
        Guid companyId,
        PartyRowVm? existingParty = null,
        PartyDialogDraftState? draft = null)
    {
        var app = (App)Microsoft.UI.Xaml.Application.Current;
        _scope = app.Services.CreateScope();
        _accountRepository = _scope.ServiceProvider.GetRequiredService<IAccountRepository>();

        InitializeComponent();

        _companyId = companyId;
        _existingParty = existingParty;
        _initialDraft = draft;

        ConfigureDialogTexts();
        ApplyInitialValues();

        _isViewReady = true;
        Loaded += PartyDialog_Loaded;
        Closed += PartyDialog_Closed;
        RefreshSummary();
    }

    public bool IsSubmitted { get; private set; }

    public PartyDialogDraftState CaptureDraftState()
        => new(
            CodeTextBox.Text ?? string.Empty,
            NameArTextBox.Text ?? string.Empty,
            NullIfWhiteSpace(NameEnTextBox.Text),
            ResolveTypeFlags(),
            NullIfWhiteSpace(PhoneTextBox.Text),
            NullIfWhiteSpace(MobileTextBox.Text),
            NullIfWhiteSpace(EmailTextBox.Text),
            NullIfWhiteSpace(TaxNoTextBox.Text),
            NullIfWhiteSpace(AddressTextBox.Text),
            NullIfWhiteSpace(NotesTextBox.Text),
            ActiveToggleSwitch.IsOn);

    public CreatePartyCommand BuildCreateCommand()
        => new(
            _companyId,
            CodeTextBox.Text ?? string.Empty,
            NameArTextBox.Text ?? string.Empty,
            NullIfWhiteSpace(NameEnTextBox.Text),
            ResolveTypeFlags(),
            PartyLedgerMode.LegacyLinkedAccounts,
            NullIfWhiteSpace(PhoneTextBox.Text),
            NullIfWhiteSpace(MobileTextBox.Text),
            NullIfWhiteSpace(EmailTextBox.Text),
            NullIfWhiteSpace(TaxNoTextBox.Text),
            NullIfWhiteSpace(AddressTextBox.Text),
            NullIfWhiteSpace(NotesTextBox.Text),
            null,
            null);

    public EditPartyCommand BuildEditCommand()
    {
        if (_existingParty is null)
            throw new InvalidOperationException("Edit command requires an existing party.");

        return new EditPartyCommand(
            _companyId,
            _existingParty.Id,
            CodeTextBox.Text ?? string.Empty,
            NameArTextBox.Text ?? string.Empty,
            NullIfWhiteSpace(NameEnTextBox.Text),
            ResolveTypeFlags(),
            _existingParty.LedgerMode,
            NullIfWhiteSpace(PhoneTextBox.Text),
            NullIfWhiteSpace(MobileTextBox.Text),
            NullIfWhiteSpace(EmailTextBox.Text),
            NullIfWhiteSpace(TaxNoTextBox.Text),
            NullIfWhiteSpace(AddressTextBox.Text),
            NullIfWhiteSpace(NotesTextBox.Text),
            ActiveToggleSwitch.IsOn,
            _existingParty.LedgerMode == PartyLedgerMode.LegacyLinkedAccounts ? _existingParty.ReceivableAccountId : null,
            _existingParty.LedgerMode == PartyLedgerMode.LegacyLinkedAccounts ? _existingParty.PayableAccountId : null);
    }

    private async void PartyDialog_Loaded(object sender, RoutedEventArgs e)
    {
        Loaded -= PartyDialog_Loaded;
        await LoadLegacyAccountsAsync();
    }

    private void PartyDialog_Closed(ContentDialog sender, ContentDialogClosedEventArgs args)
    {
        Closed -= PartyDialog_Closed;
        _scope.Dispose();
    }

    private void ConfigureDialogTexts()
    {
        if (_existingParty is null)
        {
            DialogBadgeText.Text = "إضافة";
            DialogTitleText.Text = "إضافة طرف";
            DialogSubtitleText.Text = "أنشئ عميلاً أو مورداً جديداً، وسيقوم النظام بإنشاء حساباته المرتبطة تلقائياً داخل شجرة الحسابات.";
            ActivePanel.Visibility = Visibility.Collapsed;
            return;
        }

        DialogBadgeText.Text = "تعديل";
        DialogTitleText.Text = "تعديل الطرف";
        DialogSubtitleText.Text = "راجع البيانات الأساسية ووسائل التواصل والحالة. الحسابات المرتبطة تظهر هنا للمتابعة، وليس للتعديل اليدوي من هذه الشاشة.";
    }

    private void ApplyInitialValues()
    {
        var state = _initialDraft ?? CreateInitialStateFromExistingParty();

        CodeTextBox.Text = state?.Code ?? string.Empty;
        NameArTextBox.Text = state?.NameAr ?? string.Empty;
        NameEnTextBox.Text = state?.NameEn ?? string.Empty;
        PhoneTextBox.Text = state?.Phone ?? string.Empty;
        MobileTextBox.Text = state?.Mobile ?? string.Empty;
        EmailTextBox.Text = state?.Email ?? string.Empty;
        TaxNoTextBox.Text = state?.TaxNo ?? string.Empty;
        AddressTextBox.Text = state?.Address ?? string.Empty;
        NotesTextBox.Text = state?.Notes ?? string.Empty;
        ActiveToggleSwitch.IsOn = state?.IsActive ?? true;
        TypeComboBox.SelectedIndex = (state?.TypeFlags ?? PartyTypeFlags.Customer) switch
        {
            PartyTypeFlags.Customer => 0,
            PartyTypeFlags.Vendor => 1,
            _ => 2
        };
    }

    private PartyDialogDraftState? CreateInitialStateFromExistingParty()
    {
        if (_existingParty is null)
            return null;

        return new PartyDialogDraftState(
            _existingParty.Code,
            _existingParty.NameAr,
            _existingParty.NameEn,
            _existingParty.TypeFlags,
            _existingParty.Phone,
            _existingParty.Mobile,
            _existingParty.Email,
            _existingParty.TaxNo,
            _existingParty.Address,
            _existingParty.Notes,
            _existingParty.IsActive);
    }

    private async Task LoadLegacyAccountsAsync()
    {
        _accountOptionsById.Clear();
        foreach (var accountId in GetCurrentLinkedAccountIds())
        {
            var account = await _accountRepository.GetByIdAsync(accountId, _companyId);
            if (account is null)
                continue;

            _accountOptionsById[account.Id] = new JournalAccountOptionVm(
                account.Id,
                account.Code,
                account.NameAr,
                account.Nature,
                account.SystemRole);
        }

        RefreshSummary();
    }

    private HashSet<Guid> GetCurrentLinkedAccountIds()
    {
        var ids = new HashSet<Guid>();

        if (_existingParty?.ReceivableAccountId is Guid receivableId)
            ids.Add(receivableId);

        if (_existingParty?.PayableAccountId is Guid payableId)
            ids.Add(payableId);

        return ids;
    }

    private void SaveButton_Click(object sender, RoutedEventArgs e)
    {
        IsSubmitted = false;
        ValidationBanner.Visibility = Visibility.Collapsed;
        ValidationText.Text = string.Empty;

        if (string.IsNullOrWhiteSpace(NameArTextBox.Text))
        {
            ValidationText.Text = "الاسم العربي مطلوب قبل حفظ الطرف.";
            ValidationBanner.Visibility = Visibility.Visible;
            return;
        }

        IsSubmitted = true;
        Hide();
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        IsSubmitted = false;
        Hide();
    }

    private void TypeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e) => RefreshSummary();

    private void NameArTextBox_TextChanged(object sender, TextChangedEventArgs e) => RefreshSummary();

    private void CodeTextBox_TextChanged(object sender, TextChangedEventArgs e) => RefreshSummary();

    private void ActiveToggleSwitch_Toggled(object sender, RoutedEventArgs e) => RefreshSummary();

    private void RefreshSummary()
    {
        if (!_isViewReady
            || SummaryNameText is null
            || SummaryCodeText is null
            || SummaryTypeText is null
            || SummaryStatusText is null
            || SummaryBalancesText is null
            || SummaryAccountText is null
            || AccountLinkHintText is null)
        {
            return;
        }

        var typeFlags = ResolveTypeFlags();

        SummaryNameText.Text = string.IsNullOrWhiteSpace(NameArTextBox.Text)
            ? "اسم الطرف سيظهر هنا"
            : NameArTextBox.Text.Trim();

        SummaryCodeText.Text = string.IsNullOrWhiteSpace(CodeTextBox.Text)
            ? "سيُولد الكود تلقائياً"
            : CodeTextBox.Text.Trim();

        SummaryTypeText.Text = typeFlags switch
        {
            PartyTypeFlags.Customer => "النوع: عميل",
            PartyTypeFlags.Vendor => "النوع: مورد",
            _ => "النوع: عميل ومورد"
        };

        SummaryStatusText.Text = _existingParty is null
            ? "الحالة: نشط عند الإنشاء"
            : ActiveToggleSwitch.IsOn ? "الحالة الحالية: نشط" : "الحالة الحالية: موقوف التعامل";

        FooterHintText.Text = _existingParty?.LedgerMode == PartyLedgerMode.LegacyLinkedAccounts
            ? "تبقى الحسابات المرتبطة كما هي، ويستخدمها النظام مباشرة عند اختيار هذا الطرف في السندات والقيود."
            : "هذا الطرف محفوظ على نموذج قديم مختلف، ويُعرض هنا حفاظاً على التوافق فقط.";

        if (_existingParty?.LedgerMode == PartyLedgerMode.LegacyLinkedAccounts)
        {
            SummaryAccountText.Text = $"الربط الحالي: {ResolveAccountText(_existingParty.ReceivableAccountId)} / {ResolveAccountText(_existingParty.PayableAccountId)}";
            SummaryBalancesText.Text = $"مدين مفتوح {_existingParty.ReceivableOpenText} • دائن مفتوح {_existingParty.PayableOpenText}";
            AccountLinkHintText.Text = "تُستخدم هذه الحسابات المرتبطة مباشرة في السندات والقيود الخاصة بالطرف. يظهر الربط هنا للقراءة والمتابعة.";
            LegacyAccountsPanel.Visibility = Visibility.Visible;
            SubledgerInfoPanel.Visibility = Visibility.Collapsed;
            LegacyAccountText.Text = $"ذمم مدينة: {ResolveAccountText(_existingParty.ReceivableAccountId)}\nذمم دائنة: {ResolveAccountText(_existingParty.PayableAccountId)}";
            return;
        }

        LegacyAccountsPanel.Visibility = Visibility.Collapsed;
        SubledgerInfoPanel.Visibility = Visibility.Visible;
        SummaryAccountText.Text = typeFlags switch
        {
            PartyTypeFlags.Customer => "الربط: سيُنشأ حساب عميل تلقائياً تحت الأصل 121",
            PartyTypeFlags.Vendor => "الربط: سيُنشأ حساب مورد تلقائياً تحت الأصل 221",
            _ => "الربط: سيُنشأ حسابان منفصلان تحت الأصلين 121 و221"
        };

        if (_existingParty is null)
        {
            SummaryBalancesText.Text = typeFlags switch
            {
                PartyTypeFlags.Customer => "سينشئ النظام حساب ذمم مدينة خاصاً بهذا العميل داخل شجرة الحسابات.",
                PartyTypeFlags.Vendor => "سينشئ النظام حساب ذمم دائنة خاصاً بهذا المورد داخل شجرة الحسابات.",
                _ => "سينشئ النظام حساب ذمم مدينة وحساب ذمم دائنة منفصلين لهذا الطرف."
            };
        }
        else
        {
            SummaryBalancesText.Text = $"مدين مفتوح {_existingParty.ReceivableOpenText} • دائن مفتوح {_existingParty.PayableOpenText}";
        }

        AccountLinkHintText.Text = typeFlags switch
        {
            PartyTypeFlags.Customer => "عند اختيار هذا العميل في سند قبض أو قيد يومية، سيظهر حسابه المرتبط تلقائياً داخل السطر.",
            PartyTypeFlags.Vendor => "عند اختيار هذا المورد في سند دفع أو قيد يومية، سيظهر حسابه المرتبط تلقائياً داخل السطر.",
            _ => "عند اختيار هذا الطرف، يستخدم النظام حساب العميل أو المورد المناسب تلقائياً بحسب نوع الحركة."
        };
    }

    private string ResolveAccountText(Guid? accountId)
    {
        if (!accountId.HasValue)
            return "غير محدد";

        return _accountOptionsById.TryGetValue(accountId.Value, out var option)
            ? option.DisplayText
            : "حساب محفوظ من سجل قديم";
    }

    private PartyTypeFlags ResolveTypeFlags()
        => TypeComboBox.SelectedIndex switch
        {
            1 => PartyTypeFlags.Vendor,
            2 => PartyTypeFlags.Both,
            _ => PartyTypeFlags.Customer
        };

    private static string? NullIfWhiteSpace(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}

public sealed record PartyDialogDraftState(
    string Code,
    string NameAr,
    string? NameEn,
    PartyTypeFlags TypeFlags,
    string? Phone,
    string? Mobile,
    string? Email,
    string? TaxNo,
    string? Address,
    string? Notes,
    bool IsActive
);
