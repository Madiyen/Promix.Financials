using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using Promix.Financials.Application.Abstractions;
using Promix.Financials.Application.Features.Accounts.Services;
using Promix.Financials.Infrastructure;
using Promix.Financials.Infrastructure.Persistence;
using Promix.Financials.Infrastructure.Persistence.Seeding;
using Promix.Financials.Infrastructure.Security;
using Promix.Financials.UI.Security;
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
using Windows.UI;

namespace Promix.Financials.UI;

public partial class App : Microsoft.UI.Xaml.Application
{
    private const string ThemeSettingKey = "AppTheme";
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

            ApplyPreferredTheme(GetStoredTheme(), persist: false);

            if (_window is MainWindow mainWindow)
                mainWindow.RefreshAfterLogin();
        }
        catch (Exception ex)
        {
            ShowStartupFailure(ex);
        }
    }

    public ElementTheme GetStoredTheme()
    {
        var savedTheme = ApplicationData.Current.LocalSettings.Values[ThemeSettingKey] as string;
        return string.Equals(savedTheme, "Dark", StringComparison.OrdinalIgnoreCase)
            ? ElementTheme.Dark
            : ElementTheme.Light;
    }

    public void SetPreferredTheme(ElementTheme theme)
        => ApplyPreferredTheme(theme, persist: true);

    private void ApplyPreferredTheme(ElementTheme theme, bool persist)
    {
        if (persist)
            ApplicationData.Current.LocalSettings.Values[ThemeSettingKey] = theme == ElementTheme.Dark ? "Dark" : "Light";

        TryApplyThemePalette(theme);

        if (_window?.Content is FrameworkElement root)
            root.RequestedTheme = theme;
    }

    private void TryApplyThemePalette(ElementTheme theme)
    {
        try
        {
            ApplyThemePalette(theme);
        }
        catch (COMException)
        {
            // During the earliest startup phase WinUI may not expose Application.Resources yet.
            // The initial theme is applied again from OnLaunched once the window exists.
        }
    }

    private void ApplyThemePalette(ElementTheme theme)
    {
        if (theme == ElementTheme.Dark)
        {
            SetBrush("AppBackgroundBrush", "#020617");
            SetBrush("CardBackgroundBrush", "#0F172A");
            SetBrush("CardSubtleBackgroundBrush", "#111C31");
            SetBrush("BorderBrushSoft", "#16233B");
            SetBrush("BorderBrushStrong", "#243349");
            SetBrush("PrimaryTextBrush", "#E2E8F0");
            SetBrush("MainTitleTextBrush", "#F8FAFC");
            SetBrush("BrandNavyBrush", "#0F172A");
            SetBrush("SecondaryTextBrush", "#CBD5E1");
            SetBrush("MutedTextBrush", "#94A3B8");
            SetBrush("SlateTextBrush", "#B6C2D2");
            SetBrush("SidebarBackgroundBrush", "#08111F");
            SetBrush("SidebarCardBackgroundBrush", "#12233B");
            SetBrush("SidebarMutedTextBrush", "#D8E3F3");
            SetBrush("SidebarSoftTextBrush", "#AFC0D8");
            SetBrush("SidebarHoverBrush", "#1E3A5F");
            SetBrush("SuccessSurfaceBrush", "#052E1B");
            SetBrush("WarningSurfaceBrush", "#3A2708");
            SetBrush("DangerSurfaceBrush", "#3B0D12");
            SetBrush("InfoSurfaceBrush", "#0B1D34");
            SetBrush("SelectionHoverBrush", "#11243E");
            SetBrush("SelectionPressedBrush", "#173054");
            SetBrush("SelectionSelectedBrush", "#183153");
            SetBrush("SelectionHoverBorderBrush", "#2A4A75");
            SetBrush("SelectionSelectedBorderBrush", "#3B82F6");
            SetBrush("SecondaryButtonBrush", "#111C31");
            SetBrush("SecondaryButtonHoverBrush", "#16233B");
            SetBrush("SecondaryButtonPressedBrush", "#1C2D45");
            SetBrush("SecondaryButtonTextBrush", "#E2E8F0");
            SetBrush("SecondaryButtonDisabledBrush", "#0B1322");
            SetBrush("GhostButtonHoverBrush", "#12233B");
            SetBrush("GhostButtonPressedBrush", "#183153");
            SetBrush("GhostButtonDisabledBrush", "#0B1322");
            SetBrush("SidebarButtonPressedBrush", "#173054");
            SetBrush("SidebarButtonDisabledBrush", "#102033");
            SetBrush("DisabledForegroundBrush", "#64748B");
            SetBrush("DisabledBorderBrush", "#233247");
        }
        else
        {
            SetBrush("AppBackgroundBrush", "#F1F5F9");
            SetBrush("CardBackgroundBrush", "#FFFFFF");
            SetBrush("CardSubtleBackgroundBrush", "#F8FAFC");
            SetBrush("BorderBrushSoft", "#F1F5F9");
            SetBrush("BorderBrushStrong", "#E2E8F0");
            SetBrush("PrimaryTextBrush", "#0F172A");
            SetBrush("MainTitleTextBrush", "#0F172A");
            SetBrush("BrandNavyBrush", "#1E3A5F");
            SetBrush("SecondaryTextBrush", "#64748B");
            SetBrush("MutedTextBrush", "#94A3B8");
            SetBrush("SlateTextBrush", "#64748B");
            SetBrush("SidebarBackgroundBrush", "#1E3A5F");
            SetBrush("SidebarCardBackgroundBrush", "#28486E");
            SetBrush("SidebarMutedTextBrush", "#DCE8FB");
            SetBrush("SidebarSoftTextBrush", "#BFD0E8");
            SetBrush("SidebarHoverBrush", "#143B82F6");
            SetBrush("SuccessSurfaceBrush", "#ECFDF5");
            SetBrush("WarningSurfaceBrush", "#FFF7ED");
            SetBrush("DangerSurfaceBrush", "#FEF2F2");
            SetBrush("InfoSurfaceBrush", "#EFF6FF");
            SetBrush("SelectionHoverBrush", "#F8FBFF");
            SetBrush("SelectionPressedBrush", "#EDF4FF");
            SetBrush("SelectionSelectedBrush", "#EFF6FF");
            SetBrush("SelectionHoverBorderBrush", "#DBEAFE");
            SetBrush("SelectionSelectedBorderBrush", "#93C5FD");
            SetBrush("SecondaryButtonBrush", "#FFFFFF");
            SetBrush("SecondaryButtonHoverBrush", "#F8FAFC");
            SetBrush("SecondaryButtonPressedBrush", "#F1F5F9");
            SetBrush("SecondaryButtonTextBrush", "#1E3A5F");
            SetBrush("SecondaryButtonDisabledBrush", "#F1F5F9");
            SetBrush("GhostButtonHoverBrush", "#F8FAFC");
            SetBrush("GhostButtonPressedBrush", "#F1F5F9");
            SetBrush("GhostButtonDisabledBrush", "#F8FAFC");
            SetBrush("SidebarButtonPressedBrush", "#2A4A72");
            SetBrush("SidebarButtonDisabledBrush", "#294766");
            SetBrush("DisabledForegroundBrush", "#94A3B8");
            SetBrush("DisabledBorderBrush", "#D6E0EB");
        }
    }

    private void SetBrush(string key, string hexColor)
    {
        var color = ParseColor(hexColor);
        if (Resources.TryGetValue(key, out var value) && value is SolidColorBrush brush)
        {
            brush.Color = color;
            return;
        }

        Resources[key] = new SolidColorBrush(color);
    }

    private static Color ParseColor(string hexColor)
    {
        var value = hexColor.TrimStart('#');
        if (value.Length == 6)
            value = "FF" + value;

        return ColorHelper.FromArgb(
            Convert.ToByte(value[..2], 16),
            Convert.ToByte(value.Substring(2, 2), 16),
            Convert.ToByte(value.Substring(4, 2), 16),
            Convert.ToByte(value.Substring(6, 2), 16));
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
