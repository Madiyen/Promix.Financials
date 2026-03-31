using System;

namespace Promix.Financials.UI.ViewModels.Parties.Models;

public sealed class PartyOpenItemRowVm
{
    public PartyOpenItemRowVm(
        string entryNumber,
        DateOnly entryDate,
        string accountName,
        decimal openAmount,
        int ageDays,
        string sideText,
        string? description)
    {
        EntryNumber = entryNumber;
        EntryDate = entryDate;
        AccountName = accountName;
        OpenAmount = openAmount;
        AgeDays = ageDays;
        SideText = sideText;
        Description = description;
    }

    public string EntryNumber { get; }
    public DateOnly EntryDate { get; }
    public string AccountName { get; }
    public decimal OpenAmount { get; }
    public int AgeDays { get; }
    public string SideText { get; }
    public string? Description { get; }

    public string EntryDateText => EntryDate.ToString("yyyy-MM-dd");
    public string OpenAmountText => OpenAmount.ToString("N2");
}
