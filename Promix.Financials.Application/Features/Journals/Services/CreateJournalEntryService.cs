using System;
using System.Linq;
using Promix.Financials.Application.Abstractions;
using Promix.Financials.Application.Features.Journals.Commands;
using Promix.Financials.Domain.Aggregates.Journals;
using Promix.Financials.Domain.Exceptions;

namespace Promix.Financials.Application.Features.Journals.Services;

public sealed class CreateJournalEntryService
{
    private readonly IJournalEntryRepository _entries;
    private readonly IAccountRepository _accounts;
    private readonly ICompanyCurrencyRepository _currencies;
    private readonly IUserContext _userContext;
    private readonly IDateTimeProvider _clock;
    private readonly JournalPeriodLockService _periodLockService;

    public CreateJournalEntryService(
        IJournalEntryRepository entries,
        IAccountRepository accounts,
        ICompanyCurrencyRepository currencies,
        IUserContext userContext,
        IDateTimeProvider clock,
        JournalPeriodLockService periodLockService)
    {
        _entries = entries;
        _accounts = accounts;
        _currencies = currencies;
        _userContext = userContext;
        _clock = clock;
        _periodLockService = periodLockService;
    }

    public async Task<Guid> CreateAsync(CreateJournalEntryCommand command, CancellationToken ct = default)
    {
        if (!_userContext.IsAuthenticated || _userContext.UserId == Guid.Empty)
            throw new BusinessRuleException("User is not authenticated.");

        if (command.CompanyId == Guid.Empty)
            throw new BusinessRuleException("CompanyId is required.");

        if (command.Lines is null || command.Lines.Count < 2)
            throw new BusinessRuleException("The journal entry must contain at least two lines.");

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

        var entryNumber = await _entries.GenerateNextNumberAsync(command.CompanyId, command.Type, ct);

        var entry = new JournalEntry(
            companyId: command.CompanyId,
            entryNumber: entryNumber,
            entryDate: command.EntryDate,
            type: command.Type,
            currencyCode: selectedCurrency.CurrencyCode,
            exchangeRate: exchangeRate,
            currencyAmount: currencyAmount,
            createdByUserId: _userContext.UserId,
            createdAtUtc: _clock.UtcNow,
            referenceNo: command.ReferenceNo,
            description: command.Description);

        foreach (var line in command.Lines)
        {
            var account = await _accounts.GetByIdAsync(line.AccountId, command.CompanyId);
            if (account is null)
                throw new BusinessRuleException("One of the selected accounts was not found.");

            if (!account.IsPosting)
                throw new BusinessRuleException($"Account {account.Code} must be a posting account.");

            if (!account.IsActive)
                throw new BusinessRuleException($"Account {account.Code} is inactive.");

            entry.AddLine(line.AccountId, line.Description, line.Debit, line.Credit);
        }

        if (command.PostNow)
            entry.Post(_userContext.UserId, _clock.UtcNow);

        await _entries.AddAsync(entry, ct);
        await _entries.SaveChangesAsync(ct);

        return entry.Id;
    }
}
