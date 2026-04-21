using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using Microsoft.Windows.ApplicationModel.Resources;
using System;
using Windows.Globalization;
using Windows.Storage;

namespace Promix.Financials.UI.Views;

public sealed partial class SettingsView : Page
{
    private const string LanguageSettingKey = "AppLanguage";
    private bool _isInitializing;
    private readonly ResourceLoader _loader = new();

    public SettingsView()
    {
        InitializeComponent();
    }

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        LoadThemePreference();
        LoadLanguages();
    }

    private void LoadThemePreference()
    {
        _isInitializing = true;
        ThemeToggle.OnContent = _loader.GetString("Theme_Dark");
        ThemeToggle.OffContent = _loader.GetString("Theme_Light");
        ThemeToggle.IsOn = ((App)Microsoft.UI.Xaml.Application.Current).GetStoredTheme() == ElementTheme.Dark;
        _isInitializing = false;
    }

    private void LoadLanguages()
    {
        _isInitializing = true;

        var saved = ApplicationData.Current.LocalSettings.Values[LanguageSettingKey] as string;
        var current = !string.IsNullOrWhiteSpace(saved)
            ? saved
            : (string.IsNullOrWhiteSpace(ApplicationLanguages.PrimaryLanguageOverride)
                ? "en-US"
                : ApplicationLanguages.PrimaryLanguageOverride);

        LanguageCombo.Items.Clear();

        var enItem = new ComboBoxItem
        {
            Content = _loader.GetString("Lang_English"),
            Tag = "en-US"
        };

        var arItem = new ComboBoxItem
        {
            Content = _loader.GetString("Lang_Arabic"),
            Tag = "ar-SA"
        };

        LanguageCombo.Items.Add(enItem);
        LanguageCombo.Items.Add(arItem);

        LanguageCombo.SelectedItem =
            string.Equals(current, "ar-SA", StringComparison.OrdinalIgnoreCase) ? arItem : enItem;

        _isInitializing = false;
    }

    private void ThemeToggle_Toggled(object sender, RoutedEventArgs e)
    {
        if (_isInitializing)
            return;

        ((App)Microsoft.UI.Xaml.Application.Current).SetPreferredTheme(ThemeToggle.IsOn ? ElementTheme.Dark : ElementTheme.Light);
    }

    private async void LanguageCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_isInitializing) return;

        if (LanguageCombo.SelectedItem is not ComboBoxItem cbi) return;
        var selectedLang = cbi.Tag as string;
        if (string.IsNullOrWhiteSpace(selectedLang)) return;

        var settings = ApplicationData.Current.LocalSettings;
        var current = settings.Values[LanguageSettingKey] as string
            ?? (string.IsNullOrWhiteSpace(ApplicationLanguages.PrimaryLanguageOverride)
                ? "en-US"
                : ApplicationLanguages.PrimaryLanguageOverride);

        if (string.Equals(current, selectedLang, StringComparison.OrdinalIgnoreCase))
            return;

        settings.Values[LanguageSettingKey] = selectedLang;

        var dlg = new ContentDialog
        {
            Title = _loader.GetString("RestartRequired_Title"),
            Content = _loader.GetString("RestartRequired_Message"),
            PrimaryButtonText = _loader.GetString("RestartNow"),
            CloseButtonText = _loader.GetString("RestartLater"),
            XamlRoot = this.XamlRoot
        };

        var result = await dlg.ShowAsync();
        if (result == ContentDialogResult.Primary)
        {
            RestartApp();
        }
    }

    private static void RestartApp()
    {
        var exe = Environment.ProcessPath;
        if (!string.IsNullOrWhiteSpace(exe))
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(exe)
            {
                UseShellExecute = true
            });

        Environment.Exit(0);
    }
}
