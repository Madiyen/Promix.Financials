using System;

namespace Promix.Financials.UI.ViewModels.Parties.Models;

public sealed class PartySettlementRowVm
{
    public PartySettlementRowVm(DateOnly settledOn, string debitEntryNumber, string creditEntryNumber, decimal amount)
    {
        SettledOn = settledOn;
        DebitEntryNumber = debitEntryNumber;
        CreditEntryNumber = creditEntryNumber;
        Amount = amount;
    }

    public DateOnly SettledOn { get; }
    public string DebitEntryNumber { get; }
    public string CreditEntryNumber { get; }
    public decimal Amount { get; }

    public string SettledOnText => SettledOn.ToString("yyyy-MM-dd");
    public string AmountText => Amount.ToString("N2");
}
