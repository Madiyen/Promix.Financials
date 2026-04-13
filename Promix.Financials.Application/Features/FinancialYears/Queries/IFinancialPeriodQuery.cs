using Promix.Financials.Domain.Enums;

namespace Promix.Financials.Application.Features.FinancialYears.Queries;

public sealed record FinancialPeriodListItemDto(
    Guid Id,
    string Code,
    string Name,
    DateOnly StartDate,
    DateOnly EndDate,
    FinancialPeriodStatus Status,
    bool IsAdjustmentPeriod,
    int EntryCount);

public interface IFinancialPeriodQuery
{
    Task<IReadOnlyList<FinancialPeriodListItemDto>> GetFinancialPeriodsAsync(Guid companyId, Guid financialYearId, CancellationToken ct = default);
}
