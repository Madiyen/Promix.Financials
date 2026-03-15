using Promix.Financials.Domain.Common;
using Promix.Financials.Domain.Exceptions;

namespace Promix.Financials.Domain.Accounting;

public sealed class CompanyCurrency : Entity<Guid>
{
    private CompanyCurrency() { } // ← EF Core

    public CompanyCurrency(
        Guid companyId,
        string currencyCode,
        string nameAr,
        string? nameEn,
        string? symbol,
        byte decimalPlaces,
        decimal exchangeRate,
        bool isBaseCurrency)
    {
        if (companyId == Guid.Empty)
            throw new BusinessRuleException("CompanyId is required.");

        if (string.IsNullOrWhiteSpace(currencyCode))
            throw new BusinessRuleException("Currency code is required.");

        if (string.IsNullOrWhiteSpace(nameAr))
            throw new BusinessRuleException("Arabic currency name is required.");

        if (decimalPlaces > 6)
            throw new BusinessRuleException("Decimal places must be between 0 and 6.");

        if (exchangeRate <= 0)
            throw new BusinessRuleException("Exchange rate must be greater than zero.");

        if (isBaseCurrency && exchangeRate != 1)
            throw new BusinessRuleException("Base currency exchange rate must be 1.");

        Id = Guid.NewGuid();
        CompanyId = companyId;
        CurrencyCode = currencyCode.Trim().ToUpperInvariant();
        NameAr = nameAr.Trim();
        NameEn = string.IsNullOrWhiteSpace(nameEn) ? null : nameEn.Trim();
        Symbol = string.IsNullOrWhiteSpace(symbol) ? CurrencyCode : symbol.Trim();
        DecimalPlaces = decimalPlaces;
        ExchangeRate = exchangeRate;
        IsBaseCurrency = isBaseCurrency;
        IsActive = true;
    }

    // ─── Properties ───────────────────────────────────────────────
    public Guid CompanyId { get; private set; }
    public string CurrencyCode { get; private set; } = default!; // لا يتغير بعد الإنشاء
    public string NameAr { get; private set; } = default!;
    public string? NameEn { get; private set; }
    public string Symbol { get; private set; } = default!;
    public byte DecimalPlaces { get; private set; }
    public decimal ExchangeRate { get; private set; }
    public bool IsBaseCurrency { get; private set; }
    public bool IsActive { get; private set; }

    // ─── Domain Methods ───────────────────────────────────────────

    /// <summary>
    /// تعديل البيانات التوصيفية فقط — الكود لا يتغير أبداً
    /// </summary>
    public void Update(string nameAr, string? nameEn, string? symbol, byte decimalPlaces)
    {
        if (string.IsNullOrWhiteSpace(nameAr))
            throw new BusinessRuleException("Arabic currency name is required.");

        if (decimalPlaces > 6)
            throw new BusinessRuleException("Decimal places must be between 0 and 6.");

        NameAr = nameAr.Trim();
        NameEn = string.IsNullOrWhiteSpace(nameEn) ? null : nameEn.Trim();
        Symbol = string.IsNullOrWhiteSpace(symbol) ? CurrencyCode : symbol.Trim();
        DecimalPlaces = decimalPlaces;
    }

    /// <summary>
    /// تحديث سعر التعادل — ممنوع على العملة الرئيسية
    /// </summary>
    public void UpdateRate(decimal newRate)
    {
        if (IsBaseCurrency)
            throw new BusinessRuleException("Cannot change exchange rate of base currency.");

        if (newRate <= 0)
            throw new BusinessRuleException("Exchange rate must be greater than zero.");

        ExchangeRate = newRate;
    }

    /// <summary>
    /// إيقاف العملة — ممنوع على العملة الرئيسية
    /// </summary>
    public void Deactivate()
    {
        if (IsBaseCurrency)
            throw new BusinessRuleException("Cannot deactivate base currency.");

        IsActive = false;
    }

    /// <summary>
    /// تفعيل ��لعملة
    /// </summary>
    public void Activate()
        => IsActive = true;
}