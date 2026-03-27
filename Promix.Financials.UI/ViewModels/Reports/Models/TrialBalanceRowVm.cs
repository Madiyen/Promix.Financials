using System;
using Microsoft.UI.Xaml.Media;
using Promix.Financials.Domain.Enums;
using Promix.Financials.UI.ViewModels.Journals.Models;

namespace Promix.Financials.UI.ViewModels.Reports.Models;

public sealed class TrialBalanceRowVm
{
    public TrialBalanceRowVm(
        Guid accountId,
        string code,
        string nameAr,
        AccountNature nature,
        decimal openingDebit,
        decimal openingCredit,
        decimal periodDebit,
        decimal periodCredit,
        decimal closingDebit,
        decimal closingCredit)
    {
        AccountId = accountId;
        Code = code;
        NameAr = nameAr;
        Nature = nature;
        OpeningDebit = openingDebit;
        OpeningCredit = openingCredit;
        PeriodDebit = periodDebit;
        PeriodCredit = periodCredit;
        ClosingDebit = closingDebit;
        ClosingCredit = closingCredit;
    }

    public Guid AccountId { get; }
    public string Code { get; }
    public string NameAr { get; }
    public AccountNature Nature { get; }
    public decimal OpeningDebit { get; }
    public decimal OpeningCredit { get; }
    public decimal PeriodDebit { get; }
    public decimal PeriodCredit { get; }
    public decimal ClosingDebit { get; }
    public decimal ClosingCredit { get; }

    public string AccountDisplayText => $"{Code} - {NameAr}";
    public string NatureText => Nature == AccountNature.Debit ? "مدينة" : "دائنة";
    public string OpeningDebitText => FormatAmount(OpeningDebit);
    public string OpeningCreditText => FormatAmount(OpeningCredit);
    public string PeriodDebitText => FormatAmount(PeriodDebit);
    public string PeriodCreditText => FormatAmount(PeriodCredit);
    public string ClosingDebitText => FormatAmount(ClosingDebit);
    public string ClosingCreditText => FormatAmount(ClosingCredit);
    public string ClosingSideText => ClosingDebit > 0m ? "مدين" : ClosingCredit > 0m ? "دائن" : "صفري";
    public Brush ClosingAccentBrush => ClosingDebit > 0m
        ? JournalActivityBarVm.FromHex("#1D4ED8")
        : ClosingCredit > 0m
            ? JournalActivityBarVm.FromHex("#0369A1")
            : JournalActivityBarVm.FromHex("#64748B");

    private static string FormatAmount(decimal amount) => amount == 0m ? "—" : amount.ToString("N2");
}
