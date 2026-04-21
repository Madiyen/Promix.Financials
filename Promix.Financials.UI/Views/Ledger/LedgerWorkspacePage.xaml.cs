using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Navigation;
using Microsoft.UI.Xaml.Media;
using Promix.Financials.UI.Navigation;
using Promix.Financials.UI.Views;
using Promix.Financials.UI.Views.Accounts;
using Promix.Financials.UI.Views.Journals;
using Promix.Financials.UI.Views.Parties;
using System;
using System.Collections.Generic;
using Microsoft.UI;

namespace Promix.Financials.UI.Views.Ledger;

public sealed partial class LedgerWorkspacePage : Page
{
    private static readonly SolidColorBrush ActiveTabForegroundBrush = new(ColorHelper.FromArgb(255, 15, 23, 42));
    private static readonly SolidColorBrush InactiveTabForegroundBrush = new(ColorHelper.FromArgb(255, 100, 116, 139));
    private readonly Dictionary<LedgerWorkspaceTab, Frame> _tabFrames;
    private readonly Dictionary<LedgerWorkspaceTab, ToggleButton> _tabButtons;
    private readonly HashSet<LedgerWorkspaceTab> _initializedTabs = new();
    private bool _isTabSelectionSyncing;
    private LedgerWorkspaceTab _selectedTab = LedgerWorkspaceTab.Accounts;
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

        _tabButtons = new()
        {
            [LedgerWorkspaceTab.Accounts] = AccountsTabButton,
            [LedgerWorkspaceTab.Journals] = JournalsTabButton,
            [LedgerWorkspaceTab.AccountStatement] = StatementTabButton,
            [LedgerWorkspaceTab.TrialBalance] = TrialBalanceTabButton,
            [LedgerWorkspaceTab.ReceivablesPayables] = ReceivablesTabButton,
            [LedgerWorkspaceTab.FinancialYears] = FinancialYearsTabButton
        };

        Loaded += (_, _) =>
        {
            SelectTab(GetSelectedOrDefaultTab());
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

    private void LedgerTabButton_Click(object sender, RoutedEventArgs e)
    {
        if (_isTabSelectionSyncing)
            return;

        if ((sender as ToggleButton)?.Tag is not string tag)
            return;

        SelectTab(ParseTabTag(tag));
    }

    private void SelectTab(LedgerWorkspaceTab tab)
    {
        _selectedTab = tab;
        _isTabSelectionSyncing = true;

        SyncTabVisuals(tab);

        foreach (var frame in _tabFrames)
            frame.Value.Visibility = frame.Key == tab ? Visibility.Visible : Visibility.Collapsed;

        _isTabSelectionSyncing = false;

        EnsureTabInitialized(tab);
        UpdateHeaderSummary(tab);
    }

    private void SyncTabVisuals(LedgerWorkspaceTab selectedTab)
    {
        foreach (var buttonEntry in _tabButtons)
        {
            var isSelected = buttonEntry.Key == selectedTab;
            var button = buttonEntry.Value;
            button.IsChecked = isSelected;
            button.Foreground = isSelected ? ActiveTabForegroundBrush : InactiveTabForegroundBrush;
            button.ApplyTemplate();

            if (FindNamedChild<Border>(button, "Underline") is Border underline)
                underline.Opacity = isSelected ? 1 : 0;
        }
    }

    private static T? FindNamedChild<T>(DependencyObject root, string childName) where T : FrameworkElement
    {
        var childCount = VisualTreeHelper.GetChildrenCount(root);
        for (var index = 0; index < childCount; index++)
        {
            var child = VisualTreeHelper.GetChild(root, index);
            if (child is T typedChild && typedChild.Name == childName)
                return typedChild;

            if (FindNamedChild<T>(child, childName) is T descendant)
                return descendant;
        }

        return null;
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

    private LedgerWorkspaceTab GetSelectedTab() => _selectedTab;

    private LedgerWorkspaceTab GetSelectedOrDefaultTab() => _selectedTab;

    private static LedgerWorkspaceTab ParseTabTag(string tag)
        => tag switch
        {
            "Journals" => LedgerWorkspaceTab.Journals,
            "AccountStatement" => LedgerWorkspaceTab.AccountStatement,
            "TrialBalance" => LedgerWorkspaceTab.TrialBalance,
            "ReceivablesPayables" => LedgerWorkspaceTab.ReceivablesPayables,
            "FinancialYears" => LedgerWorkspaceTab.FinancialYears,
            _ => LedgerWorkspaceTab.Accounts
        };

    private void UpdateHeaderSummary(LedgerWorkspaceTab tab)
    {
        var context = ResolveHeaderContext(tab);
        HeaderContextChanged?.Invoke(this, new LedgerWorkspaceHeaderContextChangedEventArgs(context.Title, context.Subtitle));
    }

    public LedgerWorkspaceHeaderContextChangedEventArgs GetHeaderContext()
    {
        var context = ResolveHeaderContext(GetSelectedTab());
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
