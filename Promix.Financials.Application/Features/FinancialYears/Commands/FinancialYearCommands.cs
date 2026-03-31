namespace Promix.Financials.Application.Features.FinancialYears.Commands;

public sealed record CreateFinancialYearCommand(
    Guid CompanyId,
    string Code,
    string Name,
    DateOnly StartDate,
    DateOnly EndDate,
    bool SetActive);

public sealed record EditFinancialYearCommand(
    Guid CompanyId,
    Guid FinancialYearId,
    string Code,
    string Name,
    DateOnly StartDate,
    DateOnly EndDate);

public sealed record ActivateFinancialYearCommand(
    Guid CompanyId,
    Guid FinancialYearId);
