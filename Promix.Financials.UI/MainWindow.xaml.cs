using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using Promix.Financials.Application.Abstractions;
using Promix.Financials.Application.Features.Auth;
using Promix.Financials.UI.Controls;
using Promix.Financials.UI.Navigation;
using Promix.Financials.UI.Views;
using System;
using Promix.Financials.UI.Views.Accounts;
using Promix.Financials.UI.Views.Journals;
using Promix.Financials.UI.Views.Ledger;
namespace Promix.Financials.UI;

public sealed partial class MainWindow : Window
{
    private readonly IUserContext _userContext;
    private readonly IAuthService _authService;
    private readonly IUserContextBootstrapper _bootstrapper;
    public MainWindow()
    {
        InitializeComponent();

        var services = ((App)Microsoft.UI.Xaml.Application.Current).Services;
        _userContext = services.GetRequiredService<IUserContext>();
        _authService = services.GetRequiredService<IAuthService>();
        _bootstrapper = services.GetRequiredService<IUserContextBootstrapper>();

        Header.SettingsRequested += (_, __) =>
        {
            NavigateTo(SidebarDestination.Settings);
        };

        RootFrame.Navigated += RootFrame_Navigated;

        InitializeNavigation();
    }

    private void InitializeNavigation()
    {
        if (!_userContext.IsAuthenticated)
        {
            Header.Visibility = Visibility.Collapsed;
            Sidebar.Visibility = Visibility.Collapsed;
            RootFrame.Navigate(typeof(LoginView));
            return;
        }

        // ✅ authenticated but company not selected yet
        if (_userContext.CompanyId is null)
        {
            Header.Visibility = Visibility.Collapsed;
            Sidebar.Visibility = Visibility.Collapsed;
            RootFrame.Navigate(typeof(CompanySelectionView));
            return;
        }

        Header.Visibility = Visibility.Visible;
        Sidebar.Visibility = Visibility.Visible;
        Sidebar.SetUserInfo(_userContext.Username, string.Empty);
        Header.SetUser(_userContext.Username);
        RootFrame.Navigate(typeof(DashboardView));
    }

    public void RefreshAfterLogin()
    {
        InitializeNavigation();
    }
    public void RefreshAfterCompanySelected()
    {
        InitializeNavigation();
    }

    private void Sidebar_NavigateRequested(object sender, SidebarNavigateEventArgs e)
        => NavigateTo(e.Destination);

    public void NavigateTo(SidebarDestination destination, object? parameter = null)
    {
        switch (destination)
        {
            case SidebarDestination.Dashboard:
                RootFrame.Navigate(typeof(DashboardView));
                break;

            case SidebarDestination.Ledger:
                NavigateToLedger(new LedgerWorkspaceNavigationRequest(LedgerWorkspaceTab.Accounts));
                break;

            case SidebarDestination.ChartOfAccounts:
                NavigateToLedger(new LedgerWorkspaceNavigationRequest(LedgerWorkspaceTab.Accounts));
                break;

            case SidebarDestination.Journals:
                NavigateToLedger(new LedgerWorkspaceNavigationRequest(LedgerWorkspaceTab.Journals));
                break;

            case SidebarDestination.Parties:
                NavigateToLedger(new LedgerWorkspaceNavigationRequest(LedgerWorkspaceTab.ReceivablesPayables));
                break;

            case SidebarDestination.Items:
                RootFrame.Navigate(typeof(Promix.Financials.UI.Views.ItemsPage));
                break;

            case SidebarDestination.Reports:
                NavigateToLedger(new LedgerWorkspaceNavigationRequest(
                    LedgerWorkspaceTab.AccountStatement,
                    parameter is Guid accountId && accountId != Guid.Empty ? accountId : null));
                break;

            case SidebarDestination.TrialBalance:
                NavigateToLedger(new LedgerWorkspaceNavigationRequest(LedgerWorkspaceTab.TrialBalance));
                break;

            case SidebarDestination.Settings:
                RootFrame.Navigate(typeof(SettingsView));
                break;
            case SidebarDestination.Currencies:
                RootFrame.Navigate(typeof(Promix.Financials.UI.Views.Currencies.CompanyCurrenciesView));
                break;
        }
    }

