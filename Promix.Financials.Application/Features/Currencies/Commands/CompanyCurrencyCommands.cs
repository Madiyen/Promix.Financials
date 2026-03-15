namespace Promix.Financials.Application.Features.Currencies.Commands;

public sealed record CreateCompanyCurrencyCommand(
    Guid CompanyId,
    string CurrencyCode,
    string NameAr,
    string? NameEn,
    string? Symbol,
    byte DecimalPlaces,
    decimal ExchangeRate,
    bool IsBaseCurrency
);

public sealed record EditCompanyCurrencyCommand(
    Guid Id,
    Guid CompanyId,
    string NameAr,
    string? NameEn,
    string? Symbol,
    byte DecimalPlaces,
    decimal ExchangeRate
);

public sealed record DeactivateCompanyCurrencyCommand(
    Guid Id,
    Guid CompanyId
);