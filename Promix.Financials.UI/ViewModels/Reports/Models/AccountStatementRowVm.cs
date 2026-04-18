using System;
using Microsoft.UI.Xaml.Media;
using Promix.Financials.UI.ViewModels.Journals.Models;

namespace Promix.Financials.UI.ViewModels.Reports.Models;

public sealed class AccountStatementRowVm
{
    public AccountStatementRowVm(
        Guid entryId,
        string entryNumber,
        DateOnly entryDate,
        string typeText,
        string? referenceNo,
        string? description,
        decimal debit,
        decimal credit,
        string runningBalanceText,
        bool isOpeningBalanceRow = false)
    {
        EntryId = entryId;
        EntryNumber = entryNumber;
        EntryDate = entryDate;
        TypeText = typeText;
        ReferenceNo = referenceNo;
        Description = description;
        Debit = debit;
        Credit = credit;
        RunningBalanceText = runningBalanceText;
        IsOpeningBalanceRow = isOpeningBalanceRow;
    }

    public Guid EntryId { get; }
    public string EntryNumber { get; }
    public DateOnly EntryDate { get; }
    public string TypeText { get; }
    public string? ReferenceNo { get; }
    public string? Description { get; }
    public decimal Debit { get; }
    public decimal Credit { get; }
    public string RunningBalanceText { get; }
    public bool IsOpeningBalanceRow { get; }

    public string EntryDateText => IsOpeningBalanceRow ? "—" : EntryDate.ToString("yyyy-MM-dd");
    public string EntryNumberDisplay => string.IsNullOrWhiteSpace(EntryNumber) ? "—" : EntryNumber;
    public string ReferenceDisplay => string.IsNullOrWhiteSpace(ReferenceNo) ? "—" : ReferenceNo!;
    public string DescriptionDisplay => string.IsNullOrWhiteSpace(Description) ? "بدون بيان" : Description!;
    public string DebitText => Debit == 0m ? "—" : Debit.ToString("N2");
    public string CreditText => Credit == 0m ? "—" : Credit.ToString("N2");
    public Brush RowBackgroundBrush => IsOpeningBalanceRow
        ? JournalActivityBarVm.FromHex("#F8FAFC")
        : JournalActivityBarVm.FromHex("#FFFFFF");
    public Brush TypeBackgroundBrush => IsOpeningBalanceRow
        ? JournalActivityBarVm.FromHex("#E0F2FE")
        : JournalActivityBarVm.FromHex("#EEF2FF");
    public Brush TypeForegroundBrush => IsOpeningBalanceRow
        ? JournalActivityBarVm.FromHex("#0369A1")
        : JournalActivityBarVm.FromHex("#1D4ED8");
    public Brush RunningBalanceBrush => IsOpeningBalanceRow
        ? JournalActivityBarVm.FromHex("#0369A1")
        : JournalActivityBarVm.FromHex("#0F172A");
}
