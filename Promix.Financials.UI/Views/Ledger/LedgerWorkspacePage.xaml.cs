using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using Promix.Financials.UI.Navigation;
using Promix.Financials.UI.Views;
using Promix.Financials.UI.Views.Accounts;
using Promix.Financials.UI.Views.Journals;
using Promix.Financials.UI.Views.Parties;
using System;
using System.Collections.Generic;

namespace Promix.Financials.UI.Views.Ledger;

public sealed partial class LedgerWorkspacePage : Page
{
    private readonly Dictionary<LedgerWorkspaceTab, Frame> _tabFrames;
    private readonly HashSet<LedgerWorkspaceTab> _initializedTabs = new();
    private bool _isTabSelectionSyncing;
    public event EventHandler<LedgerWorkspaceHeaderContextChangedEventArgs>? HeaderContextChanged;

    public LedgerWorkspacePage()
    {
        InitializeComponent();
        NavigationCacheMode = NavigationCacheMode.Required;

        _tabFrames = new()
        {
            [LedgerWorkspaceTab.Accounts] = AccountsFrame,
            [LedgerWorkspaceTab.Journals] = JournalsFrame,
            [LedgerWorkspaceTab.AccountStatement] = StatementFrame,
            [LedgerWorkspaceTab.TrialBalance] = TrialBalanceFrame,
            [LedgerWorkspaceTab.ReceivablesPayables] = ReceivablesFrame,
            [LedgerWorkspaceTab.FinancialYears] = FinancialYearsFrame
        };

        Loaded += (_, _) =>
        {
            if (LedgerTabView.SelectedItem is not TabViewItem)
            {
                SelectTab(LedgerWorkspaceTab.Accounts);
            }
            else
            {
                EnsureTabInitialized(GetSelectedTab());
                UpdateHeaderSummary(GetSelectedTab());
            }
        };
    }

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);

        var request = e.Parameter as LedgerWorkspaceNavigationRequest
            ?? new LedgerWorkspaceNavigationRequest(GetSelectedOrDefaultTab());

        ApplyNavigationRequest(request);
    }

    public void ApplyNavigationRequest(LedgerWorkspaceNavigationRequest request)
    {
        if (request is null)
            return;

        SelectTab(request.InitialTab);

        if (request.InitialTab == LedgerWorkspaceTab.AccountStatement && request.AccountId is Guid accountId && accountId != Guid.Empty)
        {
            StatementFrame.Navigate(typeof(ReportsPage), accountId);
            _initializedTabs.Add(LedgerWorkspaceTab.AccountStatement);
        }
        else
        {
            EnsureTabInitialized(request.InitialTab);
        }

        UpdateHeaderSummary(request.InitialTab);
    }

    private void LedgerTabView_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_isTabSelectionSyncing)
            return;

        var selectedTab = GetSelectedTab();
        EnsureTabInitialized(selectedTab);
        UpdateHeaderSummary(selectedTab);
    }

    private void SelectTab(LedgerWorkspaceTab tab)
    {
        _isTabSelectionSyncing = true;
        LedgerTabView.SelectedIndex = (int)tab;
        _isTabSelectionSyncing = false;
    }

    private void EnsureTabInitialized(LedgerWorkspaceTab tab)
    {
        if (_initializedTabs.Contains(tab))
            return;

        var frame = _tabFrames[tab];
        frame.Navigate(ResolvePageType(tab));
        _initializedTabs.Add(tab);
    }

    private static Type ResolvePageType(LedgerWorkspaceTab tab)
        => tab switch
        {
            LedgerWorkspaceTab.Accounts => typeof(ChartOfAccountsView),
            LedgerWorkspaceTab.Journals => typeof(JournalEntriesPage),
            LedgerWorkspaceTab.AccountStatement => typeof(ReportsPage),
            LedgerWorkspaceTab.TrialBalance => typeof(TrialBalancePage),
            LedgerWorkspaceTab.ReceivablesPayables => typeof(PartiesPage),
            LedgerWorkspaceTab.FinancialYears => typeof(FinancialYearsPage),
            _ => typeof(ChartOfAccountsView)
        };

    private LedgerWorkspaceTab GetSelectedTab()
        => LedgerTabView.SelectedIndex switch
        {
            1 => LedgerWorkspaceTab.Journals,
            2 => LedgerWorkspaceTab.AccountStatement,
            3 => LedgerWorkspaceTab.TrialBalance,
            4 => LedgerWorkspaceTab.ReceivablesPayables,
            5 => LedgerWorkspaceTab.FinancialYears,
            _ => LedgerWorkspaceTab.Accounts
        };

    private LedgerWorkspaceTab GetSelectedOrDefaultTab()
        => LedgerTabView.SelectedItem is TabViewItem ? GetSelectedTab() : LedgerWorkspaceTab.Accounts;

    private void UpdateHeaderSummary(LedgerWorkspaceTab tab)
    {
        var context = ResolveHeaderContext(tab);
        HeaderContextChanged?.Invoke(this, new LedgerWorkspaceHeaderContextChangedEventArgs(context.Title, context.Subtitle));
    }

    public LedgerWorkspaceHeaderContextChangedEventArgs GetHeaderContext()
    {
        var context = ResolveHeaderContext(GetSelectedOrDefaultTab());
        return new LedgerWorkspaceHeaderContextChangedEventArgs(context.Title, context.Subtitle);
    }

    private static (string Title, string Subtitle) ResolveHeaderContext(LedgerWorkspaceTab tab)
        => tab switch
        {
            LedgerWorkspaceTab.Accounts => ("الحسابات", string.Empty),
            LedgerWorkspaceTab.Journals => ("القيود والسندات", string.Empty),
            LedgerWorkspaceTab.AccountStatement => ("كشف الحساب", string.Empty),
            LedgerWorkspaceTab.TrialBalance => ("ميزان المراجعة", string.Empty),
            LedgerWorkspaceTab.ReceivablesPayables => ("الذمم", string.Empty),
            LedgerWorkspaceTab.FinancialYears => ("السنة المالية", string.Empty),
            _ => ("دفتر الأستاذ", string.Empty)
        };
}

public sealed class LedgerWorkspaceHeaderContextChangedEventArgs : EventArgs
{
    public LedgerWorkspaceHeaderContextChangedEventArgs(string title, string subtitle)
    {
        Title = title;
        Subtitle = subtitle;
    }

    public string Title { get; }

    public string Subtitle { get; }
}
