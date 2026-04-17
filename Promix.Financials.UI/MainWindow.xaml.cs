using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Navigation;
using Promix.Financials.Application.Abstractions;
using Promix.Financials.Application.Features.Accounts;
using Promix.Financials.Application.Features.Accounts.Commands;
using Promix.Financials.Application.Features.Accounts.Queries;
using Promix.Financials.Application.Features.Accounts.Services;
using Promix.Financials.Application.Features.Auth;
using Promix.Financials.Domain.Enums;
using Promix.Financials.UI.Controls;
using Promix.Financials.UI.Dialogs;
using Promix.Financials.UI.Dialogs.Accounts;
using Promix.Financials.UI.Dialogs.Parties;
using Promix.Financials.UI.Models;
using Promix.Financials.UI.Navigation;
using Promix.Financials.UI.Services;
using Promix.Financials.UI.Services.Journals;
using Promix.Financials.UI.ViewModels.Accounts;
using Promix.Financials.UI.Views;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Promix.Financials.UI.Views.Accounts;
using Promix.Financials.UI.Views.Journals;
using Promix.Financials.UI.Views.Ledger;
using Promix.Financials.UI.ViewModels.Parties.Models;
using Windows.Foundation;
using Windows.System;
namespace Promix.Financials.UI;

public sealed partial class MainWindow : Window
{
    private readonly IUserContext _userContext;
    private readonly IAuthService _authService;
    private readonly IUserContextBootstrapper _bootstrapper;
    private readonly TransientMessageService _messageService;
    private LedgerWorkspacePage? _activeLedgerWorkspace;
    private DispatcherQueueTimer? _messageTimer;
    private bool _isCommandPaletteOpen;
    public MainWindow()
    {
        InitializeComponent();

        var services = ((App)Microsoft.UI.Xaml.Application.Current).Services;
        _userContext = services.GetRequiredService<IUserContext>();
        _authService = services.GetRequiredService<IAuthService>();
        _bootstrapper = services.GetRequiredService<IUserContextBootstrapper>();
        _messageService = services.GetRequiredService<TransientMessageService>();

        Header.SettingsRequested += (_, __) =>
        {
            NavigateTo(SidebarDestination.Settings);
        };
        Header.CommandPaletteRequested += async (_, __) => await OpenCommandPaletteAsync();
        _messageService.MessageRaised += MessageService_MessageRaised;

        RootFrame.Navigated += RootFrame_Navigated;
        KeyboardAccelerators.Add(CreateAccelerator(VirtualKey.K, async (_, args) =>
        {
            args.Handled = true;
            await OpenCommandPaletteAsync();
        }));

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
                NavigateToLedger(new LedgerWorkspaceNavigationRequest(
                    LedgerWorkspaceTab.Journals,
                    EntryId: parameter is Guid entryId && entryId != Guid.Empty ? entryId : null));
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

        if (_activeLedgerWorkspace is not null)
        {
            _activeLedgerWorkspace.HeaderContextChanged -= LedgerWorkspace_HeaderContextChanged;
            _activeLedgerWorkspace = null;
        }

        var (destination, title, subtitle) = ResolveShellState(e.SourcePageType);
        Sidebar.SetActiveDestination(destination);
        Header.SetContext(title, subtitle);
        Header.RefreshDate();

        if (RootFrame.Content is LedgerWorkspacePage ledgerWorkspace)
        {
            _activeLedgerWorkspace = ledgerWorkspace;
            _activeLedgerWorkspace.HeaderContextChanged += LedgerWorkspace_HeaderContextChanged;
            var context = _activeLedgerWorkspace.GetHeaderContext();
            Header.SetContext(context.Title, context.Subtitle);
        }
    }

    private void LedgerWorkspace_HeaderContextChanged(object? sender, LedgerWorkspaceHeaderContextChangedEventArgs e)
    {
        Header.SetContext(e.Title, e.Subtitle);
    }

