using Promix.Financials.Application.Abstractions;
using Promix.Financials.Application.Features.Journals.Commands;
using Promix.Financials.Application.Features.Parties.Services;
using Promix.Financials.Domain.Exceptions;

namespace Promix.Financials.Application.Features.Journals.Services;

public sealed class PostJournalEntryService
{
    private readonly IJournalEntryRepository _entries;
    private readonly IUserContext _userContext;
    private readonly IDateTimeProvider _clock;
    private readonly AccountingPostingService _posting;
    private readonly RebuildPartySettlementsService _settlements;

    public PostJournalEntryService(
        IJournalEntryRepository entries,
        IUserContext userContext,
        IDateTimeProvider clock,
        AccountingPostingService posting,
        RebuildPartySettlementsService settlements)
    {
        _entries = entries;
        _userContext = userContext;
        _clock = clock;
        _posting = posting;
        _settlements = settlements;
    }

    public async Task PostAsync(PostJournalEntryCommand command, CancellationToken ct = default)
    {
        if (!_userContext.IsAuthenticated || _userContext.UserId == Guid.Empty)
            throw new BusinessRuleException("User is not authenticated.");

        var entry = await _entries.GetByIdAsync(command.CompanyId, command.EntryId, ct);
        if (entry is null)
            throw new BusinessRuleException("Journal entry not found.");

        var validation = await _posting.ValidateExistingEntryForPostingAsync(entry, ct);
        entry.AssignFinancialContext(validation.FinancialYearId, validation.FinancialPeriodId);
        entry.Post(_userContext.UserId, _clock.UtcNow);
        await _entries.SaveChangesAsync(ct);

        var scopes = RebuildPartySettlementsService.CollectScopes(entry.Lines);
        if (scopes.Count > 0 && ShouldRebuildPartySettlements(entry))
        {
            await _settlements.RebuildAsync(command.CompanyId, scopes, ct);
            await _entries.SaveChangesAsync(ct);
        }
    }

    private static bool ShouldRebuildPartySettlements(Promix.Financials.Domain.Aggregates.Journals.JournalEntry entry)
        => entry.Type != Promix.Financials.Domain.Enums.JournalEntryType.TransferVoucher
            || entry.TransferSettlementMode == Promix.Financials.Domain.Enums.TransferSettlementMode.Automatic;
}
