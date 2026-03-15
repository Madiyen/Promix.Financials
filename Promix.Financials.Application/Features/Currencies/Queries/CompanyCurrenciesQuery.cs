using Promix.Financials.Application.Abstractions;
using Promix.Financials.Domain.Exceptions;

namespace Promix.Financials.Application.Features.Currencies.Queries;

public sealed record CompanyCurrencyDto(
    Guid Id,
    string CurrencyCode,
    string NameAr,
    string? NameEn,
    string? Symbol,
    byte DecimalPlaces,
    decimal ExchangeRate,
    bool IsBaseCurrency,
    bool IsActive
);

public sealed class CompanyCurrenciesQuery
{
    private readonly ICompanyCurrencyRepository _currencies;

    public CompanyCurrenciesQuery(ICompanyCurrencyRepository currencies)
    {
        _currencies = currencies;
    }

    public async Task<IReadOnlyList<CompanyCurrencyDto>> GetAllAsync(Guid companyId, CancellationToken ct = default)
    {
        if (companyId == Guid.Empty)
            throw new BusinessRuleException("CompanyId is required.");

        var list = await _currencies.GetAllAsync(companyId, ct);

        return list.Select(c => new CompanyCurrencyDto(
            Id: c.Id,
            CurrencyCode: c.CurrencyCode,
            NameAr: c.NameAr,
            NameEn: c.NameEn,
            Symbol: c.Symbol,
            DecimalPlaces: c.DecimalPlaces,
            ExchangeRate: c.ExchangeRate,
            IsBaseCurrency: c.IsBaseCurrency,
            IsActive: c.IsActive
        )).ToList();
    }
}