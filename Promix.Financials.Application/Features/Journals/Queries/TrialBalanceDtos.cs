using Promix.Financials.Domain.Enums;

namespace Promix.Financials.Application.Features.Journals.Queries;

public sealed record TrialBalanceRowDto(
    Guid AccountId,
    string Code,
    string NameAr,
    AccountNature Nature,
    decimal OpeningDebit,
    decimal OpeningCredit,
    decimal PeriodDebit,
    decimal PeriodCredit,
    decimal ClosingDebit,
    decimal ClosingCredit
);

public sealed record JournalPeriodLockDto(
    DateOnly? LockedThroughDate,
    DateTimeOffset? LockedAtUtc,
    Guid? LockedByUserId
);
