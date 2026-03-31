using System;

namespace Promix.Financials.UI.ViewModels.Journals.Models;

public sealed class PartyOpenItemPreviewVm
{
    public PartyOpenItemPreviewVm(
        string entryNumber,
        DateOnly entryDate,
        string accountName,
        decimal openAmount,
        decimal suggestedAmount,
        string? description)
    {
        EntryNumber = entryNumber;
        EntryDate = entryDate;
        AccountName = accountName;
        OpenAmount = openAmount;
        SuggestedAmount = suggestedAmount;
        Description = description;
    }

    public string EntryNumber { get; }
    public DateOnly EntryDate { get; }
    public string AccountName { get; }
    public decimal OpenAmount { get; }
    public decimal SuggestedAmount { get; }
    public string? Description { get; }
    public string EntryDateText => EntryDate.ToString("yyyy-MM-dd");
    public string OpenAmountText => OpenAmount.ToString("N2");
    public string SuggestedAmountText => SuggestedAmount.ToString("N2");
}
