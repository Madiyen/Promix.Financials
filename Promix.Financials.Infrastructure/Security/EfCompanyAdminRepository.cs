using Microsoft.EntityFrameworkCore;
using Promix.Financials.Application.Abstractions;
using Promix.Financials.Domain.Security;
using Promix.Financials.Infrastructure.Persistence;

namespace Promix.Financials.Infrastructure.Security;

public sealed class EfCompanyAdminRepository : ICompanyAdminRepository
{
    private readonly PromixDbContext _db;
    public async Task<string> GenerateNextCompanyCodeAsync(CancellationToken ct = default)
    {
        var existingCodes = await _db.Companies
            .AsNoTracking()
            .Where(c => c.Code.StartsWith("CMP"))
            .Select(c => c.Code)
            .ToListAsync(ct);

        var maxNumber = 0;

        foreach (var code in existingCodes)
        {
            if (code.Length == 7 && int.TryParse(code.Substring(3), out var number))
            {
                if (number > maxNumber)
                    maxNumber = number;
            }
        }

        return $"CMP{(maxNumber + 1):D4}";
    }
    public EfCompanyAdminRepository(PromixDbContext db)
    {
        _db = db;
    }

    public Task<bool> CompanyCodeExistsAsync(string code, CancellationToken ct = default)
    {
        var normalized = code.Trim();
        return _db.Companies.AnyAsync(c => c.Code == normalized, ct);
    }

    public async Task<Company> CreateCompanyAsync(
        string code,
        string name,
        string baseCurrency,
        DateOnly accountingStartDate,
        Guid ownerUserId,
        CancellationToken ct = default)
    {
        var company = new Company(code, name, baseCurrency, accountingStartDate);

        _db.Companies.Add(company);
        _db.UserCompanies.Add(new UserCompany(ownerUserId, company.Id));

        await _db.SaveChangesAsync(ct);
        return company;
    }

    public async Task ResetApplicationDataAsync(CancellationToken ct = default)
    {
        await using var transaction = await _db.Database.BeginTransactionAsync(ct);

        await _db.PartySettlements.ExecuteDeleteAsync(ct);
        await _db.JournalLines.ExecuteDeleteAsync(ct);
        await _db.JournalEntries.ExecuteDeleteAsync(ct);
        await _db.Parties.ExecuteDeleteAsync(ct);
        await _db.Accounts.ExecuteDeleteAsync(ct);
        await _db.CompanyCurrencies.ExecuteDeleteAsync(ct);
        await _db.CurrencyRates.ExecuteDeleteAsync(ct);
        await _db.UserCompanies.ExecuteDeleteAsync(ct);
        await _db.Companies.ExecuteDeleteAsync(ct);

        await transaction.CommitAsync(ct);
    }
}
