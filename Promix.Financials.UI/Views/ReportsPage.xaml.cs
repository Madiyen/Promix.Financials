using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using Promix.Financials.Application.Abstractions;
using Promix.Financials.Application.Features.Journals.Queries;
using Promix.Financials.Application.Features.Journals.Services;
using Promix.Financials.Application.Features.Parties.Queries;
using Promix.Financials.UI.Dialogs.Journals;
using Promix.Financials.UI.ViewModels.Journals.Models;
using Promix.Financials.UI.ViewModels.Parties.Models;
using Promix.Financials.UI.ViewModels.Reports;
using Promix.Financials.UI.ViewModels.Reports.Models;

namespace Promix.Financials.UI.Views;

public sealed partial class ReportsPage : Page
{
    private readonly IServiceScope _scope;
    private readonly AccountStatementViewModel _vm;
    private readonly IUserContext _userContext;
    private readonly IJournalEntriesQuery _query;
    private readonly IPartyQuery _partyQuery;
    private readonly CreateJournalEntryService _createService;
    private Guid? _requestedAccountId;
    private bool _isInitializing;
    private bool _isLoadingStatement;

    public ReportsPage()
    {
        var app = (App)Microsoft.UI.Xaml.Application.Current;
        _scope = app.Services.CreateScope();
        _vm = _scope.ServiceProvider.GetRequiredService<AccountStatementViewModel>();
        _userContext = _scope.ServiceProvider.GetRequiredService<IUserContext>();
        _query = _scope.ServiceProvider.GetRequiredService<IJournalEntriesQuery>();
        _partyQuery = _scope.ServiceProvider.GetRequiredService<IPartyQuery>();
        _createService = _scope.ServiceProvider.GetRequiredService<CreateJournalEntryService>();

        InitializeComponent();
        DataContext = _vm;

        Loaded += OnLoaded;
    }

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        _requestedAccountId = e.Parameter is Guid accountId && accountId != Guid.Empty ? accountId : null;
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (_userContext.CompanyId is null)
            return;

        _isInitializing = true;
        try
        {
            await _vm.InitializeAsync(_userContext.CompanyId.Value);

            if (_requestedAccountId is Guid accountId)
            {
                _vm.SelectedAccountId = accountId;
                await _vm.LoadStatementPageAsync();
                _requestedAccountId = null;
            }
        }
        finally
        {
            _isInitializing = false;
        }
    }

    private async void LoadStatement_Click(object sender, RoutedEventArgs e)
    {
        await LoadStatementSafelyAsync();
    }

    private async void TodayRange_Click(object sender, RoutedEventArgs e)
    {
        _vm.SetTodayRange();
        await LoadStatementSafelyAsync();
    }

    private async void MonthRange_Click(object sender, RoutedEventArgs e)
    {
        _vm.SetThisMonthRange();
        await LoadStatementSafelyAsync();
    }

    private async void YearRange_Click(object sender, RoutedEventArgs e)
    {
        _vm.SetThisYearRange();
        await LoadStatementSafelyAsync();
    }

    private async void AccountStatementYearCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_isInitializing || _isLoadingStatement || _vm.IsBusy)
            return;

        if ((sender as ComboBox)?.SelectedItem is FinancialYearOptionVm)
            await LoadStatementSafelyAsync();
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
        var parties = await LoadActivePartyOptionsAsync(companyId);

        ContentDialog dialog = isReceipt
            ? new ReceiptVoucherDialog(companyId, _vm.AccountOptions.ToList(), currencyOptions, parties, _query, accountId)
            : new PaymentVoucherDialog(companyId, _vm.AccountOptions.ToList(), currencyOptions, parties, _query, accountId);

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

    private async Task<IReadOnlyList<PartyOptionVm>> LoadActivePartyOptionsAsync(Guid companyId)
    {
        var parties = await _partyQuery.GetActivePartiesAsync(companyId);
        return parties
            .Select(x => new PartyOptionVm(x.Id, x.Code, x.NameAr, x.TypeFlags, x.LedgerMode, x.ReceivableAccountId, x.PayableAccountId, x.IsActive))
            .ToList();
    }

    private async Task LoadStatementSafelyAsync()
    {
        if (_isLoadingStatement)
            return;

        _isLoadingStatement = true;
        try
        {
            await _vm.LoadStatementPageAsync();
        }
        finally
        {
            _isLoadingStatement = false;
        }
    }
}
