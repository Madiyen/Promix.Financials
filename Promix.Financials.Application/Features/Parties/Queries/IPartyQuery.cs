using Promix.Financials.Domain.Enums;

namespace Promix.Financials.Application.Features.Parties.Queries;

public sealed record PartyLookupDto(
    Guid Id,
    string Code,
    string NameAr,
    PartyTypeFlags TypeFlags,
    PartyLedgerMode LedgerMode,
    Guid? ReceivableAccountId,
    Guid? PayableAccountId,
    bool IsActive
)
{
    public PartyLookupDto(
        Guid Id,
        string Code,
        string NameAr,
        PartyTypeFlags TypeFlags,
        Guid? ReceivableAccountId,
        Guid? PayableAccountId,
        bool IsActive)
        : this(
            Id,
            Code,
            NameAr,
            TypeFlags,
            ReceivableAccountId.HasValue || PayableAccountId.HasValue
                ? PartyLedgerMode.LegacyLinkedAccounts
                : PartyLedgerMode.Subledger,
            ReceivableAccountId,
            PayableAccountId,
            IsActive)
    {
    }
}

public sealed record PartyListItemDto(
    Guid Id,
    string Code,
    string NameAr,
    string? NameEn,
    PartyTypeFlags TypeFlags,
    string? Phone,
    string? Mobile,
    string? Email,
    string? TaxNo,
    string? Address,
    string? Notes,
    bool IsActive,
    PartyLedgerMode LedgerMode,
    Guid? ReceivableAccountId,
    Guid? PayableAccountId,
    decimal ReceivableOpenBalance,
    decimal PayableOpenBalance
)
{
    public PartyListItemDto(
        Guid Id,
        string Code,
        string NameAr,
        string? NameEn,
        PartyTypeFlags TypeFlags,
        string? Phone,
        string? Mobile,
        string? Email,
        string? TaxNo,
        string? Address,
        string? Notes,
        bool IsActive,
        Guid? ReceivableAccountId,
        Guid? PayableAccountId,
        decimal ReceivableOpenBalance,
        decimal PayableOpenBalance)
        : this(
            Id,
            Code,
            NameAr,
            NameEn,
            TypeFlags,
            Phone,
            Mobile,
            Email,
            TaxNo,
            Address,
            Notes,
            IsActive,
            ReceivableAccountId.HasValue || PayableAccountId.HasValue
                ? PartyLedgerMode.LegacyLinkedAccounts
                : PartyLedgerMode.Subledger,
            ReceivableAccountId,
            PayableAccountId,
            ReceivableOpenBalance,
            PayableOpenBalance)
    {
    }
}

public sealed record PartyStatementMovementDto(
    Guid LineId,
    Guid EntryId,
    string EntryNumber,
    DateOnly EntryDate,
    string AccountCode,
    string AccountNameAr,
    decimal Debit,
    decimal Credit,
    decimal RunningBalance,
    string? ReferenceNo,
    string? Description
);

public sealed record PartyOpenItemDto(
    Guid LineId,
    Guid EntryId,
    string EntryNumber,
    DateOnly EntryDate,
    Guid AccountId,
    string AccountCode,
    string AccountNameAr,
    decimal Debit,
    decimal Credit,
    decimal SettledAmount,
    decimal OpenAmount,
    int AgeDays,
    string? ReferenceNo,
    string? Description
);

public sealed record PartySettlementDto(
    Guid Id,
    Guid DebitLineId,
    Guid CreditLineId,
    decimal Amount,
    DateOnly SettledOn,
    string DebitEntryNumber,
    string CreditEntryNumber
);

public sealed record PartyAgingBucketDto(
    string Label,
    decimal ReceivableAmount,
    decimal PayableAmount
);

public sealed record PartyStatementDto(
    Guid PartyId,
    string Code,
    string NameAr,
    PartyTypeFlags TypeFlags,
    PartyLedgerMode LedgerMode,
    Guid? ReceivableAccountId,
    Guid? PayableAccountId,
    DateOnly FromDate,
    DateOnly ToDate,
    decimal OpeningBalance,
    decimal ClosingBalance,
    IReadOnlyList<PartyStatementMovementDto> Movements,
    IReadOnlyList<PartyOpenItemDto> OpenItems,
    IReadOnlyList<PartySettlementDto> Settlements,
    IReadOnlyList<PartyAgingBucketDto> AgingBuckets
)
{
    public PartyStatementDto(
        Guid PartyId,
        string Code,
        string NameAr,
        PartyTypeFlags TypeFlags,
        Guid? ReceivableAccountId,
        Guid? PayableAccountId,
        DateOnly FromDate,
        DateOnly ToDate,
        decimal OpeningBalance,
        decimal ClosingBalance,
        IReadOnlyList<PartyStatementMovementDto> Movements,
        IReadOnlyList<PartyOpenItemDto> OpenItems,
        IReadOnlyList<PartySettlementDto> Settlements,
        IReadOnlyList<PartyAgingBucketDto> AgingBuckets)
        : this(
            PartyId,
            Code,
            NameAr,
            TypeFlags,
            ReceivableAccountId.HasValue || PayableAccountId.HasValue
                ? PartyLedgerMode.LegacyLinkedAccounts
                : PartyLedgerMode.Subledger,
            ReceivableAccountId,
            PayableAccountId,
            FromDate,
            ToDate,
            OpeningBalance,
            ClosingBalance,
            Movements,
            OpenItems,
            Settlements,
            AgingBuckets)
    {
    }
}

public interface IPartyQuery
{
    Task<IReadOnlyList<PartyLookupDto>> GetActivePartiesAsync(Guid companyId, CancellationToken ct = default);
    Task<IReadOnlyList<PartyListItemDto>> GetPartiesAsync(Guid companyId, CancellationToken ct = default);
    Task<PartyStatementDto?> GetStatementAsync(
        Guid companyId,
        Guid partyId,
        DateOnly fromDate,
        DateOnly toDate,
        CancellationToken ct = default);
}
