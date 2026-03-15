namespace Promix.Financials.UI.Dialogs.Currencies;

public sealed class DefaultCurrencyOptionVm
{
    public DefaultCurrencyOptionVm(string code, string nameAr, string? nameEn, string? symbol)
    {
        Code = code;
        NameAr = nameAr;
        NameEn = nameEn;
        Symbol = symbol ?? code;
        DisplayName = $"{code} — {nameAr}";
    }

    public string Code { get; }
    public string NameAr { get; }
    public string? NameEn { get; }
    public string? Symbol { get; }
    public string DisplayName { get; }
}