using Promix.Financials.Domain.Enums;

namespace Promix.Financials.Application.Features.Journals.Commands;

public sealed record UpdateJournalEntryCommand(
    Guid CompanyId,
    Guid EntryId,
    DateOnly EntryDate,
    string? ReferenceNo,
    string? Description,
    string? CurrencyCode,
    decimal? ExchangeRate,
    decimal? CurrencyAmount,
    bool PostNow,
    IReadOnlyList<CreateJournalEntryLineCommand> Lines,
    TransferSettlementMode? TransferSettlementMode = null
);