    private async Task OpenCommandPaletteAsync()
    {
        if (_isCommandPaletteOpen)
            return;

        var xamlRoot = ResolveXamlRoot();
        if (xamlRoot is null)
            return;

        _isCommandPaletteOpen = true;
        try
        {
            var items = await BuildCommandPaletteItemsAsync();
            var dialog = new CommandPaletteDialog(items)
            {
                XamlRoot = xamlRoot
            };

            await dialog.ShowAsync();

            if (dialog.SelectedCommand is not null)
                await ExecuteCommandPaletteItemAsync(dialog.SelectedCommand);
        }
        finally
        {
            _isCommandPaletteOpen = false;
        }
    }

    private async Task<IReadOnlyList<CommandPaletteItem>> BuildCommandPaletteItemsAsync()
    {
        var items = new List<CommandPaletteItem>
        {
            new(CommandPaletteActionKind.Navigate, "لوحة القيادة", "العودة إلى المؤشرات اليومية والحركة العامة.", "dashboard لوحة القيادة", "\uE80F", SidebarDestination.Dashboard),
            new(CommandPaletteActionKind.Navigate, "دفتر الأستاذ", "فتح مساحة العمل المالية الرئيسية.", "دفتر الأستاذ ledger", "\uE8D4", SidebarDestination.Ledger, LedgerWorkspaceTab.Accounts),
            new(CommandPaletteActionKind.Navigate, "الحسابات", "الانتقال مباشرة إلى شجرة الحسابات.", "الحسابات شجرة الحسابات chart accounts", "\uE8D4", SidebarDestination.ChartOfAccounts, LedgerWorkspaceTab.Accounts),
            new(CommandPaletteActionKind.Navigate, "القيود والسندات", "مراجعة القيود وإنشاء السندات.", "القيود السندات journals vouchers", "\uE70F", SidebarDestination.Journals, LedgerWorkspaceTab.Journals),
            new(CommandPaletteActionKind.Navigate, "كشف الحساب", "فتح تقرير كشف الحساب.", "كشف الحساب account statement", "\uE9D2", SidebarDestination.Reports, LedgerWorkspaceTab.AccountStatement),
            new(CommandPaletteActionKind.Navigate, "ميزان المراجعة", "مراجعة ميزان المراجعة للفترة.", "ميزان المراجعة trial balance", "\uE9D2", SidebarDestination.TrialBalance, LedgerWorkspaceTab.TrialBalance),
            new(CommandPaletteActionKind.Navigate, "الذمم", "الانتقال إلى الأطراف وكشوف الذمم.", "الذمم الأطراف parties receivables payables", "\uE716", SidebarDestination.Parties, LedgerWorkspaceTab.ReceivablesPayables),
            new(CommandPaletteActionKind.Navigate, "السنة المالية", "فتح إدارة السنوات والفترات المحاسبية.", "السنة المالية financial year periods", "\uE787", SidebarDestination.Ledger, LedgerWorkspaceTab.FinancialYears),
            new(CommandPaletteActionKind.CreateJournal, "سند قبض", "إنشاء سند قبض جديد مباشرة.", "سند قبض receipt voucher", "\uE8C7", JournalAction: JournalQuickAction.ReceiptVoucher),
            new(CommandPaletteActionKind.CreateJournal, "سند صرف", "إنشاء سند صرف جديد مباشرة.", "سند صرف payment voucher", "\uEAFD", JournalAction: JournalQuickAction.PaymentVoucher),
            new(CommandPaletteActionKind.CreateJournal, "سند تحويل", "إنشاء تحويل بين الحسابات.", "سند تحويل transfer voucher", "\uE7BF", JournalAction: JournalQuickAction.TransferVoucher),
            new(CommandPaletteActionKind.CreateJournal, "قيد يومية", "فتح محرر قيد يومية متعدد الأسطر.", "قيد يومية daily journal", "\uE70F", JournalAction: JournalQuickAction.DailyJournal),
            new(CommandPaletteActionKind.CreateJournal, "قيد افتتاحي", "فتح شاشة القيد الافتتاحي.", "قيد افتتاحي opening entry", "\uE823", JournalAction: JournalQuickAction.OpeningEntry),
            new(CommandPaletteActionKind.CreateAccount, "حساب جديد", "إضافة حساب جديد إلى شجرة الحسابات.", "حساب جديد account new", "\uE710"),
            new(CommandPaletteActionKind.CreateParty, "طرف جديد", "إضافة عميل أو مورد جديد.", "طرف جديد party customer vendor", "\uE8FA")
        };

        if (_userContext.CompanyId is not Guid companyId)
            return items;

        using var scope = ((App)Microsoft.UI.Xaml.Application.Current).Services.CreateScope();
        var chartQuery = scope.ServiceProvider.GetRequiredService<IChartOfAccountsQuery>();
        var accounts = await chartQuery.GetAccountsAsync(companyId);
        items.AddRange(
            accounts
                .Where(account => account.IsPosting && account.IsActive)
                .Take(150)
                .Select(account => new CommandPaletteItem(
                    CommandPaletteActionKind.OpenAccountStatement,
                    $"كشف حساب: {account.NameAr}",
                    $"{account.Code} · فتح كشف الحساب لهذا الحساب",
                    $"{account.Code} {account.NameAr} account statement كشف الحساب",
                    "\uE8D4",
                    SidebarDestination.Reports,
                    LedgerWorkspaceTab.AccountStatement,
                    AccountId: account.Id)));

        return items;
    }

