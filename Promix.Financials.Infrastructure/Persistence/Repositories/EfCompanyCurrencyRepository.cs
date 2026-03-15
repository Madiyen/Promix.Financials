using Microsoft.EntityFrameworkCore;
using Promix.Financials.Application.Abstractions;
using Promix.Financials.Domain.Accounting;

namespace Promix.Financials.Infrastructure.Persistence.Repositories;

public sealed class EfCompanyCurrencyRepository : ICompanyCurrencyRepository
{
    private readonly PromixDbContext _db;

    public EfCompanyCurrencyRepository(PromixDbContext db) => _db = db;

    public Task<bool> ExistsAsync(Guid companyId, string currencyCode, CancellationToken ct = default)
    {
        var normalized = currencyCode.Trim().ToUpperInvariant();
        return _db.CompanyCurrencies
            .AnyAsync(x => x.CompanyId == companyId && x.CurrencyCode == normalized, ct);
    }

    public Task<CompanyCurrency?> GetByIdAsync(Guid id, Guid companyId, CancellationToken ct = default)
        => _db.CompanyCurrencies
            .FirstOrDefaultAsync(x => x.Id == id && x.CompanyId == companyId, ct);

    public Task<CompanyCurrency?> GetBaseCurrencyAsync(Guid companyId, CancellationToken ct = default)
        => _db.CompanyCurrencies
            .FirstOrDefaultAsync(x => x.CompanyId == companyId && x.IsBaseCurrency, ct);

    public async Task<IReadOnlyList<CompanyCurrency>> GetAllAsync(Guid companyId, CancellationToken ct = default)
        => await _db.CompanyCurrencies
            .AsNoTracking()
            .Where(x => x.CompanyId == companyId)
            .OrderByDescending(x => x.IsBaseCurrency)
            .ThenBy(x => x.CurrencyCode)
            .ToListAsync(ct);

    public Task AddAsync(CompanyCurrency currency, CancellationToken ct = default)
    {
        _db.CompanyCurrencies.Add(currency);
        return Task.CompletedTask;
    }

    public Task SaveChangesAsync(CancellationToken ct = default)
        => _db.SaveChangesAsync(ct);
}