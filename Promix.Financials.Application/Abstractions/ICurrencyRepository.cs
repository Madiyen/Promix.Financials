namespace Promix.Financials.Application.Abstractions;

public interface ICurrencyRepository
{
    Task<bool> ExistsActiveAsync(string currencyCode, CancellationToken ct = default);
    Task<IReadOnlyList<DefaultCurrencyDto>> GetAllActiveAsync(CancellationToken ct = default); // 🆕
}

public sealed record DefaultCurrencyDto(
    string Code,
    string NameAr,
    string? NameEn,
    string? Symbol,
    byte DecimalPlaces
);