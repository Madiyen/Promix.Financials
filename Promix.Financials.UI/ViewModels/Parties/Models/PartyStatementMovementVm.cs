using System;

namespace Promix.Financials.UI.ViewModels.Parties.Models;

public sealed class PartyStatementMovementVm
{
    public PartyStatementMovementVm(
        string entryNumber,
        DateOnly entryDate,
        string accountName,
        decimal debit,
        decimal credit,
        decimal runningBalance,
        string? description)
    {
        EntryNumber = entryNumber;
        EntryDate = entryDate;
        AccountName = accountName;
        Debit = debit;
        Credit = credit;
        RunningBalance = runningBalance;
        Description = description;
    }

    public string EntryNumber { get; }
    public DateOnly EntryDate { get; }
    public string AccountName { get; }
    public decimal Debit { get; }
    public decimal Credit { get; }
    public decimal RunningBalance { get; }
    public string? Description { get; }

    public string EntryDateText => EntryDate.ToString("yyyy-MM-dd");
    public string DebitText => Debit.ToString("N2");
    public string CreditText => Credit.ToString("N2");
    public string RunningBalanceText => RunningBalance.ToString("N2");
}
