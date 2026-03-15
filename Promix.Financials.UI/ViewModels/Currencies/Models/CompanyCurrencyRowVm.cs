using System;

namespace Promix.Financials.UI.ViewModels.Currencies.Models;

public sealed class CompanyCurrencyRowVm
{
    public CompanyCurrencyRowVm(
        Guid id,
        string currencyCode,
        string nameAr,
        string? nameEn,
        string? symbol,
        byte decimalPlaces,
        decimal exchangeRate,
        bool isBaseCurrency,
        bool isActive)
    {
        Id = id;
        CurrencyCode = currencyCode;
        NameAr = nameAr;
        NameEn = nameEn;
        Symbol = symbol ?? currencyCode;
        DecimalPlaces = decimalPlaces;
        ExchangeRate = exchangeRate;
        IsBaseCurrency = isBaseCurrency;
        IsActive = isActive;
    }

    public Guid Id { get; }
    public string CurrencyCode { get; }
    public string NameAr { get; }
    public string? NameEn { get; }
    public string Symbol { get; }
    public byte DecimalPlaces { get; }
    public decimal ExchangeRate { get; }
    public bool IsBaseCurrency { get; }
    public bool IsActive { get; }

    public string StatusText => IsActive ? "فعّال" : "موقوف";
    public string BaseCurrencyText => IsBaseCurrency ? "✔ رئيسية" : "—";
    public string ExchangeRateText => ExchangeRate.ToString("F4");
    public string DisplayName => $"{CurrencyCode} — {NameAr}";
}