using Microsoft.Extensions.DependencyInjection;
using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Promix.Financials.Application.Abstractions;
using Promix.Financials.Application.Features.Journals.Queries;
using Promix.Financials.Application.Features.Journals.Services;
using Promix.Financials.UI.Dialogs.Journals;
using Promix.Financials.UI.ViewModels.Journals.Models;
using Promix.Financials.UI.ViewModels.Reports;
using Promix.Financials.UI.ViewModels.Reports.Models;

namespace Promix.Financials.UI.Views;

public sealed partial class ReportsPage : Page
{
    private readonly IServiceScope _scope;
    private readonly AccountStatementViewModel _vm;
    private readonly IUserContext _userContext;
    private readonly IJournalEntriesQuery _query;
    private readonly CreateJournalEntryService _createService;

    public ReportsPage()
    {
        var app = (App)Microsoft.UI.Xaml.Application.Current;
        _scope = app.Services.CreateScope();
        _vm = _scope.ServiceProvider.GetRequiredService<AccountStatementViewModel>();
        _userContext = _scope.ServiceProvider.GetRequiredService<IUserContext>();
        _query = _scope.ServiceProvider.GetRequiredService<IJournalEntriesQuery>();
        _createService = _scope.ServiceProvider.GetRequiredService<CreateJournalEntryService>();

        InitializeComponent();
        DataContext = _vm;

        Loaded += OnLoaded;
        Unloaded += (_, _) => _scope.Dispose();
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (_userContext.CompanyId is null)
            return;

        await _vm.InitializeAsync(_userContext.CompanyId.Value);
    }

    private async void LoadStatement_Click(object sender, RoutedEventArgs e)
    {
        await _vm.LoadAllAsync();
    }

    private async void TodayRange_Click(object sender, RoutedEventArgs e)
    {
        _vm.SetTodayRange();
        await _vm.LoadAllAsync();
    }

    private async void MonthRange_Click(object sender, RoutedEventArgs e)
    {
        _vm.SetThisMonthRange();
        await _vm.LoadAllAsync();
    }

    private async void YearRange_Click(object sender, RoutedEventArgs e)
    {
        _vm.SetThisYearRange();
        await _vm.LoadAllAsync();
    }

    private async void LoadTrialBalance_Click(object sender, RoutedEventArgs e)
    {
        await _vm.LoadTrialBalanceAsync();
    }

    private async void TrialBalanceYearCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if ((sender as ComboBox)?.SelectedItem is int year)
        {
            _vm.SelectedFiscalYear = year;
            await _vm.LoadAllAsync();
        }
    }

    private async void TrialBalanceViewModeCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if ((sender as ComboBox)?.SelectedItem is ComboBoxItem item)
        {
            _vm.TrialBalanceViewModeKey = item.Tag?.ToString() ?? "selected";
            await _vm.LoadTrialBalanceAsync();
        }
    }

    private async void IncludeZeroRowsCheckBox_Click(object sender, RoutedEventArgs e)
    {
        await _vm.LoadTrialBalanceAsync();
    }

    private async void OpenStatementFromTrialBalance_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not FrameworkElement element || element.DataContext is not TrialBalanceRowVm row)
            return;

        await _vm.OpenAccountStatementFromTrialBalanceAsync(row.AccountId);
    }

    private async void CreateReceiptVoucher_Click(object sender, RoutedEventArgs e)
    {
        await LaunchVoucherAsync(isReceipt: true);
    }

    private async void CreatePaymentVoucher_Click(object sender, RoutedEventArgs e)
    {
        await LaunchVoucherAsync(isReceipt: false);
    }

    private async Task LaunchVoucherAsync(bool isReceipt)
    {
        if (_userContext.CompanyId is not Guid companyId || _vm.SelectedAccountId is not Guid accountId)
            return;

        var currencies = await _query.GetActiveCurrenciesAsync(companyId);
        var currencyOptions = currencies
            .Select(currency => new JournalCurrencyOptionVm(
                currency.CurrencyCode,
                currency.NameAr,
                currency.NameEn,
                currency.Symbol,
                currency.DecimalPlaces,
                currency.ExchangeRate,
                currency.IsBaseCurrency))
            .ToList();

        ContentDialog dialog = isReceipt
            ? new ReceiptVoucherDialog(companyId, _vm.AccountOptions.ToList(), currencyOptions, _query, accountId)
            : new PaymentVoucherDialog(companyId, _vm.AccountOptions.ToList(), currencyOptions, _query, accountId);

        dialog.XamlRoot = XamlRoot;
        await ShowVoucherDialogAsync(dialog);
    }

    private async Task ShowVoucherDialogAsync(ContentDialog dialog)
    {
        try
        {
            await dialog.ShowAsync();

            var command = dialog switch
            {
                ReceiptVoucherDialog receiptDialog => receiptDialog.ResultCommand,
                PaymentVoucherDialog paymentDialog => paymentDialog.ResultCommand,
                _ => null
            };

            if (command is null)
                return;

            await _createService.CreateAsync(command);
            await _vm.LoadAllAsync();
        }
        catch (Exception ex)
        {
            var errorDialog = new ContentDialog
            {
                XamlRoot = XamlRoot,
                Title = "تعذر حفظ السند",
                Content = ex.Message,
                CloseButtonText = "إغلاق",
                DefaultButton = ContentDialogButton.Close
            };

            await errorDialog.ShowAsync();
        }
    }
}
