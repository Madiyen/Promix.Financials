using System.Linq;
using Promix.Financials.Application.Abstractions;
using Promix.Financials.Application.Features.Journals.Commands;
using Promix.Financials.Application.Features.Parties.Services;
using Promix.Financials.Domain.Aggregates.Journals;
using Promix.Financials.Domain.Enums;
using Promix.Financials.Domain.Exceptions;

namespace Promix.Financials.Application.Features.Journals.Services;

public sealed class AccountingPostingService
{
    private readonly IJournalEntryRepository _entries;
    private readonly IAccountRepository _accounts;
    private readonly ICompanyCurrencyRepository _currencies;
    private readonly PartyPostingRulesService _partyPostingRules;
    private readonly FinancialPeriodGuard _periodGuard;

    public AccountingPostingService(
        IJournalEntryRepository entries,
        IAccountRepository accounts,
        ICompanyCurrencyRepository currencies,
        PartyPostingRulesService partyPostingRules,
        FinancialPeriodGuard periodGuard)
    {
        _entries = entries;
        _accounts = accounts;
        _currencies = currencies;
        _partyPostingRules = partyPostingRules;
        _periodGuard = periodGuard;
    }

    public async Task<AccountingEntryPreparation> PrepareCreateAsync(CreateJournalEntryCommand command, CancellationToken ct = default)
    {
        var basePreparation = await PrepareCoreAsync(
            command.CompanyId,
            command.EntryDate,
            command.Type,
            command.CurrencyCode,
            command.ExchangeRate,
            command.CurrencyAmount,
            command.Lines,
            command.ReferenceNo,
            command.SourceDocumentType,
            command.SourceDocumentId,
            command.SourceDocumentNumber,
            command.SourceLineId,
            ct);

        var entryNumber = await _entries.GenerateNextNumberAsync(command.CompanyId, basePreparation.FinancialYearId, command.Type, ct);
        return basePreparation with
        {
            EntryNumber = entryNumber,
            SourceDocumentNumber = basePreparation.SourceDocumentNumber ?? entryNumber
        };
    }

    public Task<AccountingEntryPreparation> PrepareUpdateAsync(
        UpdateJournalEntryCommand command,
        JournalEntry entry,
        CancellationToken ct = default)
        => PrepareCoreAsync(
            command.CompanyId,
            command.EntryDate,
            entry.Type,
            command.CurrencyCode,
            command.ExchangeRate,
            command.CurrencyAmount,
            command.Lines,
            command.ReferenceNo,
            command.SourceDocumentType ?? entry.SourceDocumentType,
            command.SourceDocumentId ?? entry.SourceDocumentId,
            command.SourceDocumentNumber ?? entry.SourceDocumentNumber,
            command.SourceLineId ?? entry.SourceLineId,
            ct,
            entry.EntryNumber);

    public async Task<AccountingExistingEntryValidation> ValidateExistingEntryForPostingAsync(JournalEntry entry, CancellationToken ct = default)
    {
        var period = await _periodGuard.ResolveOpenPeriodAsync(entry.CompanyId, entry.EntryDate, ct);
        await ValidateCurrencyAsync(entry.CompanyId, entry.CurrencyCode, entry.ExchangeRate, ct);
        await ValidateExistingLinesAsync(entry.CompanyId, entry.Lines, ct);

        return new AccountingExistingEntryValidation(period.FinancialYear.Id, period.FinancialPeriod.Id);
    }

