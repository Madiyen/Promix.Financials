using Microsoft.UI.Xaml.Controls;
using System;
using System.Globalization;
using System.Linq;

namespace Promix.Financials.UI.Controls;

public sealed partial class AppHeader : UserControl
{
    public string SettingsTooltip => "الإعدادات";
    public event EventHandler? SettingsRequested;
    public event EventHandler? CommandPaletteRequested;

    public AppHeader()
    {
        InitializeComponent();
        RefreshDate();
        SetFiscalYearLabel("الفترة الحالية");
        SearchBox.PlaceholderText = "ابحث عن سند، حساب، أو تقرير...";
    }

    public void SetContext(string title, string subtitle)
    {
        TitleText.Text = title;
        SubtitleText.Text = subtitle;
        SubtitleText.Visibility = string.IsNullOrWhiteSpace(subtitle)
            ? Microsoft.UI.Xaml.Visibility.Collapsed
            : Microsoft.UI.Xaml.Visibility.Visible;
    }

    public void SetUser(string username)
    {
        AvatarText.Text = BuildInitials(username);
    }

    public void SetFiscalYearLabel(string label)
    {
        FiscalYearText.Text = label;
    }

    public void RefreshDate()
    {
        var culture = new CultureInfo("ar-SA");
        var now = DateTime.Now;
        var dayName = now.ToString("dddd", culture);
        var monthName = now.ToString("MMMM", culture);
        var day = now.Day.ToString(CultureInfo.InvariantCulture);
        var year = now.Year.ToString(CultureInfo.InvariantCulture);

        DateText.Text = $"{dayName}، {day} {monthName} {year}";
    }

    private static string BuildInitials(string username)
    {
        var parts = (username ?? string.Empty)
            .Split(new[] { ' ', '.', '_', '-' }, StringSplitOptions.RemoveEmptyEntries)
            .Take(2)
            .Select(p => char.ToUpperInvariant(p[0]))
            .ToArray();

        return parts.Length == 0 ? "PM" : new string(parts);
    }

    private void Settings_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        SettingsRequested?.Invoke(this, EventArgs.Empty);
    }

    private void SearchBox_GotFocus(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        CommandPaletteRequested?.Invoke(this, EventArgs.Empty);
    }

    private void SearchBox_Tapped(object sender, Microsoft.UI.Xaml.Input.TappedRoutedEventArgs e)
    {
        CommandPaletteRequested?.Invoke(this, EventArgs.Empty);
    }

    private void SearchBox_KeyDown(object sender, Microsoft.UI.Xaml.Input.KeyRoutedEventArgs e)
    {
        if (e.Key != Windows.System.VirtualKey.Enter)
            return;

        e.Handled = true;
        CommandPaletteRequested?.Invoke(this, EventArgs.Empty);
    }
}
