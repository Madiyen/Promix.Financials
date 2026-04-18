using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Promix.Financials.Application.Abstractions;
using Promix.Financials.Application.Features.Journals.Commands;
using Promix.Financials.Application.Features.Journals.Queries;
using Promix.Financials.Application.Features.Parties.Queries;
using Promix.Financials.Domain.Enums;
using Promix.Financials.UI.Dialogs.Journals;
using Promix.Financials.UI.ViewModels.Journals;
using Promix.Financials.UI.ViewModels.Parties.Models;
using Windows.System;

namespace Promix.Financials.UI.Views.Journals;

public sealed partial class JournalEntriesPage : Page
{
    private readonly IServiceScope _scope;
    private readonly JournalEntriesViewModel _vm;
    private readonly IUserContext _userContext;
    private readonly IJournalEntriesQuery _query;
    private readonly IPartyQuery _partyQuery;

    public JournalEntriesPage()
    {
        var app = (App)Microsoft.UI.Xaml.Application.Current;
        _scope = app.Services.CreateScope();
        _vm = _scope.ServiceProvider.GetRequiredService<JournalEntriesViewModel>();
        _userContext = _scope.ServiceProvider.GetRequiredService<IUserContext>();
        _query = _scope.ServiceProvider.GetRequiredService<IJournalEntriesQuery>();
        _partyQuery = _scope.ServiceProvider.GetRequiredService<IPartyQuery>();

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
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (_userContext.CompanyId is null)
            return;

        await _vm.InitializeAsync(_userContext.CompanyId.Value);
        await _vm.EnsureEntryDetailsLoadedAsync(_vm.SelectedEntry);
    }

    private async void Refresh_Click(object sender, RoutedEventArgs e)
    {
        await _vm.RefreshAsync();
        await _vm.EnsureEntryDetailsLoadedAsync(_vm.SelectedEntry);
    }

