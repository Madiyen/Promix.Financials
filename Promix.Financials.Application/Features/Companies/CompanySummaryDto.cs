using System;

namespace Promix.Financials.Application.Features.Companies;

public sealed record CompanySummaryDto(
    Guid Id,
    string ArabicName,
    string? EnglishName,
    string BaseCurrency,
    DateOnly AccountingStartDate
)
{
    public string DisplayName =>
        !string.IsNullOrWhiteSpace(EnglishName)
            ? EnglishName!
            : ArabicName;

    public string AccountingStartDateText => AccountingStartDate.ToString("yyyy-MM-dd");
}
