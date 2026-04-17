using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.UI.Xaml;
using Promix.Financials.Application.Abstractions;
using Promix.Financials.Application.Features.Accounts.Services;
using Promix.Financials.Infrastructure;
using Promix.Financials.Infrastructure.Persistence;
using Promix.Financials.Infrastructure.Persistence.Seeding;
using Promix.Financials.Infrastructure.Security;
using Promix.Financials.UI.Security;
using Promix.Financials.UI.Services;
using Promix.Financials.UI.Services.Journals;
using Promix.Financials.UI.ViewModels.Accounts;
using Promix.Financials.UI.ViewModels.Currencies;
using Promix.Financials.UI.ViewModels.Dashboard;
using Promix.Financials.UI.ViewModels.Journals;
using Promix.Financials.UI.ViewModels.Ledger;
using Promix.Financials.UI.ViewModels.Parties;
using Promix.Financials.UI.ViewModels.Reports;
using System;
using System.Runtime.InteropServices;
using Windows.Globalization;
using Windows.Storage;

namespace Promix.Financials.UI;

public partial class App : Microsoft.UI.Xaml.Application
{
    private Window? _window;
    private readonly IHost _host;
    public IServiceProvider Services => _host.Services;
    public Window? CurrentWindow => _window;

    public App()
    {
        InitializeComponent();

        // ✅ UnhandledException في Constructor — المكان الصحيح
        UnhandledException += (sender, e) =>
        {
            var msg = $"[CRASH] {e.Exception?.GetType()?.FullName}\n" +
                      $"Message: {e.Exception?.Message}\n" +
                      $"Inner: {e.Exception?.InnerException?.Message}\n" +
                      $"Stack: {e.Exception?.StackTrace}";

            System.Diagnostics.Debug.WriteLine(msg);

            try
            {
                var crashPath = System.IO.Path.Combine(
    ApplicationData.Current.LocalFolder.Path, "crash_log.txt");
                System.IO.File.WriteAllText(crashPath, msg);
            }
            catch { /* تجاهل إذا فشل الكتابة */ }

            e.Handled = true;
        };

        var savedLang = ApplicationData.Current.LocalSettings.Values["AppLanguage"] as string;
        if (!string.IsNullOrWhiteSpace(savedLang))
            ApplicationLanguages.PrimaryLanguageOverride = savedLang;

        _host = Host.CreateDefaultBuilder()
            .ConfigureServices(services =>
            {
                var config = new ConfigurationBuilder()
                    .SetBasePath(AppContext.BaseDirectory)
                    .AddJsonFile("appsettings.json", optional: false)
                    .Build();

                var cs = config.GetConnectionString("Promix")
                    ?? throw new InvalidOperationException("Missing ConnectionStrings:Promix in appsettings.json");
                services.AddInfrastructure(cs);
                services.AddTransient<CompanyCurrenciesViewModel>();
                services.AddTransient<DashboardViewModel>();
                services.AddTransient<JournalEntriesViewModel>();
                services.AddTransient<PartiesPageViewModel>();
                services.AddTransient<FinancialYearsViewModel>();
                services.AddTransient<AccountStatementViewModel>();
                services.AddTransient<TrialBalanceViewModel>();
                services.AddSingleton<IJournalQuickDefaultsStore, LocalSettingsJournalQuickDefaultsStore>();
                services.AddSingleton<ISessionStore, LocalSettingsSessionStore>();
                services.AddSingleton<TransientMessageService>();
                services.AddTransient<JournalDialogLauncher>();
                services.AddTransient<ChartOfAccountsViewModel>();
                services.AddTransient<NewAccountDialogViewModel>();
                
                services.AddTransient<EditAccountDialogViewModel>();
            })
            .Build();
    }

    protected override async void OnLaunched(Microsoft.UI.Xaml.LaunchActivatedEventArgs args)
    {
        try
        {
            using (var scope = Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<PromixDbContext>();
                var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();
                await SeedData.EnsureSeedAsync(db, hasher);
            }

            var bootstrapper = Services.GetRequiredService<IUserContextBootstrapper>();
            await bootstrapper.InitializeAsync();

            _window = new MainWindow();

            var lang = ApplicationLanguages.PrimaryLanguageOverride;
            if (_window.Content is FrameworkElement fe)
            {
                fe.FlowDirection = (!string.IsNullOrWhiteSpace(lang) && lang.StartsWith("ar"))
                    ? FlowDirection.RightToLeft
                    : FlowDirection.LeftToRight;
            }

            _window.Activate();

            if (_window is MainWindow mainWindow)
                mainWindow.RefreshAfterLogin();
        }
        catch (Exception ex)
        {
            ShowStartupFailure(ex);
        }
    }

    private void ShowStartupFailure(Exception ex)
    {
        const string title = "خطأ في التشغيل";
        var message = $"فشل تشغيل التطبيق:{Environment.NewLine}{Environment.NewLine}{ex.Message}{Environment.NewLine}{Environment.NewLine}تحقق من اتصال قاعدة البيانات أو راجع سجل الأعطال.";

        try
        {
            if (_window?.Content is FrameworkElement root && root.XamlRoot is not null)
            {
                var dialog = new Microsoft.UI.Xaml.Controls.ContentDialog
                {
                    XamlRoot = root.XamlRoot,
                    Title = title,
                    Content = message,
                    CloseButtonText = "إغلاق"
                };

                _window.DispatcherQueue.TryEnqueue(async () => await dialog.ShowAsync());
                return;
            }
        }
        catch
        {
            // ننتقل إلى الرسالة الأصلية على مستوى النظام إذا فشل مسار WinUI.
        }

        NativeMethods.MessageBoxW(nint.Zero, message, title, 0x00000010);
    }

    private static partial class NativeMethods
    {
        [DllImport("user32.dll", CharSet = CharSet.Unicode, EntryPoint = "MessageBoxW")]
        public static extern int MessageBoxW(nint hWnd, string text, string caption, uint type);
    }
}