    private async void CreateReceipt_Click(object sender, RoutedEventArgs e)
    {
        if (_userContext.CompanyId is null)
            return;

        var parties = await LoadActivePartyOptionsAsync(_userContext.CompanyId.Value);
        var dialog = new ReceiptVoucherDialog(
            _userContext.CompanyId.Value,
            _vm.AccountOptions.ToList(),
            _vm.CurrencyOptions.ToList(),
            parties,
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

        var parties = await LoadActivePartyOptionsAsync(_userContext.CompanyId.Value);
        var dialog = new PaymentVoucherDialog(
            _userContext.CompanyId.Value,
            _vm.AccountOptions.ToList(),
            _vm.CurrencyOptions.ToList(),
            parties,
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

        var parties = await LoadActivePartyOptionsAsync(_userContext.CompanyId.Value);
        var dialog = new TransferVoucherDialog(
            _userContext.CompanyId.Value,
            _vm.AccountOptions.ToList(),
            _vm.CurrencyOptions.ToList(),
            parties,
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

        var parties = await LoadActivePartyOptionsAsync(_userContext.CompanyId.Value);
        var dialog = new DailyJournalDialog(
            _userContext.CompanyId.Value,
            _vm.AccountOptions.ToList(),
            _vm.CurrencyOptions.ToList(),
            parties)
        {
            XamlRoot = XamlRoot
        };
        await ShowJournalDialogAsync(dialog);
    }

    private async void CreateOpeningEntry_Click(object sender, RoutedEventArgs e)
    {
        if (_userContext.CompanyId is null)
            return;

        var parties = await LoadActivePartyOptionsAsync(_userContext.CompanyId.Value);
        var dialog = new OpeningEntryDialog(_userContext.CompanyId.Value, _vm.AccountOptions.ToList(), parties) { XamlRoot = XamlRoot };
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

        switch (dialog)
        {
            case ReceiptVoucherDialog receipt:
                await HandleVoucherDialogResultAsync(
                    receipt.ResultCommand,
                    receipt.UpdateCommand,
                    receipt.DeleteCommand,
                    receipt.ViewModel.Title);
                return;
            case PaymentVoucherDialog payment:
                await HandleVoucherDialogResultAsync(
                    payment.ResultCommand,
                    payment.UpdateCommand,
                    payment.DeleteCommand,
                    payment.ViewModel.Title);
                return;
            case TransferVoucherDialog transfer:
                await HandleVoucherDialogResultAsync(
                    transfer.ResultCommand,
                    transfer.UpdateCommand,
                    transfer.DeleteCommand,
                    transfer.ViewModel.Title);
                return;
            case DailyJournalDialog dailyJournal when dailyJournal.ResultCommand is not null:
                await _vm.CreateAsync(dailyJournal.ResultCommand);
                return;
            case OpeningEntryDialog opening when opening.ResultCommand is not null:
                await _vm.CreateAsync(opening.ResultCommand);
                return;
        }
    }

    private async Task HandleVoucherDialogResultAsync(
        CreateJournalEntryCommand? createCommand,
        UpdateJournalEntryCommand? updateCommand,
        DeleteJournalEntryCommand? deleteCommand,
        string titleText)
    {
        if (deleteCommand is not null)
        {
            var confirmDelete = new ContentDialog
            {
                Title = "حذف السند",
                Content = $"هل تريد حذف {titleText} من القوائم التشغيلية؟",
                PrimaryButtonText = "حذف",
                CloseButtonText = "إلغاء",
                DefaultButton = ContentDialogButton.Close,
                XamlRoot = XamlRoot
            };

            if (await confirmDelete.ShowAsync() == ContentDialogResult.Primary)
                await _vm.DeleteAsync(deleteCommand);

            return;
        }

        if (updateCommand is not null)
        {
            await _vm.UpdateAsync(updateCommand);
            return;
        }

        if (createCommand is not null)
            await _vm.CreateAsync(createCommand);
    }

    private async void EntriesListView_DoubleTapped(object sender, DoubleTappedRoutedEventArgs e)
    {
        await OpenSelectedVoucherAsync();
    }

    private async void EntriesListView_KeyDown(object sender, KeyRoutedEventArgs e)
    {
        if (e.Key != VirtualKey.Enter)
            return;

        e.Handled = true;
        await OpenSelectedVoucherAsync();
    }

    private async void EntriesDataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        await _vm.EnsureEntryDetailsLoadedAsync(_vm.SelectedEntry);
    }

    private async Task OpenSelectedVoucherAsync()
    {
        if (_userContext.CompanyId is not Guid companyId || _vm.SelectedEntry is not { } entry)
            return;

        if (!IsSupportedVoucherType(entry.Type))
            return;

        var detail = await _query.GetEntryDetailAsync(companyId, entry.Id);
        if (detail is null)
        {
            var notFoundDialog = new ContentDialog
            {
                Title = "تعذر فتح السند",
                Content = "لم أتمكن من تحميل تفاصيل هذا السند. جرّب تحديث القائمة ثم أعد المحاولة.",
                CloseButtonText = "حسنًا",
                DefaultButton = ContentDialogButton.Close,
                XamlRoot = XamlRoot
            };
            await notFoundDialog.ShowAsync();
            return;
        }

        ContentDialog dialog = (JournalEntryType)detail.Type switch
        {
            JournalEntryType.ReceiptVoucher => new ReceiptVoucherDialog(
                companyId,
                _vm.AccountOptions.ToList(),
                _vm.CurrencyOptions.ToList(),
                await LoadActivePartyOptionsAsync(companyId),
                _query,
                detail,
                _userContext.IsAdmin),
            JournalEntryType.PaymentVoucher => new PaymentVoucherDialog(
                companyId,
                _vm.AccountOptions.ToList(),
                _vm.CurrencyOptions.ToList(),
                await LoadActivePartyOptionsAsync(companyId),
                _query,
                detail,
                _userContext.IsAdmin),
            JournalEntryType.TransferVoucher => new TransferVoucherDialog(
                companyId,
                _vm.AccountOptions.ToList(),
                _vm.CurrencyOptions.ToList(),
                await LoadActivePartyOptionsAsync(companyId),
                _query,
                detail,
                _userContext.IsAdmin),
            _ => throw new InvalidOperationException("Unsupported voucher type.")
        };

        dialog.XamlRoot = XamlRoot;
        await ShowJournalDialogAsync(dialog);
    }

    private static bool IsSupportedVoucherType(JournalEntryType type)
        => type is JournalEntryType.ReceiptVoucher
            or JournalEntryType.PaymentVoucher
            or JournalEntryType.TransferVoucher;

    private async Task<IReadOnlyList<PartyOptionVm>> LoadActivePartyOptionsAsync(Guid companyId)
    {
        var parties = await _partyQuery.GetActivePartiesAsync(companyId);
        return parties
            .Select(x => new PartyOptionVm(x.Id, x.Code, x.NameAr, x.TypeFlags, x.LedgerMode, x.ReceivableAccountId, x.PayableAccountId, x.IsActive))
            .ToList();
    }

    private void SearchFilterBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        _vm.SearchText = (sender as TextBox)?.Text ?? string.Empty;
    }

    private void TypeFilterCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        _vm.SetTypeFilter(GetSelectedTag(sender));
    }

    private void StatusFilterCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        _vm.SetStatusFilter(GetSelectedTag(sender));
    }

    private void PeriodFilterCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        _vm.SetPeriodFilter(GetSelectedTag(sender));
    }

    private void ClearFilters_Click(object sender, RoutedEventArgs e)
    {
        SearchFilterBox.Text = string.Empty;
        TypeFilterCombo.SelectedIndex = 0;
        StatusFilterCombo.SelectedIndex = 0;
        PeriodFilterCombo.SelectedIndex = 0;
        _vm.ClearFilters();
    }

    private static string? GetSelectedTag(object sender)
    {
        if ((sender as ComboBox)?.SelectedItem is ComboBoxItem comboItem)
            return comboItem.Tag?.ToString();

        return null;
    }
}