    private void NavigateToLedger(LedgerWorkspaceNavigationRequest request)
    {
        if (RootFrame.Content is LedgerWorkspacePage workspace)
        {
            workspace.ApplyNavigationRequest(request);
            return;
        }

        RootFrame.Navigate(typeof(LedgerWorkspacePage), request);
    }

    private void RootFrame_Navigated(object sender, NavigationEventArgs e)
    {
        if (Header.Visibility != Visibility.Visible || Sidebar.Visibility != Visibility.Visible)
        {
            return;
        }

        var (destination, title, subtitle) = ResolveShellState(e.SourcePageType);
        Sidebar.SetActiveDestination(destination);
        Header.SetContext(title, subtitle);
        Header.RefreshDate();
    }

    private static (SidebarDestination Destination, string Title, string Subtitle) ResolveShellState(Type pageType)
    {
        if (pageType == typeof(DashboardView))
        {
            return (SidebarDestination.Dashboard, "لوحة القيادة", "صورة سريعة للحركة اليومية والمؤشرات المالية الأساسية.");
        }

        if (pageType == typeof(LedgerWorkspacePage))
        {
            return (SidebarDestination.Ledger, "دفتر الأستاذ", "مساحة موحّدة للحسابات والقيود وكشف الحساب وميزان المراجعة والذمم والسنة المالية.");
        }

        if (pageType == typeof(Promix.Financials.UI.Views.Accounts.ChartOfAccountsView))
        {
            return (SidebarDestination.Ledger, "دفتر الأستاذ", "مساحة موحّدة للحسابات والقيود وكشف الحساب وميزان المراجعة والذمم والسنة المالية.");
        }

        if (pageType == typeof(JournalEntriesPage))
        {
            return (SidebarDestination.Ledger, "دفتر الأستاذ", "مساحة موحّدة للحسابات والقيود وكشف الحساب وميزان المراجعة والذمم والسنة المالية.");
        }

        if (pageType == typeof(Promix.Financials.UI.Views.Parties.PartiesPage))
        {
            return (SidebarDestination.Ledger, "دفتر الأستاذ", "مساحة موحّدة للحسابات والقيود وكشف الحساب وميزان المراجعة والذمم والسنة المالية.");
        }

        if (pageType == typeof(Promix.Financials.UI.Views.ReportsPage))
        {
            return (SidebarDestination.Ledger, "دفتر الأستاذ", "مساحة موحّدة للحسابات والقيود وكشف الحساب وميزان المراجعة والذمم والسنة المالية.");
        }

        if (pageType == typeof(Promix.Financials.UI.Views.TrialBalancePage))
        {
            return (SidebarDestination.Ledger, "دفتر الأستاذ", "مساحة موحّدة للحسابات والقيود وكشف الحساب وميزان المراجعة والذمم والسنة المالية.");
        }

        if (pageType == typeof(Promix.Financials.UI.Views.Currencies.CompanyCurrenciesView))
        {
            return (SidebarDestination.Currencies, "العملات", "إدارة العملات الافتراضية وأسعار الصرف حسب الشركة.");
        }

        if (pageType == typeof(Promix.Financials.UI.Views.ItemsPage))
        {
            return (SidebarDestination.Items, "الأصناف", "واجهة مبدئية محفوظة لمسار الأصناف عند تفعيله لاحقًا.");
        }

        if (pageType == typeof(SettingsView))
        {
            return (SidebarDestination.Settings, "الإعدادات", "ضبط النظام والمظهر والخيارات العامة للتطبيق.");
        }

        return (SidebarDestination.Dashboard, "لوحة القيادة", "صورة سريعة للحركة اليومية والمؤشرات المالية الأساسية.");
    }


    private async void Sidebar_LogoutRequested(object sender, EventArgs e)
    {
        await _authService.LogoutAsync();

        // ✅ أعِد مزامنة IUserContext (سيصبح غير مسجل)
        await _bootstrapper.InitializeAsync();

        Header.Visibility = Visibility.Collapsed;
        Sidebar.Visibility = Visibility.Collapsed;

        RootFrame.Navigate(typeof(LoginView));
    }
}
