using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Promix.Financials.Application.Abstractions;
using Promix.Financials.UI.ViewModels.Reports;
using Promix.Financials.UI.ViewModels.Reports.Models;

namespace Promix.Financials.UI.Views;

public sealed partial class TrialBalancePage : Page
{
    private readonly IServiceScope _scope;
    private readonly TrialBalanceViewModel _vm;
    private readonly IUserContext _userContext;
    private bool _isInitializing;

    public TrialBalancePage()
    {
        var app = (App)Microsoft.UI.Xaml.Application.Current;
        _scope = app.Services.CreateScope();
        _vm = _scope.ServiceProvider.GetRequiredService<TrialBalanceViewModel>();
        _userContext = _scope.ServiceProvider.GetRequiredService<IUserContext>();

        InitializeComponent();
        DataContext = _vm;

        Loaded += OnLoaded;
        Unloaded += (_, _) => _scope.Dispose();
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (_userContext.CompanyId is null)
            return;

        _isInitializing = true;
        try
        {
            await _vm.InitializeAsync(_userContext.CompanyId.Value);
            SelectedPeriodModeRadio.IsChecked = _vm.IsSelectedPeriodMode;
            LockedSnapshotModeRadio.IsChecked = _vm.IsLockedSnapshotMode;
        }
        finally
        {
            _isInitializing = false;
        }
    }

    private async void LoadTrialBalance_Click(object sender, RoutedEventArgs e)
    {
        await _vm.LoadAsync();
    }

    private async void TrialBalanceYearCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_isInitializing)
            return;

        if ((sender as ComboBox)?.SelectedItem is FinancialYearOptionVm)
            await _vm.LoadAsync();
    }

    private async void SelectedPeriodModeRadio_Checked(object sender, RoutedEventArgs e)
    {
        if (!IsLoaded || _isInitializing)
            return;

        _vm.TrialBalanceViewModeKey = "selected";
        await _vm.LoadAsync();
    }

    private async void LockedSnapshotModeRadio_Checked(object sender, RoutedEventArgs e)
    {
        if (!IsLoaded || _isInitializing || !_vm.CanUseLockedSnapshot)
            return;

        _vm.TrialBalanceViewModeKey = "locked";
        await _vm.LoadAsync();
    }

    private async void IncludeZeroRowsCheckBox_Click(object sender, RoutedEventArgs e)
    {
        await _vm.LoadAsync();
    }

    private async void TodayRange_Click(object sender, RoutedEventArgs e)
    {
        _vm.SetTodayRange();
        await _vm.LoadAsync();
    }

    private async void MonthRange_Click(object sender, RoutedEventArgs e)
    {
        _vm.SetThisMonthRange();
        await _vm.LoadAsync();
    }

    private async void YearRange_Click(object sender, RoutedEventArgs e)
    {
        _vm.SetThisYearRange();
        await _vm.LoadAsync();
    }

    private void OpenStatementFromTrialBalance_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not FrameworkElement element || element.DataContext is not TrialBalanceRowVm row)
            return;

        (((App)Microsoft.UI.Xaml.Application.Current).CurrentWindow as MainWindow)?.NavigateTo(Controls.SidebarDestination.Reports, row.AccountId);
    }
}
