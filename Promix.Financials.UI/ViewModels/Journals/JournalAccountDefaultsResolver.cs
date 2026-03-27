using System;
using System.Collections.Generic;
using System.Linq;
using Promix.Financials.Domain.Enums;
using Promix.Financials.UI.ViewModels.Journals.Models;

namespace Promix.Financials.UI.ViewModels.Journals;

internal static class JournalAccountDefaultsResolver
{
    private static readonly string[] PrimaryCashKeywords =
    [
        "الصندوق الرئيسي",
        "الصندوق",
        "الأموال الجاهزة",
        "الخزنة الرئيسية",
        "الخزينة الرئيسية",
        "الخزنة",
        "الخزينة"
    ];

    private static readonly string[] MainTreasuryKeywords =
    [
        "الأموال الجاهزة",
        "الخزنة الرئيسية",
        "الخزينة الرئيسية"
    ];

    public static Guid? ResolvePreferredCashAccount(IEnumerable<JournalAccountOptionVm> accounts)
        => ResolveByKeywords(accounts, PrimaryCashKeywords);

    public static Guid? ResolveMainTreasuryAccount(IEnumerable<JournalAccountOptionVm> accounts)
        => ResolveByKeywords(accounts, MainTreasuryKeywords) ?? ResolvePreferredCashAccount(accounts);

    public static Guid? ResolvePreferredCounterpartyAccount(IEnumerable<JournalAccountOptionVm> accounts, JournalEntryType type)
    {
        var candidates = accounts.Where(x => !x.IsCashLike).ToList();
        if (candidates.Count == 0)
            return null;

        IEnumerable<string> keywords = type == JournalEntryType.ReceiptVoucher
            ? ["عميل", "زبون", "مدين", "إيراد", "مبيعات"]
            : ["مورد", "دائن", "مصروف", "أجور", "رواتب", "سلفة"];

        foreach (var keyword in keywords)
        {
            var exact = candidates.FirstOrDefault(x => x.NameAr.Equals(keyword, StringComparison.CurrentCultureIgnoreCase));
            if (exact is not null)
                return exact.Id;
        }

        foreach (var keyword in keywords)
        {
            var partial = candidates.FirstOrDefault(x => x.NameAr.Contains(keyword, StringComparison.CurrentCultureIgnoreCase));
            if (partial is not null)
                return partial.Id;
        }

        return candidates.FirstOrDefault()?.Id;
    }

    public static Guid? ResolveExistingAccount(IEnumerable<JournalAccountOptionVm> accounts, Guid? accountId)
    {
        if (accountId is not Guid id || id == Guid.Empty)
            return null;

        return accounts.Any(x => x.Id == id) ? id : null;
    }

    private static Guid? ResolveByKeywords(IEnumerable<JournalAccountOptionVm> accounts, IReadOnlyList<string> keywords)
    {
        var ordered = accounts.ToList();

        foreach (var keyword in keywords)
        {
            var exact = ordered.FirstOrDefault(x => x.NameAr.Equals(keyword, StringComparison.CurrentCultureIgnoreCase));
            if (exact is not null)
                return exact.Id;
        }

        foreach (var keyword in keywords)
        {
            var partial = ordered.FirstOrDefault(x => x.NameAr.Contains(keyword, StringComparison.CurrentCultureIgnoreCase));
            if (partial is not null)
                return partial.Id;
        }

        return ordered.FirstOrDefault()?.Id;
    }
}
