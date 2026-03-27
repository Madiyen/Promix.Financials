using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Promix.Financials.Application.Abstractions;
using Promix.Financials.Application.Features.Journals.Queries;
using Promix.Financials.UI.Dialogs.Journals;
using Promix.Financials.UI.ViewModels.Journals;

namespace Promix.Financials.UI.Views.Journals;

public sealed partial class JournalEntriesPage : Page
{
    private readonly IServiceScope _scope;
    private readonly JournalEntriesViewModel _vm;
    private readonly IUserContext _userContext;
    private readonly IJournalEntriesQuery _query;
    private bool _isUiReady;
    private bool _isApplyingQuickView;

    public JournalEntriesPage()
    {
        var app = (App)Microsoft.UI.Xaml.Application.Current;
        _scope = app.Services.CreateScope();
        _vm = _scope.ServiceProvider.GetRequiredService<JournalEntriesViewModel>();
        _userContext = _scope.ServiceProvider.GetRequiredService<IUserContext>();
        _query = _scope.ServiceProvider.GetRequiredService<IJournalEntriesQuery>();

        InitializeComponent();

        DataContext = _vm;

        _vm.PropertyChanged += (_, args) =>
        {
            if (args.PropertyName is nameof(_vm.ErrorMessage) or null)
            {
                ErrorBannerText.Text = _vm.ErrorMessage ?? string.Empty;
                ErrorBanner.Visibility = _vm.HasError ? Visibility.Visible : Visibility.Collapsed;
            }

            if (args.PropertyName is nameof(_vm.SuccessMessage) or null)
            {
                SuccessBannerText.Text = _vm.SuccessMessage ?? string.Empty;
                SuccessBanner.Visibility = _vm.HasSuccess ? Visibility.Visible : Visibility.Collapsed;
            }
        };

        Loaded += OnLoaded;
        Unloaded += (_, __) => _scope.Dispose();
        _isUiReady = true;
        SyncQuickViewButtons();
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (_userContext.CompanyId is null)
            return;

        await _vm.InitializeAsync(_userContext.CompanyId.Value);
    }

    private async void Refresh_Click(object sender, RoutedEventArgs e)
    {
        await _vm.RefreshAsync();
    }

    private async void CreateReceipt_Click(object sender, RoutedEventArgs e)
    {
        if (_userContext.CompanyId is null)
            return;

        var dialog = new ReceiptVoucherDialog(
            _userContext.CompanyId.Value,
            _vm.AccountOptions.ToList(),
            _vm.CurrencyOptions.ToList(),
            _query)
        {
            XamlRoot = XamlRoot
        };
        await ShowJournalDialogAsync(dialog);
    }

    private async void CreatePayment_Click(object sender, RoutedEventArgs e)
    {
        if (_userContext.CompanyId is null)
            return;

        var dialog = new PaymentVoucherDialog(
            _userContext.CompanyId.Value,
            _vm.AccountOptions.ToList(),
            _vm.CurrencyOptions.ToList(),
            _query)
        {
            XamlRoot = XamlRoot
        };
        await ShowJournalDialogAsync(dialog);
    }

    private async void CreateTransfer_Click(object sender, RoutedEventArgs e)
    {
        if (_userContext.CompanyId is null)
            return;

        var dialog = new TransferVoucherDialog(
            _userContext.CompanyId.Value,
            _vm.AccountOptions.ToList(),
            _vm.CurrencyOptions.ToList(),
            _query)
        {
            XamlRoot = XamlRoot
        };
        await ShowJournalDialogAsync(dialog);
    }

    private async void CreateDailyJournal_Click(object sender, RoutedEventArgs e)
    {
        if (_userContext.CompanyId is null)
            return;

        var dialog = new DailyJournalDialog(_userContext.CompanyId.Value, _vm.AccountOptions.ToList()) { XamlRoot = XamlRoot };
        await ShowJournalDialogAsync(dialog);
    }

    private async void CreateOpeningEntry_Click(object sender, RoutedEventArgs e)
    {
        if (_userContext.CompanyId is null)
            return;

        var dialog = new OpeningEntryDialog(_userContext.CompanyId.Value, _vm.AccountOptions.ToList()) { XamlRoot = XamlRoot };
        await ShowJournalDialogAsync(dialog);
    }

    private async void CreateDailyCashClosing_Click(object sender, RoutedEventArgs e)
    {
        if (_userContext.CompanyId is null)
            return;

        var dialog = new DailyCashClosingDialog(_userContext.CompanyId.Value, _vm.AccountOptions.ToList()) { XamlRoot = XamlRoot };
        await dialog.ShowAsync();

        if (dialog.ResultCommand is null)
            return;

        await _vm.CreateCashClosingAsync(dialog.ResultCommand);
    }

    private async void PostSelected_Click(object sender, RoutedEventArgs e)
    {
        if (_vm.SelectedEntry is null)
            return;

        var confirm = new ContentDialog
        {
            Title = "ترحيل السند",
            Content = $"هل تريد ترحيل السند {_vm.SelectedEntry.EntryNumber} الآن؟",
            PrimaryButtonText = "ترحيل",
            CloseButtonText = "إلغاء",
            DefaultButton = ContentDialogButton.Close,
            XamlRoot = XamlRoot
        };

        if (await confirm.ShowAsync() != ContentDialogResult.Primary)
            return;

        await _vm.PostSelectedAsync();
    }

