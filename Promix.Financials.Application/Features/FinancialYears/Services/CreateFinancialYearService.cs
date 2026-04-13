using Promix.Financials.Application.Abstractions;
using Promix.Financials.Application.Features.FinancialYears.Commands;
using Promix.Financials.Domain.Accounting;
using Promix.Financials.Domain.Exceptions;

namespace Promix.Financials.Application.Features.FinancialYears.Services;

public sealed class CreateFinancialYearService
{
    private readonly IFinancialYearRepository _financialYears;
    private readonly IFinancialPeriodRepository _financialPeriods;

    public CreateFinancialYearService(IFinancialYearRepository financialYears, IFinancialPeriodRepository financialPeriods)
    {
        _financialYears = financialYears;
        _financialPeriods = financialPeriods;
    }

    public async Task<Guid> CreateAsync(CreateFinancialYearCommand command, CancellationToken ct = default)
    {
        if (await _financialYears.CodeExistsAsync(command.CompanyId, command.Code, null, ct))
            throw new BusinessRuleException("رمز السنة المالية مستخدم مسبقًا داخل هذه الشركة.");

        if (await _financialYears.HasOverlapAsync(command.CompanyId, command.StartDate, command.EndDate, null, ct))
            throw new BusinessRuleException("لا يمكن إنشاء سنة مالية تتداخل مع سنة أخرى في نفس الشركة.");

        var existingYears = await _financialYears.GetByCompanyAsync(command.CompanyId, ct);
        foreach (var existingYear in existingYears.Where(x => x.IsActive))
            existingYear.Deactivate();

        var setActive = command.SetActive || existingYears.Count == 0;
        var financialYear = new FinancialYear(
            command.CompanyId,
            command.Code,
            command.Name,
            command.StartDate,
            command.EndDate,
            setActive);

        await _financialYears.AddAsync(financialYear, ct);
        await _financialYears.SaveChangesAsync(ct);

        var periods = financialYear.BuildMonthlyPeriods();
        await _financialPeriods.AddRangeAsync(periods, ct);
        await _financialPeriods.SaveChangesAsync(ct);
        return financialYear.Id;
    }
}
