namespace Promix.Financials.Application.Features.Journals.Commands;

public sealed record DeleteJournalEntryCommand(
    Guid CompanyId,
    Guid EntryId
);
