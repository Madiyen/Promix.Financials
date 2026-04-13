using Promix.Financials.Application.Abstractions;
using Promix.Financials.Application.Features.Journals.Commands;
using Promix.Financials.Application.Features.Parties.Services;
using Promix.Financials.Domain.Aggregates.Journals;
using Promix.Financials.Domain.Exceptions;

namespace Promix.Financials.Application.Features.Journals.Services;

public sealed class CreateJournalEntryService
{
    private readonly IJournalEntryRepository _entries;
    private readonly IUserContext _userContext;
    private readonly IDateTimeProvider _clock;
    private readonly AccountingPostingService _posting;
    private readonly RebuildPartySettlementsService _settlements;

    public CreateJournalEntryService(
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

    public async Task<Guid> CreateAsync(CreateJournalEntryCommand command, CancellationToken ct = default)
    {
        if (!_userContext.IsAuthenticated || _userContext.UserId == Guid.Empty)
            throw new BusinessRuleException("User is not authenticated.");

        if (command.CompanyId == Guid.Empty)
            throw new BusinessRuleException("CompanyId is required.");

        var preparation = await _posting.PrepareCreateAsync(command, ct);

        var entry = new JournalEntry(
            companyId: command.CompanyId,
            entryNumber: preparation.EntryNumber ?? throw new BusinessRuleException("Failed to generate journal entry number."),
            entryDate: command.EntryDate,
            financialYearId: preparation.FinancialYearId,
            financialPeriodId: preparation.FinancialPeriodId,
            type: command.Type,
            currencyCode: preparation.CurrencyCode,
            exchangeRate: preparation.ExchangeRate,
            currencyAmount: preparation.CurrencyAmount,
            sourceDocumentType: preparation.SourceDocumentType,
            sourceDocumentId: preparation.SourceDocumentId,
            sourceDocumentNumber: preparation.SourceDocumentNumber,
            sourceLineId: preparation.SourceLineId,
            createdByUserId: _userContext.UserId,
            createdAtUtc: _clock.UtcNow,
            referenceNo: command.ReferenceNo,
            description: command.Description,
            transferSettlementMode: command.TransferSettlementMode);

        foreach (var line in preparation.Lines)
            entry.AddLine(line.AccountId, line.PartyId, line.PartyName, line.Description, line.Debit, line.Credit);

        if (command.PostNow)
            entry.Post(_userContext.UserId, _clock.UtcNow);

        await _entries.AddAsync(entry, ct);
        await _entries.SaveChangesAsync(ct);

        if (entry.Status == Domain.Enums.JournalEntryStatus.Posted && ShouldRebuildPartySettlements(entry))
        {
            await _settlements.RebuildAsync(
                command.CompanyId,
                RebuildPartySettlementsService.CollectScopes(entry.Lines),
                ct);
            await _entries.SaveChangesAsync(ct);
        }

        return entry.Id;
    }

    private static bool ShouldRebuildPartySettlements(JournalEntry entry)
        => entry.Type != Domain.Enums.JournalEntryType.TransferVoucher
            || entry.TransferSettlementMode == Domain.Enums.TransferSettlementMode.Automatic;
}
