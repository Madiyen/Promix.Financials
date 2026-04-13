using Promix.Financials.Application.Abstractions;
using Promix.Financials.Application.Features.Journals.Commands;
using Promix.Financials.Application.Features.Parties.Services;
using Promix.Financials.Domain.Exceptions;

namespace Promix.Financials.Application.Features.Journals.Services;

public sealed class UpdateJournalEntryService
{
    private readonly IJournalEntryRepository _entries;
    private readonly IUserContext _userContext;
    private readonly IDateTimeProvider _clock;
    private readonly AccountingPostingService _posting;
    private readonly RebuildPartySettlementsService _settlements;

    public UpdateJournalEntryService(
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

    public async Task UpdateAsync(UpdateJournalEntryCommand command, CancellationToken ct = default)
    {
        if (!_userContext.IsAuthenticated || _userContext.UserId == Guid.Empty)
            throw new BusinessRuleException("User is not authenticated.");

        if (!_userContext.IsAdmin)
            throw new BusinessRuleException("Only Admin can edit vouchers.");

        if (command.CompanyId == Guid.Empty)
            throw new BusinessRuleException("CompanyId is required.");

        if (command.EntryId == Guid.Empty)
            throw new BusinessRuleException("EntryId is required.");

        var entry = await _entries.GetByIdAsync(command.CompanyId, command.EntryId, ct)
            ?? throw new BusinessRuleException("The journal entry was not found.");

        if (entry.Status == Domain.Enums.JournalEntryStatus.Posted)
            throw new BusinessRuleException("Posted journal entries are immutable. Use reversal or void in a later correction flow.");

        var preparation = await _posting.PrepareUpdateAsync(command, entry, ct);

        entry.Update(
            command.EntryDate,
            preparation.FinancialYearId,
            preparation.FinancialPeriodId,
            preparation.CurrencyCode,
            preparation.ExchangeRate,
            preparation.CurrencyAmount,
            preparation.SourceDocumentType,
            preparation.SourceDocumentId,
            preparation.SourceDocumentNumber,
            preparation.SourceLineId,
            command.ReferenceNo,
            command.Description,
            preparation.Lines,
            _userContext.UserId,
            _clock.UtcNow,
            command.TransferSettlementMode);

        if (command.PostNow)
            entry.Post(_userContext.UserId, _clock.UtcNow);

        await _entries.SaveChangesAsync(ct);

        var currentScopes = entry.Status == Domain.Enums.JournalEntryStatus.Posted
            ? RebuildPartySettlementsService.CollectScopes(entry.Lines)
            : Array.Empty<RebuildPartySettlementsService.PartyAccountScope>();

        if (currentScopes.Count > 0 && ShouldRebuildPartySettlements(entry))
        {
            await _settlements.RebuildAsync(command.CompanyId, currentScopes, ct);
            await _entries.SaveChangesAsync(ct);
        }
    }

    private static bool ShouldRebuildPartySettlements(Promix.Financials.Domain.Aggregates.Journals.JournalEntry entry)
        => entry.Type != Domain.Enums.JournalEntryType.TransferVoucher
            || entry.TransferSettlementMode == Domain.Enums.TransferSettlementMode.Automatic;
}
