namespace Promix.Financials.UI.ViewModels.Journals.Models;

public sealed class JournalEntryLineDetailVm
{
    public JournalEntryLineDetailVm(string accountName, decimal debit, decimal credit, string description)
    {
        AccountName = accountName;
        Debit = debit;
        Credit = credit;
        Description = description;
    }

    public string AccountName { get; }
    public decimal Debit { get; }
    public decimal Credit { get; }
    public string Description { get; }

    public string DebitText => Debit > 0 ? Debit.ToString("N2") : "—";
    public string CreditText => Credit > 0 ? Credit.ToString("N2") : "—";
    public string DescriptionText => string.IsNullOrWhiteSpace(Description) ? "—" : Description;
}