    private async Task ExecuteCommandPaletteItemAsync(CommandPaletteItem item)
    {
        switch (item.ActionKind)
        {
            case CommandPaletteActionKind.Navigate:
                if (item.LedgerTab is LedgerWorkspaceTab ledgerTab)
                    NavigateToLedger(new LedgerWorkspaceNavigationRequest(ledgerTab));
                else if (item.Destination is SidebarDestination destination)
                    NavigateTo(destination);
                break;

            case CommandPaletteActionKind.OpenAccountStatement:
                if (item.AccountId is Guid accountId && accountId != Guid.Empty)
                    NavigateTo(SidebarDestination.Reports, accountId);
                break;

            case CommandPaletteActionKind.CreateJournal:
                await LaunchJournalQuickActionAsync(item.JournalAction);
                break;

            case CommandPaletteActionKind.CreateAccount:
                await OpenNewAccountFromShellAsync();
                break;

            case CommandPaletteActionKind.CreateParty:
                await OpenNewPartyFromShellAsync();
                break;
        }
    }

    private async Task LaunchJournalQuickActionAsync(JournalQuickAction? action)
    {
        if (_userContext.CompanyId is not Guid companyId || action is null)
            return;

        var xamlRoot = ResolveXamlRoot();
        if (xamlRoot is null)
            return;

        if (action == JournalQuickAction.OpeningEntry)
        {
            await OpenOpeningEntryAsync(companyId, xamlRoot);
            return;
        }

        var launcher = ((App)Microsoft.UI.Xaml.Application.Current).Services.GetRequiredService<JournalDialogLauncher>();
        var result = action switch
        {
            JournalQuickAction.ReceiptVoucher => await launcher.OpenReceiptVoucherAsync(companyId, xamlRoot),
            JournalQuickAction.PaymentVoucher => await launcher.OpenPaymentVoucherAsync(companyId, xamlRoot),
            JournalQuickAction.TransferVoucher => await launcher.OpenTransferVoucherAsync(companyId, xamlRoot),
            JournalQuickAction.DailyJournal => await launcher.OpenDailyJournalAsync(companyId, xamlRoot),
            _ => JournalDialogLaunchResult.FromCancel()
        };

        if (result.Saved)
            _messageService.ShowSuccess("تم حفظ السند بنجاح.");
        else if (!string.IsNullOrWhiteSpace(result.ErrorMessage))
            _messageService.ShowError(result.ErrorMessage);
    }

