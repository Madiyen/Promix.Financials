using System;
using System.Linq;
using Promix.Financials.Domain.Enums;

namespace Promix.Financials.UI.ViewModels.Journals.Models;

public sealed class JournalAccountOptionVm
{
    private static readonly string[] CashKeywords =
    [
        "صندوق",
        "خزنة",
        "خزينة",
        "الأموال الجاهزة",
        "مصرف",
        "بنك"
    ];

    public JournalAccountOptionVm(Guid id, string code, string nameAr, AccountNature nature, string? systemRole)
    {
        Id = id;
        Code = code;
        NameAr = nameAr;
        Nature = nature;
        SystemRole = systemRole;
    }

    public Guid Id { get; set; }
    public string Code { get; set; }
    public string NameAr { get; set; }
    public AccountNature Nature { get; set; }
    public string? SystemRole { get; set; }

    public string DisplayText => $"{Code} - {NameAr}";
    public string SearchText => $"{Code} {NameAr} {SystemRole}";
    public bool IsCashLike => IsCashAccount(Code, NameAr, SystemRole);
    public string CategoryText => IsCashLike ? "نقدي" : "تشغيلي";
    public string NatureText => Nature == AccountNature.Debit ? "طبيعته مدينة" : "طبيعته دائنة";

    public bool Matches(string? query)
        => GetMatchRank(query) < int.MaxValue;

    public int GetMatchRank(string? query)
    {
        if (string.IsNullOrWhiteSpace(query))
            return 500;

        var normalized = query.Trim();
        if (string.Equals(Code, normalized, StringComparison.OrdinalIgnoreCase))
            return 0;

        if (Code.StartsWith(normalized, StringComparison.OrdinalIgnoreCase))
            return 1;

        if (string.Equals(NameAr, normalized, StringComparison.CurrentCultureIgnoreCase))
            return 2;

        if (NameAr.StartsWith(normalized, StringComparison.CurrentCultureIgnoreCase))
            return 3;

        if (DisplayText.Contains(normalized, StringComparison.CurrentCultureIgnoreCase))
            return 4;

        if (SearchText.Contains(normalized, StringComparison.CurrentCultureIgnoreCase))
            return 5;

        return int.MaxValue;
    }

    public override string ToString() => DisplayText;

    private static bool IsCashAccount(string code, string nameAr, string? systemRole)
    {
        if (!string.IsNullOrWhiteSpace(systemRole) &&
            (systemRole.Contains("cash", StringComparison.OrdinalIgnoreCase)
            || systemRole.Contains("bank", StringComparison.OrdinalIgnoreCase)
            || systemRole.Contains("treasury", StringComparison.OrdinalIgnoreCase)))
        {
            return true;
        }

        if (code.StartsWith("13", StringComparison.OrdinalIgnoreCase))
            return true;

        return CashKeywords.Any(keyword => nameAr.Contains(keyword, StringComparison.CurrentCultureIgnoreCase));
    }
}
