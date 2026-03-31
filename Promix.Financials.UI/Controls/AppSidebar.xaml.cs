using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace Promix.Financials.UI.Controls;

public sealed partial class AppSidebar : UserControl
{
    private readonly Dictionary<SidebarDestination, Button> _buttonMap;

    public event EventHandler<SidebarNavigateEventArgs>? NavigateRequested;
    public event EventHandler? LogoutRequested;

    public AppSidebar()
    {
        InitializeComponent();

        _buttonMap = new()
        {
            [SidebarDestination.Dashboard] = BtnDashboard,
            [SidebarDestination.Ledger] = BtnLedger,
            [SidebarDestination.Currencies] = BtnCurrencies,
            [SidebarDestination.Items] = BtnItems,
            [SidebarDestination.Settings] = BtnSettings,
        };

        SetActiveDestination(SidebarDestination.Dashboard);
    }

    public void SetUserInfo(string username, string companyName)
    {
        UserNameText.Text = HumanizeUsername(username);
        CompanyText.Text = string.IsNullOrWhiteSpace(companyName) ? string.Empty : companyName;
        CompanyText.Visibility = string.IsNullOrWhiteSpace(companyName) ? Visibility.Collapsed : Visibility.Visible;
        AvatarInitialsText.Text = BuildInitials(username);
    }

    public void SetActiveDestination(SidebarDestination destination)
    {
        foreach (var pair in _buttonMap)
        {
            ApplyButtonState(pair.Value, pair.Key == destination);
        }
    }

    private static void ApplyButtonState(Button button, bool isActive)
    {
        button.Background = new SolidColorBrush(isActive
            ? ColorHelper.FromArgb(0x38, 0x3B, 0x82, 0xF6)
            : Colors.Transparent);
        button.BorderBrush = new SolidColorBrush(isActive
            ? ColorHelper.FromArgb(0x66, 0x60, 0xA5, 0xFA)
            : Colors.Transparent);
        button.BorderThickness = isActive ? new Thickness(1) : new Thickness(1);
        button.Foreground = new SolidColorBrush(isActive
            ? Colors.White
            : ColorHelper.FromArgb(0xF2, 0xFF, 0xFF, 0xFF));
        button.FontWeight = isActive ? Microsoft.UI.Text.FontWeights.SemiBold : Microsoft.UI.Text.FontWeights.Normal;
    }

    private static string BuildInitials(string username)
    {
        var parts = (username ?? string.Empty)
            .Split(new[] { ' ', '.', '_', '-' }, StringSplitOptions.RemoveEmptyEntries)
            .Take(2)
            .Select(p => char.ToUpperInvariant(p[0]))
            .ToArray();

        if (parts.Length == 0)
        {
            return "PM";
        }

        return new string(parts);
    }

    private static string HumanizeUsername(string username)
    {
        if (string.IsNullOrWhiteSpace(username))
        {
            return "Promix User";
        }

        var textInfo = CultureInfo.CurrentCulture.TextInfo;
        return textInfo.ToTitleCase(username.Replace('.', ' ').Replace('_', ' ').Trim());
    }

    private void Dashboard_Click(object sender, RoutedEventArgs e)
        => NavigateRequested?.Invoke(this, new SidebarNavigateEventArgs(SidebarDestination.Dashboard));

    private void Ledger_Click(object sender, RoutedEventArgs e)
        => NavigateRequested?.Invoke(this, new SidebarNavigateEventArgs(SidebarDestination.Ledger));

    private void Currencies_Click(object sender, RoutedEventArgs e)
        => NavigateRequested?.Invoke(this, new SidebarNavigateEventArgs(SidebarDestination.Currencies));

    private void Items_Click(object sender, RoutedEventArgs e)
        => NavigateRequested?.Invoke(this, new SidebarNavigateEventArgs(SidebarDestination.Items));

    private void Settings_Click(object sender, RoutedEventArgs e)
        => NavigateRequested?.Invoke(this, new SidebarNavigateEventArgs(SidebarDestination.Settings));

    private void Logout_Click(object sender, RoutedEventArgs e)
        => LogoutRequested?.Invoke(this, EventArgs.Empty);
}

public enum SidebarDestination
{
    Dashboard,
    Ledger,
    ChartOfAccounts,
    Journals,
    Parties,
    Currencies,
    Items,
    Reports,
    TrialBalance,
    Settings
}

public sealed class SidebarNavigateEventArgs : EventArgs
{
    public SidebarDestination Destination { get; }

    public SidebarNavigateEventArgs(SidebarDestination destination)
        => Destination = destination;
}