    private async Task OpenOpeningEntryAsync(Guid companyId, XamlRoot xamlRoot)
    {
        using var scope = ((App)Microsoft.UI.Xaml.Application.Current).Services.CreateScope();
        var query = scope.ServiceProvider.GetRequiredService<Promix.Financials.Application.Features.Journals.Queries.IJournalEntriesQuery>();
        var partyQuery = scope.ServiceProvider.GetRequiredService<Promix.Financials.Application.Features.Parties.Queries.IPartyQuery>();
        var createService = scope.ServiceProvider.GetRequiredService<Promix.Financials.Application.Features.Journals.Services.CreateJournalEntryService>();
        var accounts = await query.GetPostingAccountsAsync(companyId);
        var parties = await partyQuery.GetActivePartiesAsync(companyId);

        var dialog = new Dialogs.Journals.OpeningEntryDialog(
            companyId,
            accounts.Select(account => new ViewModels.Journals.Models.JournalAccountOptionVm(account.Id, account.Code, account.NameAr, account.Nature, account.SystemRole, account.IsLegacyPartyLinkedAccount)).ToList(),
            parties.Select(x => new PartyOptionVm(x.Id, x.Code, x.NameAr, x.TypeFlags, x.LedgerMode, x.ReceivableAccountId, x.PayableAccountId, x.IsActive)).ToList())
        {
            XamlRoot = xamlRoot
        };

        await dialog.ShowAsync();
        if (dialog.ResultCommand is null)
            return;

        await createService.CreateAsync(dialog.ResultCommand);
        _messageService.ShowSuccess(dialog.ResultCommand.PostNow ? "تم حفظ القيد الافتتاحي وترحيله." : "تم حفظ القيد الافتتاحي كمسودة.");
    }

    private async Task OpenNewAccountFromShellAsync()
    {
        if (_userContext.CompanyId is not Guid companyId)
            return;

        var xamlRoot = ResolveXamlRoot();
        if (xamlRoot is null)
            return;

        try
        {
            using var scope = ((App)Microsoft.UI.Xaml.Application.Current).Services.CreateScope();
            var vm = scope.ServiceProvider.GetRequiredService<NewAccountDialogViewModel>();
            await vm.InitializeAsync(companyId);

            var dialog = new NewAccountDialog(vm) { XamlRoot = xamlRoot };
            await dialog.ShowAsync();
            if (!dialog.IsSubmitted)
                return;

            var draft = vm.BuildDraft();
            var createService = scope.ServiceProvider.GetRequiredService<CreateAccountService>();
            await createService.CreateAsync(new CreateAccountCommand(
                draft.CompanyId,
                draft.ParentId,
                draft.Code,
                draft.ArabicName,
                draft.EnglishName,
                draft.IsPosting,
                DeriveNature(draft.Code),
                draft.CurrencyCode,
                draft.SystemRole,
                draft.IsActive,
                draft.Notes));

            _messageService.ShowSuccess("تم إنشاء الحساب الجديد.");
            NavigateToLedger(new LedgerWorkspaceNavigationRequest(LedgerWorkspaceTab.Accounts));
        }
        catch (Exception ex)
        {
            _messageService.ShowError(ex.Message, "تعذر إنشاء الحساب");
        }
    }

    private async Task OpenNewPartyFromShellAsync()
    {
        if (_userContext.CompanyId is not Guid companyId)
            return;

        var xamlRoot = ResolveXamlRoot();
        if (xamlRoot is null)
            return;

        try
        {
            using var scope = ((App)Microsoft.UI.Xaml.Application.Current).Services.CreateScope();
            var dialog = new PartyDialog(companyId) { XamlRoot = xamlRoot };
            await dialog.ShowAsync();
            if (!dialog.IsSubmitted)
                return;

            var createService = scope.ServiceProvider.GetRequiredService<Promix.Financials.Application.Features.Parties.Services.CreatePartyService>();
            await createService.CreateAsync(dialog.BuildCreateCommand());
            _messageService.ShowSuccess("تمت إضافة الطرف بنجاح.");
            NavigateToLedger(new LedgerWorkspaceNavigationRequest(LedgerWorkspaceTab.ReceivablesPayables));
        }
        catch (Exception ex)
        {
            _messageService.ShowError(ex.Message, "تعذر إضافة الطرف");
        }
    }

