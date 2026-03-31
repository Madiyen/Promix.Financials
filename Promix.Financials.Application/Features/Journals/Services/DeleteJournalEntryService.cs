using System;
using Promix.Financials.Application.Abstractions;
using Promix.Financials.Application.Features.Journals.Commands;
using Promix.Financials.Application.Features.Parties.Services;
using Promix.Financials.Domain.Exceptions;

namespace Promix.Financials.Application.Features.Journals.Services;

public sealed class DeleteJournalEntryService
{
    private readonly IJournalEntryRepository _entries;
    private readonly IUserContext _userContext;
    private readonly IDateTimeProvider _clock;
    private readonly RebuildPartySettlementsService _settlements;

    public DeleteJournalEntryService(
        IJournalEntryRepository entries,
        IUserContext userContext,
        IDateTimeProvider clock,
        RebuildPartySettlementsService settlements)
    {
        _entries = entries;
        _userContext = userContext;
        _clock = clock;
        _settlements = settlements;
    }

    public async Task DeleteAsync(DeleteJournalEntryCommand command, CancellationToken ct = default)
    {
        if (!_userContext.IsAuthenticated || _userContext.UserId == Guid.Empty)
            throw new BusinessRuleException("User is not authenticated.");

        if (!_userContext.IsAdmin)
            throw new BusinessRuleException("Only Admin can delete vouchers.");

        if (command.CompanyId == Guid.Empty)
            throw new BusinessRuleException("CompanyId is required.");

        if (command.EntryId == Guid.Empty)
            throw new BusinessRuleException("EntryId is required.");

        var entry = await _entries.GetByIdAsync(command.CompanyId, command.EntryId, ct)
            ?? throw new BusinessRuleException("The journal entry was not found.");
        var affectedScopes = entry.Status == Domain.Enums.JournalEntryStatus.Posted
            ? RebuildPartySettlementsService.CollectScopes(entry.Lines)
            : Array.Empty<RebuildPartySettlementsService.PartyAccountScope>();

        entry.Delete(_userContext.UserId, _clock.UtcNow);
        await _entries.SaveChangesAsync(ct);

        if (affectedScopes.Count > 0)
        {
            await _settlements.RebuildAsync(command.CompanyId, affectedScopes, ct);
            await _entries.SaveChangesAsync(ct);
        }
    }
}
