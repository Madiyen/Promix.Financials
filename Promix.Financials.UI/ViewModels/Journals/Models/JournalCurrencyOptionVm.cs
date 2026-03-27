namespace Promix.Financials.UI.ViewModels.Journals.Models;

public sealed class JournalCurrencyOptionVm
{
    public JournalCurrencyOptionVm(
        string currencyCode,
        string nameAr,
        string? nameEn,
        string symbol,
        byte decimalPlaces,
        decimal exchangeRate,
        bool isBaseCurrency)
    {
        CurrencyCode = currencyCode;
        NameAr = nameAr;
        NameEn = nameEn;
        Symbol = string.IsNullOrWhiteSpace(symbol) ? currencyCode : symbol;
        DecimalPlaces = decimalPlaces;
        ExchangeRate = exchangeRate;
        IsBaseCurrency = isBaseCurrency;
    }

    public string CurrencyCode { get; }
    public string NameAr { get; }
    public string? NameEn { get; }
    public string Symbol { get; }
    public byte DecimalPlaces { get; }
    public decimal ExchangeRate { get; }
    public bool IsBaseCurrency { get; }

    public string DisplayText => $"{CurrencyCode} - {NameAr}";
}