    private void MessageService_MessageRaised(object? sender, TransientMessageRequest e)
    {
        DispatcherQueue.TryEnqueue(() =>
        {
            GlobalInfoBar.Severity = e.Severity;
            GlobalInfoBar.Title = e.Title;
            GlobalInfoBar.Message = e.Message;
            GlobalInfoBar.IsOpen = true;

            _messageTimer?.Stop();
            _messageTimer = null;

            if (e.AutoDismissAfter is not TimeSpan dismissAfter)
                return;

            _messageTimer = DispatcherQueue.CreateTimer();
            _messageTimer.Interval = dismissAfter;
            _messageTimer.IsRepeating = false;
            _messageTimer.Tick += (_, _) =>
            {
                GlobalInfoBar.IsOpen = false;
                _messageTimer?.Stop();
                _messageTimer = null;
            };
            _messageTimer.Start();
        });
    }

    private void GlobalInfoBar_Closed(InfoBar sender, InfoBarClosedEventArgs args)
    {
        _messageTimer?.Stop();
        _messageTimer = null;
    }

    private XamlRoot? ResolveXamlRoot()
        => Content is FrameworkElement element
            ? element.XamlRoot
            : Header.XamlRoot;

    private static KeyboardAccelerator CreateAccelerator(
        VirtualKey key,
        TypedEventHandler<KeyboardAccelerator, KeyboardAcceleratorInvokedEventArgs> handler,
        VirtualKeyModifiers modifiers = VirtualKeyModifiers.Control)
    {
        var accelerator = new KeyboardAccelerator
        {
            Key = key,
            Modifiers = modifiers
        };
        accelerator.Invoked += handler;
        return accelerator;
    }

    private static AccountNature DeriveNature(string code)
    {
        var root = code?.Split('.')[0] ?? string.Empty;
        return root switch
        {
            "2" or "3" or "4" => AccountNature.Credit,
            _ => AccountNature.Debit
        };
    }

    private static (SidebarDestination Destination, string Title, string Subtitle) ResolveShellState(Type pageType)
    {
        if (pageType == typeof(DashboardView))
        {
            return (SidebarDestination.Dashboard, "لوحة القيادة", string.Empty);
        }

        if (pageType == typeof(LedgerWorkspacePage))
        {
            return (SidebarDestination.Ledger, "دفتر الأستاذ", string.Empty);
        }

        if (pageType == typeof(Promix.Financials.UI.Views.Accounts.ChartOfAccountsView))
        {
            return (SidebarDestination.Ledger, "الحسابات", string.Empty);
        }

        if (pageType == typeof(JournalEntriesPage))
        {
            return (SidebarDestination.Ledger, "القيود والسندات", string.Empty);
        }

        if (pageType == typeof(Promix.Financials.UI.Views.Parties.PartiesPage))
        {
            return (SidebarDestination.Ledger, "الذمم", string.Empty);
        }

        if (pageType == typeof(Promix.Financials.UI.Views.ReportsPage))
        {
            return (SidebarDestination.Ledger, "كشف الحساب", string.Empty);
        }

        if (pageType == typeof(Promix.Financials.UI.Views.TrialBalancePage))
        {
            return (SidebarDestination.Ledger, "ميزان المراجعة", string.Empty);
        }

        if (pageType == typeof(Promix.Financials.UI.Views.Currencies.CompanyCurrenciesView))
        {
            return (SidebarDestination.Currencies, "العملات", string.Empty);
        }

        if (pageType == typeof(Promix.Financials.UI.Views.ItemsPage))
        {
            return (SidebarDestination.Items, "الأصناف", string.Empty);
        }

        if (pageType == typeof(SettingsView))
        {
            return (SidebarDestination.Settings, "الإعدادات", string.Empty);
        }

        return (SidebarDestination.Dashboard, "لوحة القيادة", string.Empty);
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
