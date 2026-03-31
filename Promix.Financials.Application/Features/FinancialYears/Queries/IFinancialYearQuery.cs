namespace Promix.Financials.Application.Features.FinancialYears.Queries;

public sealed record FinancialYearListItemDto(
    Guid Id,
    string Code,
    string Name,
    DateOnly StartDate,
    DateOnly EndDate,
    bool IsActive);

public sealed record FinancialYearOptionDto(
    Guid? Id,
    string DisplayText,
    DateOnly StartDate,
    DateOnly EndDate,
    bool IsActive,
    bool IsDerivedFallback);

public interface IFinancialYearQuery
{
    Task<IReadOnlyList<FinancialYearListItemDto>> GetFinancialYearsAsync(Guid companyId, CancellationToken ct = default);
    Task<IReadOnlyList<FinancialYearOptionDto>> GetSelectableYearsAsync(Guid companyId, CancellationToken ct = default);
}
