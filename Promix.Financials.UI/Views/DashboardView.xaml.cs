using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Promix.Financials.Application.Abstractions;
using Promix.Financials.UI;
using Promix.Financials.UI.Controls;
using Promix.Financials.UI.Services.Journals;
using Promix.Financials.UI.ViewModels.Dashboard;
using System;
using System.Threading.Tasks;

namespace Promix.Financials.UI.Views;

public sealed partial class DashboardView : Page
{
    private readonly IServiceScope _scope;
    private readonly DashboardViewModel _vm;
    private readonly IUserContext _userContext;
    private readonly JournalDialogLauncher _dialogLauncher;

    public DashboardView()
    {
        var app = (App)Microsoft.UI.Xaml.Application.Current;
        _scope = app.Services.CreateScope();
        _vm = _scope.ServiceProvider.GetRequiredService<DashboardViewModel>();
        _userContext = _scope.ServiceProvider.GetRequiredService<IUserContext>();
        _dialogLauncher = _scope.ServiceProvider.GetRequiredService<JournalDialogLauncher>();

        InitializeComponent();
        DataContext = _vm;

        Loaded += OnLoaded;
        Unloaded += OnUnloaded;
        QuickActionsPanel.QuickActionRequested += QuickActionsPanel_QuickActionRequested;
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (_userContext.CompanyId is null)
        {
            return;
        }

        await _vm.InitializeAsync(_userContext.CompanyId.Value);
    }

    private void OnUnloaded(object sender, RoutedEventArgs e)
    {
        QuickActionsPanel.QuickActionRequested -= QuickActionsPanel_QuickActionRequested;
        _scope.Dispose();
    }

    private async void QuickActionsPanel_QuickActionRequested(object? sender, QuickActionRequestedEventArgs e)
    {
        switch (e.Action)
        {
            case DashboardQuickAction.OpenAccounts:
                (((App)Microsoft.UI.Xaml.Application.Current).CurrentWindow as MainWindow)?.NavigateTo(SidebarDestination.ChartOfAccounts);
                return;
            case DashboardQuickAction.OpenReports:
                (((App)Microsoft.UI.Xaml.Application.Current).CurrentWindow as MainWindow)?.NavigateTo(SidebarDestination.Reports);
                return;
        }

        if (_userContext.CompanyId is null)
        {
            await ShowErrorAsync("لا يمكن تنفيذ هذا الإجراء قبل اختيار شركة فعّالة.");
            return;
        }

        JournalDialogLaunchResult result = e.Action switch
        {
            DashboardQuickAction.CreateReceiptVoucher => await _dialogLauncher.OpenReceiptVoucherAsync(_userContext.CompanyId.Value, XamlRoot),
            DashboardQuickAction.CreatePaymentVoucher => await _dialogLauncher.OpenPaymentVoucherAsync(_userContext.CompanyId.Value, XamlRoot),
            DashboardQuickAction.CreateTransferVoucher => await _dialogLauncher.OpenTransferVoucherAsync(_userContext.CompanyId.Value, XamlRoot),
            DashboardQuickAction.CreateDailyJournal => await _dialogLauncher.OpenDailyJournalAsync(_userContext.CompanyId.Value, XamlRoot),
            _ => JournalDialogLaunchResult.FromCancel()
        };

        if (!result.Saved)
        {
            if (!string.IsNullOrWhiteSpace(result.ErrorMessage))
            {
                await ShowErrorAsync(result.ErrorMessage);
            }

            return;
        }

        await _vm.RefreshAsync();
    }

    private async Task ShowErrorAsync(string message)
    {
        var dialog = new ContentDialog
        {
            Title = "تعذر إكمال العملية",
            Content = message,
            CloseButtonText = "إغلاق",
            XamlRoot = XamlRoot
        };

        await dialog.ShowAsync();
    }
}