    private async Task ShowJournalDialogAsync(ContentDialog dialog)
    {
        await dialog.ShowAsync();

        if (dialog switch
            {
                ReceiptVoucherDialog receipt when receipt.ResultCommand is not null => receipt.ResultCommand,
                PaymentVoucherDialog payment when payment.ResultCommand is not null => payment.ResultCommand,
                TransferVoucherDialog transfer when transfer.ResultCommand is not null => transfer.ResultCommand,
                DailyJournalDialog dailyJournal when dailyJournal.ResultCommand is not null => dailyJournal.ResultCommand,
                OpeningEntryDialog opening when opening.ResultCommand is not null => opening.ResultCommand,
                _ => null
            } is not { } command)
            return;

        await _vm.CreateAsync(command);
    }

    private void SearchFilterBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        _vm.SearchText = (sender as TextBox)?.Text ?? string.Empty;
        SyncQuickViewButtons();
    }

    private void TypeFilterCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        _vm.SetTypeFilter(GetSelectedTag(sender));
        SyncQuickViewButtons();
    }

    private void StatusFilterCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        _vm.SetStatusFilter(GetSelectedTag(sender));
        SyncQuickViewButtons();
    }

    private void PeriodFilterCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        _vm.SetPeriodFilter(GetSelectedTag(sender));
        SyncQuickViewButtons();
    }

    private void QuickViewButton_Click(object sender, RoutedEventArgs e)
    {
        if (!_isUiReady || _isApplyingQuickView)
            return;

        _isApplyingQuickView = true;
        try
        {
            switch ((sender as Button)?.Tag?.ToString())
            {
                case "today":
                    TypeFilterCombo.SelectedIndex = 0;
                    StatusFilterCombo.SelectedIndex = 0;
                    PeriodFilterCombo.SelectedIndex = 1;
                    break;
                case "drafts":
                    TypeFilterCombo.SelectedIndex = 0;
                    StatusFilterCombo.SelectedIndex = 2;
                    PeriodFilterCombo.SelectedIndex = 0;
                    break;
                case "posted":
                    TypeFilterCombo.SelectedIndex = 0;
                    StatusFilterCombo.SelectedIndex = 1;
                    PeriodFilterCombo.SelectedIndex = 0;
                    break;
                case "month":
                    TypeFilterCombo.SelectedIndex = 0;
                    StatusFilterCombo.SelectedIndex = 0;
                    PeriodFilterCombo.SelectedIndex = 3;
                    break;
                default:
                    TypeFilterCombo.SelectedIndex = 0;
                    StatusFilterCombo.SelectedIndex = 0;
                    PeriodFilterCombo.SelectedIndex = 0;
                    break;
            }
        }
        finally
        {
            _isApplyingQuickView = false;
            SyncQuickViewButtons();
        }
    }

    private void ClearFilters_Click(object sender, RoutedEventArgs e)
    {
        SearchFilterBox.Text = string.Empty;
        TypeFilterCombo.SelectedIndex = 0;
        StatusFilterCombo.SelectedIndex = 0;
        PeriodFilterCombo.SelectedIndex = 0;
        _vm.ClearFilters();
        SyncQuickViewButtons();
    }

    private static string? GetSelectedTag(object sender)
    {
        if ((sender as ComboBox)?.SelectedItem is ComboBoxItem comboItem)
            return comboItem.Tag?.ToString();

        return null;
    }

    private void SyncQuickViewButtons()
    {
        if (!_isUiReady || _isApplyingQuickView)
            return;

        if (!string.IsNullOrWhiteSpace(SearchFilterBox?.Text))
        {
            SetQuickViewState(null);
            return;
        }

        var type = GetSelectedTag(TypeFilterCombo) ?? "all";
        var status = GetSelectedTag(StatusFilterCombo) ?? "all";
        var period = GetSelectedTag(PeriodFilterCombo) ?? "all";

        var quickViewKey =
            type == "all" && status == "all" && period == "all" ? "all" :
            type == "all" && status == "all" && period == "today" ? "today" :
            type == "all" && status == "draft" && period == "all" ? "drafts" :
            type == "all" && status == "posted" && period == "all" ? "posted" :
            type == "all" && status == "all" && period == "month" ? "month" :
            null;

        SetQuickViewState(quickViewKey);
    }

    private void SetQuickViewState(string? quickViewKey)
    {
        SetQuickViewButtonStyle(QuickViewAllButton, quickViewKey == "all");
        SetQuickViewButtonStyle(QuickViewTodayButton, quickViewKey == "today");
        SetQuickViewButtonStyle(QuickViewDraftsButton, quickViewKey == "drafts");
        SetQuickViewButtonStyle(QuickViewPostedButton, quickViewKey == "posted");
        SetQuickViewButtonStyle(QuickViewMonthButton, quickViewKey == "month");
    }

    private void SetQuickViewButtonStyle(Button? button, bool isActive)
    {
        if (button is null)
            return;

        button.Style = (Style)Resources[isActive ? "QuickViewButtonActiveStyle" : "QuickViewButtonStyle"];
    }
}
