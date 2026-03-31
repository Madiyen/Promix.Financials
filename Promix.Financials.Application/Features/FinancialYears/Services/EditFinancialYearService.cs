using Promix.Financials.Application.Abstractions;
using Promix.Financials.Application.Features.FinancialYears.Commands;
using Promix.Financials.Domain.Exceptions;

namespace Promix.Financials.Application.Features.FinancialYears.Services;

public sealed class EditFinancialYearService
{
    private readonly IFinancialYearRepository _financialYears;

    public EditFinancialYearService(IFinancialYearRepository financialYears)
    {
        _financialYears = financialYears;
    }

    public async Task EditAsync(EditFinancialYearCommand command, CancellationToken ct = default)
    {
        var financialYear = await _financialYears.GetByIdAsync(command.CompanyId, command.FinancialYearId, ct)
            ?? throw new BusinessRuleException("السنة المالية غير موجودة.");

        if (await _financialYears.CodeExistsAsync(command.CompanyId, command.Code, command.FinancialYearId, ct))
            throw new BusinessRuleException("رمز السنة المالية مستخدم مسبقًا داخل هذه الشركة.");

        if (await _financialYears.HasOverlapAsync(command.CompanyId, command.StartDate, command.EndDate, command.FinancialYearId, ct))
            throw new BusinessRuleException("لا يمكن تعديل السنة المالية بطريقة تجعلها تتداخل مع سنة أخرى.");

        financialYear.Update(command.Code, command.Name, command.StartDate, command.EndDate);
        await _financialYears.SaveChangesAsync(ct);
    }
}
