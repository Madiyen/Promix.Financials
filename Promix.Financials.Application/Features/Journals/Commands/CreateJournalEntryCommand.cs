using Promix.Financials.Domain.Enums;

namespace Promix.Financials.Application.Features.Journals.Commands;

public sealed record CreateJournalEntryLineCommand(
    Guid AccountId,
    decimal Debit,
    decimal Credit,
    string? Description,
    string? PartyName = null,
    Guid? PartyId = null
);

public sealed record CreateJournalEntryCommand(
    Guid CompanyId,
    DateOnly EntryDate,
    JournalEntryType Type,
    string? ReferenceNo,
    string? Description,
    string? CurrencyCode,
    decimal? ExchangeRate,
    decimal? CurrencyAmount,
    bool PostNow,
    IReadOnlyList<CreateJournalEntryLineCommand> Lines,
    TransferSettlementMode? TransferSettlementMode = null,
    SourceDocumentType? SourceDocumentType = null,
    Guid? SourceDocumentId = null,
    string? SourceDocumentNumber = null,
    Guid? SourceLineId = null
);
