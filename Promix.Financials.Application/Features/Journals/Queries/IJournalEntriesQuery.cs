using Promix.Financials.Domain.Enums;

namespace Promix.Financials.Application.Features.Journals.Queries;

public sealed record JournalEntrySummaryDto(
    Guid Id,
    string EntryNumber,
    DateOnly EntryDate,
    int Type,
    int Status,
    string? ReferenceNo,
    string? Description,
    decimal TotalDebit,
    decimal TotalCredit,
    int LineCount,
    DateTimeOffset CreatedAtUtc
);

public sealed record JournalPostingAccountDto(
    Guid Id,
    string Code,
    string NameAr,
    AccountNature Nature,
    string? SystemRole
);

public sealed record JournalAccountBalanceDto(
    Guid AccountId,
    string Code,
    string NameAr,
    AccountNature Nature,
    decimal TotalDebit,
    decimal TotalCredit
);

public sealed record JournalCashMovementDto(
    DateOnly EntryDate,
    decimal NetMovement
);

public sealed record JournalCurrencyOptionDto(
    string CurrencyCode,
    string NameAr,
    string? NameEn,
    string Symbol,
    byte DecimalPlaces,
    decimal ExchangeRate,
    bool IsBaseCurrency
);

public interface IJournalEntriesQuery
{
    Task<IReadOnlyList<JournalEntrySummaryDto>> GetEntriesAsync(Guid companyId, CancellationToken ct = default);
    Task<IReadOnlyList<JournalPostingAccountDto>> GetPostingAccountsAsync(Guid companyId, CancellationToken ct = default);
    Task<IReadOnlyList<JournalCurrencyOptionDto>> GetActiveCurrenciesAsync(Guid companyId, CancellationToken ct = default);
    Task<IReadOnlyList<JournalAccountBalanceDto>> GetPostedAccountBalancesAsync(
        Guid companyId,
        IReadOnlyList<Guid> accountIds,
        DateOnly throughDate,
        CancellationToken ct = default);
    Task<IReadOnlyList<JournalCashMovementDto>> GetCashMovementSeriesAsync(
        Guid companyId,
        DateOnly fromDate,
        DateOnly toDate,
        CancellationToken ct = default);
}
