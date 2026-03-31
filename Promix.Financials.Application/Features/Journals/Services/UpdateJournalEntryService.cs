using System;
using System.Linq;
using Promix.Financials.Application.Abstractions;
using Promix.Financials.Application.Features.Journals.Commands;
using Promix.Financials.Application.Features.Parties.Services;
using Promix.Financials.Domain.Aggregates.Journals;
using Promix.Financials.Domain.Exceptions;

namespace Promix.Financials.Application.Features.Journals.Services;

public sealed class UpdateJournalEntryService
{
    private readonly IJournalEntryRepository _entries;
    private readonly IAccountRepository _accounts;
    private readonly IPartyRepository _parties;
    private readonly ICompanyCurrencyRepository _currencies;
    private readonly IUserContext _userContext;
    private readonly IDateTimeProvider _clock;
    private readonly JournalPeriodLockService _periodLockService;
    private readonly RebuildPartySettlementsService _settlements;
    private readonly PartyPostingRulesService _partyPostingRules;

    public UpdateJournalEntryService(
        IJournalEntryRepository entries,
        IAccountRepository accounts,
        IPartyRepository parties,
        ICompanyCurrencyRepository currencies,
        IUserContext userContext,
        IDateTimeProvider clock,
        JournalPeriodLockService periodLockService,
        RebuildPartySettlementsService settlements,
        PartyPostingRulesService? partyPostingRules = null)
    {
        _entries = entries;
        _accounts = accounts;
        _parties = parties;
        _currencies = currencies;
        _userContext = userContext;
        _clock = clock;
        _periodLockService = periodLockService;
        _settlements = settlements;
        _partyPostingRules = partyPostingRules ?? new PartyPostingRulesService(accounts, parties);
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

        if (command.Lines is null || command.Lines.Count < 2)
            throw new BusinessRuleException("The journal entry must contain at least two lines.");

        var entry = await _entries.GetByIdAsync(command.CompanyId, command.EntryId, ct)
            ?? throw new BusinessRuleException("The journal entry was not found.");
        var previousShouldRebuildSettlements = ShouldRebuildPartySettlements(entry);
        var previousScopes = entry.Status == Domain.Enums.JournalEntryStatus.Posted
            ? RebuildPartySettlementsService.CollectScopes(entry.Lines)
            : Array.Empty<RebuildPartySettlementsService.PartyAccountScope>();

        await _periodLockService.EnsureEntryDateIsOpenAsync(command.CompanyId, command.EntryDate, ct);

        var totalDebit = command.Lines.Sum(x => x.Debit);
        if (totalDebit <= 0)
            throw new BusinessRuleException("The journal entry total must be greater than zero.");

        var activeCurrencies = await _currencies.GetAllAsync(command.CompanyId, ct);
        var baseCurrency = activeCurrencies.FirstOrDefault(x => x.IsBaseCurrency && x.IsActive)
            ?? throw new BusinessRuleException("Base company currency was not found.");

        var requestedCurrencyCode = string.IsNullOrWhiteSpace(command.CurrencyCode)
            ? baseCurrency.CurrencyCode
            : command.CurrencyCode.Trim().ToUpperInvariant();

        var selectedCurrency = activeCurrencies.FirstOrDefault(x =>
            x.IsActive &&
            string.Equals(x.CurrencyCode, requestedCurrencyCode, StringComparison.OrdinalIgnoreCase))
            ?? throw new BusinessRuleException("The selected voucher currency is not available for this company.");

        var exchangeRate = command.ExchangeRate is > 0
            ? decimal.Round(command.ExchangeRate.Value, 8, MidpointRounding.AwayFromZero)
            : selectedCurrency.ExchangeRate;

        if (selectedCurrency.IsBaseCurrency)
            exchangeRate = 1m;

        if (exchangeRate <= 0)
            throw new BusinessRuleException("Voucher exchange rate must be greater than zero.");

        var currencyAmount = command.CurrencyAmount is > 0
            ? decimal.Round(command.CurrencyAmount.Value, 4, MidpointRounding.AwayFromZero)
            : decimal.Round(totalDebit / exchangeRate, 4, MidpointRounding.AwayFromZero);

        var editableLines = new List<JournalEntryEditableLine>(command.Lines.Count);
        foreach (var line in command.Lines)
        {
            var account = await _accounts.GetByIdAsync(line.AccountId, command.CompanyId);
            if (account is null)
                throw new BusinessRuleException("One of the selected accounts was not found.");

            if (!account.IsPosting)
                throw new BusinessRuleException($"Account {account.Code} must be a posting account.");

            if (!account.IsActive)
                throw new BusinessRuleException($"Account {account.Code} is inactive.");

            var partyName = await _partyPostingRules.ResolvePartyNameAsync(
                command.CompanyId,
                line.AccountId,
                line.PartyId,
                line.PartyName,
                ct);
            editableLines.Add(new JournalEntryEditableLine(
                line.AccountId,
                line.PartyId,
                partyName,
                line.Description,
                line.Debit,
                line.Credit));
        }

        entry.Update(
            command.EntryDate,
            selectedCurrency.CurrencyCode,
            exchangeRate,
            currencyAmount,
            command.ReferenceNo,
            command.Description,
            editableLines,
            _userContext.UserId,
            _clock.UtcNow,
            command.TransferSettlementMode);

        if (entry.Status != Domain.Enums.JournalEntryStatus.Posted && command.PostNow)
            entry.Post(_userContext.UserId, _clock.UtcNow);

        await _entries.SaveChangesAsync(ct);

        var currentScopes = entry.Status == Domain.Enums.JournalEntryStatus.Posted
            ? RebuildPartySettlementsService.CollectScopes(entry.Lines)
            : Array.Empty<RebuildPartySettlementsService.PartyAccountScope>();
        var affectedScopes = previousScopes.Concat(currentScopes).Distinct().ToList();

        if (affectedScopes.Count > 0
            && (previousShouldRebuildSettlements || ShouldRebuildPartySettlements(entry)))
        {
            await _settlements.RebuildAsync(command.CompanyId, affectedScopes, ct);
            await _entries.SaveChangesAsync(ct);
        }
    }

    private static bool ShouldRebuildPartySettlements(JournalEntry entry)
        => entry.Type != Domain.Enums.JournalEntryType.TransferVoucher
            || entry.TransferSettlementMode == Domain.Enums.TransferSettlementMode.Automatic;
}
