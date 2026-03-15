using Promix.Financials.Application.Abstractions;
using Promix.Financials.Application.Features.Currencies.Commands;
using Promix.Financials.Domain.Accounting;
using Promix.Financials.Domain.Exceptions;

namespace Promix.Financials.Application.Features.Currencies.Services;

public sealed class CreateCompanyCurrencyService
{
    private readonly ICompanyCurrencyRepository _currencies;
    private readonly ICurrencyRepository _defaultCurrencies;

    public CreateCompanyCurrencyService(
        ICompanyCurrencyRepository currencies,
        ICurrencyRepository defaultCurrencies)
    {
        _currencies = currencies;
        _defaultCurrencies = defaultCurrencies;
    }

    public async Task<Guid> CreateAsync(CreateCompanyCurrencyCommand cmd, CancellationToken ct = default)
    {
        if (cmd.CompanyId == Guid.Empty)
            throw new BusinessRuleException("CompanyId is required.");

        var currencyCode = cmd.CurrencyCode.Trim().ToUpperInvariant();

        if (string.IsNullOrWhiteSpace(currencyCode))
            throw new BusinessRuleException("Currency code is required.");

        var nameAr = cmd.NameAr.Trim();
        if (string.IsNullOrWhiteSpace(nameAr))
            throw new BusinessRuleException("Arabic name is required.");

        if (cmd.ExchangeRate <= 0)
            throw new BusinessRuleException("Exchange rate must be greater than zero.");

        // لا تكرار داخل نفس الشركة
        if (await _currencies.ExistsAsync(cmd.CompanyId, currencyCode, ct))
            throw new BusinessRuleException("Currency already exists for this company.");

        // العملة الرئيسية يجب أن يكون سعرها 1
        if (cmd.IsBaseCurrency && cmd.ExchangeRate != 1m)
            throw new BusinessRuleException("Base currency exchange rate must be 1.");

        // إذا طلب إضافة عملة رئيسية تحقق أنه لا توجد واحدة مسبقاً
        if (cmd.IsBaseCurrency)
        {
            var existing = await _currencies.GetBaseCurrencyAsync(cmd.CompanyId, ct);
            if (existing is not null)
                throw new BusinessRuleException("Company already has a base currency.");
        }

        // تحقق أن العملة موجودة في قائمة العملات الافتراضية
        if (!await _defaultCurrencies.ExistsActiveAsync(currencyCode, ct))
            throw new BusinessRuleException("Currency code is invalid or inactive.");

        var currency = new CompanyCurrency(
            companyId: cmd.CompanyId,
            currencyCode: currencyCode,
            nameAr: nameAr,
            nameEn: cmd.NameEn?.Trim(),
            symbol: cmd.Symbol?.Trim(),
            decimalPlaces: cmd.DecimalPlaces,
            exchangeRate: cmd.ExchangeRate,
            isBaseCurrency: cmd.IsBaseCurrency
        );

        await _currencies.AddAsync(currency, ct);
        await _currencies.SaveChangesAsync(ct);

        return currency.Id;
    }
}