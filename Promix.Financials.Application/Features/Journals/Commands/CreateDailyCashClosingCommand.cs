namespace Promix.Financials.Application.Features.Journals.Commands;

public sealed record CreateDailyCashClosingCommand(
    Guid CompanyId,
    DateOnly EntryDate,
    Guid SourceAccountId,
    Guid TargetAccountId,
    string? ReferenceNo,
    string? Description,
    bool LockThroughEntryDate
);
