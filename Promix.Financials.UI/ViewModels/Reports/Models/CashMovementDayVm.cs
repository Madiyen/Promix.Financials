using System;
using Microsoft.UI.Xaml.Media;
using Promix.Financials.UI.ViewModels.Journals.Models;

namespace Promix.Financials.UI.ViewModels.Reports.Models;

public sealed class CashMovementDayVm
{
    public CashMovementDayVm(DateOnly entryDate, decimal netMovement)
    {
        EntryDate = entryDate;
        NetMovement = netMovement;
    }

    public DateOnly EntryDate { get; }
    public decimal NetMovement { get; }

    public string FullDateText => EntryDate.ToString("yyyy-MM-dd");
    public string DirectionText => NetMovement switch
    {
        > 0m => "تدفق داخل",
        < 0m => "تدفق خارج",
        _ => "بدون صافي"
    };

    public string NetMovementText => NetMovement switch
    {
        > 0m => $"+{NetMovement:N2}",
        < 0m => $"-{Math.Abs(NetMovement):N2}",
        _ => "0.00"
    };

    public Brush AccentBrush => NetMovement switch
    {
        > 0m => JournalActivityBarVm.FromHex("#059669"),
        < 0m => JournalActivityBarVm.FromHex("#DC2626"),
        _ => JournalActivityBarVm.FromHex("#64748B")
    };

    public Brush AccentBackgroundBrush => NetMovement switch
    {
        > 0m => JournalActivityBarVm.FromHex("#ECFDF5"),
        < 0m => JournalActivityBarVm.FromHex("#FEF2F2"),
        _ => JournalActivityBarVm.FromHex("#F8FAFC")
    };
}
