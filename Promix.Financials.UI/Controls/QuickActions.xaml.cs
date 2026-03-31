using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;

namespace Promix.Financials.UI.Controls;

public sealed partial class QuickActions : UserControl
{
    public event EventHandler<QuickActionRequestedEventArgs>? QuickActionRequested;

    public QuickActions()
    {
        InitializeComponent();
    }

    private void ReceiptVoucher_Click(object sender, RoutedEventArgs e)
        => Raise(DashboardQuickAction.CreateReceiptVoucher);

    private void PaymentVoucher_Click(object sender, RoutedEventArgs e)
        => Raise(DashboardQuickAction.CreatePaymentVoucher);

    private void TransferVoucher_Click(object sender, RoutedEventArgs e)
        => Raise(DashboardQuickAction.CreateTransferVoucher);

    private void DailyJournal_Click(object sender, RoutedEventArgs e)
        => Raise(DashboardQuickAction.CreateDailyJournal);

    private void Accounts_Click(object sender, RoutedEventArgs e)
        => Raise(DashboardQuickAction.OpenAccounts);

    private void Reports_Click(object sender, RoutedEventArgs e)
        => Raise(DashboardQuickAction.OpenReports);

    private void Raise(DashboardQuickAction action)
        => QuickActionRequested?.Invoke(this, new QuickActionRequestedEventArgs(action));
}

public enum DashboardQuickAction
{
    CreateReceiptVoucher,
    CreatePaymentVoucher,
    CreateTransferVoucher,
    CreateDailyJournal,
    OpenAccounts,
    OpenReports
}

public sealed class QuickActionRequestedEventArgs : EventArgs
{
    public QuickActionRequestedEventArgs(DashboardQuickAction action)
    {
        Action = action;
    }

    public DashboardQuickAction Action { get; }
}
