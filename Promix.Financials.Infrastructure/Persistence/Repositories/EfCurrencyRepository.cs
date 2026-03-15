using Microsoft.EntityFrameworkCore;
using Promix.Financials.Application.Abstractions;

namespace Promix.Financials.Infrastructure.Persistence.Repositories;

public sealed class EfCurrencyRepository : ICurrencyRepository
{
    private readonly PromixDbContext _db;

    public EfCurrencyRepository(PromixDbContext db)
    {
        _db = db;
    }

    // ✅ مُصحَّح — Id هو الكود في DefaultCurrency
    public Task<bool> ExistsActiveAsync(string currencyCode, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(currencyCode))
            return Task.FromResult(false);

        var normalized = currencyCode.Trim().ToUpperInvariant();

        return _db.Currencies.AnyAsync(x => x.Id == normalized && x.IsActive, ct); // ✅ Id = Code
    }

    // 🆕 جلب كل العملات الفعّالة
    public async Task<IReadOnlyList<DefaultCurrencyDto>> GetAllActiveAsync(CancellationToken ct = default)
    {
        return await _db.Currencies
            .Where(x => x.IsActive)
            .OrderBy(x => x.DisplayOrder)
            .ThenBy(x => x.Id)
            .Select(x => new DefaultCurrencyDto(
                x.Id,       // Id هو الكود
                x.NameAr,
                x.NameEn,
                x.Symbol,
                x.DecimalPlaces))
            .ToListAsync(ct);
    }
}