    private async Task<AccountingEntryPreparation> PrepareCoreAsync(
        Guid companyId,
        DateOnly entryDate,
        JournalEntryType type,
        string? requestedCurrencyCode,
        decimal? requestedExchangeRate,
        decimal? requestedCurrencyAmount,
        IReadOnlyList<CreateJournalEntryLineCommand> lines,
        string? referenceNo,
        SourceDocumentType? sourceDocumentType,
        Guid? sourceDocumentId,
        string? sourceDocumentNumber,
        Guid? sourceLineId,
        CancellationToken ct,
        string? existingEntryNumber = null)
    {
        if (companyId == Guid.Empty)
            throw new BusinessRuleException("CompanyId is required.");

        if (lines is null || lines.Count < 2)
            throw new BusinessRuleException("The journal entry must contain at least two lines.");

        var totalDebit = lines.Sum(x => x.Debit);
        if (totalDebit <= 0)
            throw new BusinessRuleException("The journal entry total must be greater than zero.");

        var period = await _periodGuard.ResolveOpenPeriodAsync(companyId, entryDate, ct);
        var currency = await ResolveCurrencyAsync(companyId, requestedCurrencyCode, requestedExchangeRate, requestedCurrencyAmount, totalDebit, ct);
        var validatedLines = await ValidateLinesAsync(companyId, lines, ct);
        var resolvedSourceType = ResolveSourceDocumentType(type, sourceDocumentType);

        return new AccountingEntryPreparation(
            EntryNumber: existingEntryNumber,
            FinancialYearId: period.FinancialYear.Id,
            FinancialPeriodId: period.FinancialPeriod.Id,
            CurrencyCode: currency.CurrencyCode,
            ExchangeRate: currency.ExchangeRate,
            CurrencyAmount: currency.CurrencyAmount,
            Lines: validatedLines,
            SourceDocumentType: resolvedSourceType,
            SourceDocumentId: sourceDocumentId == Guid.Empty ? null : sourceDocumentId,
            SourceDocumentNumber: NormalizeSourceDocumentNumber(sourceDocumentNumber, referenceNo, existingEntryNumber),
            SourceLineId: sourceLineId == Guid.Empty ? null : sourceLineId);
    }

    private async Task<AccountingCurrencyResolution> ResolveCurrencyAsync(
        Guid companyId,
        string? requestedCurrencyCode,
        decimal? requestedExchangeRate,
        decimal? requestedCurrencyAmount,
        decimal totalDebit,
        CancellationToken ct)
    {
        var activeCurrencies = await _currencies.GetAllAsync(companyId, ct);
        var baseCurrency = activeCurrencies.FirstOrDefault(x => x.IsBaseCurrency && x.IsActive)
            ?? throw new BusinessRuleException("Base company currency was not found.");

        var normalizedRequestedCode = string.IsNullOrWhiteSpace(requestedCurrencyCode)
            ? baseCurrency.CurrencyCode
            : requestedCurrencyCode.Trim().ToUpperInvariant();

        var selectedCurrency = activeCurrencies.FirstOrDefault(x =>
            x.IsActive && string.Equals(x.CurrencyCode, normalizedRequestedCode, StringComparison.OrdinalIgnoreCase))
            ?? throw new BusinessRuleException("The selected voucher currency is not available for this company.");

        var exchangeRate = requestedExchangeRate is > 0
            ? decimal.Round(requestedExchangeRate.Value, 8, MidpointRounding.AwayFromZero)
            : selectedCurrency.ExchangeRate;

        if (selectedCurrency.IsBaseCurrency)
            exchangeRate = 1m;

        if (exchangeRate <= 0)
            throw new BusinessRuleException("Voucher exchange rate must be greater than zero.");

        var currencyAmount = requestedCurrencyAmount is > 0
            ? decimal.Round(requestedCurrencyAmount.Value, 4, MidpointRounding.AwayFromZero)
            : decimal.Round(totalDebit / exchangeRate, 4, MidpointRounding.AwayFromZero);

        return new AccountingCurrencyResolution(selectedCurrency.CurrencyCode, exchangeRate, currencyAmount);
    }

    private async Task ValidateCurrencyAsync(Guid companyId, string currencyCode, decimal exchangeRate, CancellationToken ct)
    {
        var activeCurrencies = await _currencies.GetAllAsync(companyId, ct);
        var selectedCurrency = activeCurrencies.FirstOrDefault(x =>
            x.IsActive && string.Equals(x.CurrencyCode, currencyCode, StringComparison.OrdinalIgnoreCase))
            ?? throw new BusinessRuleException("The selected voucher currency is not available for this company.");

        if (selectedCurrency.IsBaseCurrency && exchangeRate != 1m)
            throw new BusinessRuleException("Base currency entries must use an exchange rate of 1.");

        if (exchangeRate <= 0)
            throw new BusinessRuleException("Voucher exchange rate must be greater than zero.");
    }

