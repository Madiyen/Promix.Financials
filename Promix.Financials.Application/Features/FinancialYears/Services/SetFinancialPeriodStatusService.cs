using Promix.Financials.Application.Abstractions;
using Promix.Financials.Domain.Enums;
using Promix.Financials.Domain.Exceptions;

namespace Promix.Financials.Application.Features.FinancialYears.Services;

public sealed class SetFinancialPeriodStatusService
{
    private readonly IFinancialPeriodRepository _periods;

    public SetFinancialPeriodStatusService(IFinancialPeriodRepository periods)
    {
        _periods = periods;
    }

    public async Task SetStatusAsync(Guid companyId, Guid financialPeriodId, FinancialPeriodStatus status, CancellationToken ct = default)
    {
        if (companyId == Guid.Empty)
            throw new BusinessRuleException("CompanyId is required.");

        if (financialPeriodId == Guid.Empty)
            throw new BusinessRuleException("FinancialPeriodId is required.");

        var period = await _periods.GetByIdAsync(companyId, financialPeriodId, ct)
            ?? throw new BusinessRuleException("الفترة المالية غير موجودة.");

        if (status == FinancialPeriodStatus.Closed)
            period.Close();
        else
            period.Reopen();

        await _periods.SaveChangesAsync(ct);
    }
}
