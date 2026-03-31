namespace Promix.Financials.UI.ViewModels.Parties.Models;

public sealed class PartyAgingBucketVm
{
    public PartyAgingBucketVm(string label, decimal receivableAmount, decimal payableAmount)
    {
        Label = label;
        ReceivableAmount = receivableAmount;
        PayableAmount = payableAmount;
    }

    public string Label { get; }
    public decimal ReceivableAmount { get; }
    public decimal PayableAmount { get; }
    public string ReceivableAmountText => ReceivableAmount.ToString("N2");
    public string PayableAmountText => PayableAmount.ToString("N2");
}
