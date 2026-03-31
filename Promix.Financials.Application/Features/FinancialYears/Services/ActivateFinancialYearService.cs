using Promix.Financials.Application.Abstractions;
using Promix.Financials.Application.Features.FinancialYears.Commands;
using Promix.Financials.Domain.Exceptions;

namespace Promix.Financials.Application.Features.FinancialYears.Services;

public sealed class ActivateFinancialYearService
{
    private readonly IFinancialYearRepository _financialYears;

    public ActivateFinancialYearService(IFinancialYearRepository financialYears)
    {
        _financialYears = financialYears;
    }

    public async Task ActivateAsync(ActivateFinancialYearCommand command, CancellationToken ct = default)
    {
        var financialYear = await _financialYears.GetByIdAsync(command.CompanyId, command.FinancialYearId, ct)
            ?? throw new BusinessRuleException("السنة المالية غير موجودة.");

        var existingYears = await _financialYears.GetByCompanyAsync(command.CompanyId, ct);
        foreach (var existingYear in existingYears)
        {
            if (existingYear.Id == financialYear.Id)
                existingYear.Activate();
            else if (existingYear.IsActive)
                existingYear.Deactivate();
        }

        await _financialYears.SaveChangesAsync(ct);
    }
}
