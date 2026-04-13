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
    string CurrencyCode,
    decimal ExchangeRate,
    decimal CurrencyAmount,
    decimal TotalDebit,
    decimal TotalCredit,
    int LineCount,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset? PostedAtUtc,
    DateTimeOffset? ModifiedAtUtc
);

public sealed record JournalEntryDetailLineDto(
    Guid AccountId,
    decimal Debit,
    decimal Credit,
    string? PartyName,
    string? Description,
    Guid? PartyId = null
);

public sealed record JournalEntryDetailDto(
    Guid Id,
    string EntryNumber,
    DateOnly EntryDate,
    int Type,
    int Status,
    string? ReferenceNo,
    string? Description,
    string CurrencyCode,
    decimal ExchangeRate,
    decimal CurrencyAmount,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset? PostedAtUtc,
    Guid? ModifiedByUserId,
    DateTimeOffset? ModifiedAtUtc,
    IReadOnlyList<JournalEntryDetailLineDto> Lines,
    TransferSettlementMode? TransferSettlementMode = null,
    int? SourceDocumentType = null,
    Guid? SourceDocumentId = null,
    string? SourceDocumentNumber = null,
    Guid? SourceLineId = null
);

public sealed record JournalPostingAccountDto(
    Guid Id,
    string Code,
    string NameAr,
    AccountNature Nature,
    string? SystemRole,
    bool IsLegacyPartyLinkedAccount
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
    Task<JournalEntryDetailDto?> GetEntryDetailAsync(Guid companyId, Guid entryId, CancellationToken ct = default);
    Task<IReadOnlyList<JournalPostingAccountDto>> GetPostingAccountsAsync(Guid companyId, CancellationToken ct = default);
    Task<IReadOnlyList<JournalCurrencyOptionDto>> GetActiveCurrenciesAsync(Guid companyId, CancellationToken ct = default);
    Task<IReadOnlyList<int>> GetAvailableFiscalYearsAsync(Guid companyId, CancellationToken ct = default);
    Task<JournalPeriodLockDto> GetJournalPeriodLockAsync(Guid companyId, CancellationToken ct = default);
    Task<AccountStatementDto?> GetAccountStatementAsync(
        Guid companyId,
        Guid accountId,
        DateOnly fromDate,
        DateOnly toDate,
        CancellationToken ct = default);
    Task<IReadOnlyList<TrialBalanceRowDto>> GetTrialBalanceAsync(
        Guid companyId,
        DateOnly fromDate,
        DateOnly toDate,
        bool includeZeroBalance,
        CancellationToken ct = default);
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