    private async Task<IReadOnlyList<JournalEntryEditableLine>> ValidateLinesAsync(
        Guid companyId,
        IReadOnlyList<CreateJournalEntryLineCommand> lines,
        CancellationToken ct)
    {
        var validatedLines = new List<JournalEntryEditableLine>(lines.Count);

        foreach (var line in lines)
        {
            var account = await _accounts.GetByIdAsync(line.AccountId, companyId, ct)
                ?? throw new BusinessRuleException("One of the selected accounts was not found.");

            if (!account.IsPosting)
                throw new BusinessRuleException($"Account {account.Code} must be a posting account.");

            if (!account.IsActive)
                throw new BusinessRuleException($"Account {account.Code} is inactive.");

            var partyName = await _partyPostingRules.ResolvePartyNameAsync(
                companyId,
                line.AccountId,
                line.PartyId,
                line.PartyName,
                ct);

            validatedLines.Add(new JournalEntryEditableLine(
                line.AccountId,
                line.PartyId,
                partyName,
                line.Description,
                line.Debit,
                line.Credit));
        }

        return validatedLines;
    }

    private async Task ValidateExistingLinesAsync(Guid companyId, IReadOnlyCollection<JournalLine> lines, CancellationToken ct)
    {
        foreach (var line in lines)
        {
            var account = await _accounts.GetByIdAsync(line.AccountId, companyId, ct)
                ?? throw new BusinessRuleException("One of the selected accounts was not found.");

            if (!account.IsPosting)
                throw new BusinessRuleException($"Account {account.Code} must be a posting account.");

            if (!account.IsActive)
                throw new BusinessRuleException($"Account {account.Code} is inactive.");

            await _partyPostingRules.ResolvePartyNameAsync(
                companyId,
                line.AccountId,
                line.PartyId,
                line.PartyName,
                ct);
        }
    }

    private static SourceDocumentType ResolveSourceDocumentType(JournalEntryType entryType, SourceDocumentType? explicitType)
        => explicitType ?? entryType switch
        {
            JournalEntryType.ReceiptVoucher => SourceDocumentType.ReceiptVoucher,
            JournalEntryType.PaymentVoucher => SourceDocumentType.PaymentVoucher,
            JournalEntryType.TransferVoucher => SourceDocumentType.TransferVoucher,
            JournalEntryType.OpeningEntry => SourceDocumentType.OpeningEntry,
            JournalEntryType.Adjustment => SourceDocumentType.Adjustment,
            JournalEntryType.DailyCashClosing => SourceDocumentType.DailyCashClosing,
            _ => SourceDocumentType.ManualJournal
        };

    private static string? NormalizeSourceDocumentNumber(string? sourceDocumentNumber, string? referenceNo, string? fallbackEntryNumber)
    {
        if (!string.IsNullOrWhiteSpace(sourceDocumentNumber))
            return sourceDocumentNumber.Trim();

        if (!string.IsNullOrWhiteSpace(referenceNo))
            return referenceNo.Trim();

        return string.IsNullOrWhiteSpace(fallbackEntryNumber) ? null : fallbackEntryNumber.Trim();
    }
}

public sealed record AccountingEntryPreparation(
    string? EntryNumber,
    Guid FinancialYearId,
    Guid FinancialPeriodId,
    string CurrencyCode,
    decimal ExchangeRate,
    decimal CurrencyAmount,
    IReadOnlyList<JournalEntryEditableLine> Lines,
    SourceDocumentType SourceDocumentType,
    Guid? SourceDocumentId,
    string? SourceDocumentNumber,
    Guid? SourceLineId);

public sealed record AccountingExistingEntryValidation(
    Guid FinancialYearId,
    Guid FinancialPeriodId);

internal sealed record AccountingCurrencyResolution(
    string CurrencyCode,
    decimal ExchangeRate,
    decimal CurrencyAmount);
