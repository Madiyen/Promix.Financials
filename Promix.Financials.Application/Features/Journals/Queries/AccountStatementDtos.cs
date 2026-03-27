using Promix.Financials.Domain.Enums;

namespace Promix.Financials.Application.Features.Journals.Queries;

public sealed record AccountStatementMovementDto(
    Guid EntryId,
    string EntryNumber,
    DateOnly EntryDate,
    int Type,
    string? ReferenceNo,
    string? Description,
    decimal Debit,
    decimal Credit
);

public sealed record AccountStatementDto(
    Guid AccountId,
    string Code,
    string NameAr,
    AccountNature Nature,
    DateOnly FromDate,
    DateOnly ToDate,
    decimal OpeningDebit,
    decimal OpeningCredit,
    IReadOnlyList<AccountStatementMovementDto> Movements
);
