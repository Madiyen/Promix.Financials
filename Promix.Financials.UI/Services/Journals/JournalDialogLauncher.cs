using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Promix.Financials.Application.Features.Journals.Commands;
using Promix.Financials.Application.Features.Journals.Queries;
using Promix.Financials.Application.Features.Journals.Services;
using Promix.Financials.Application.Features.Parties.Queries;
using Promix.Financials.UI.Dialogs.Journals;
using Promix.Financials.UI.ViewModels.Journals.Models;
using Promix.Financials.UI.ViewModels.Parties.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Promix.Financials.UI.Services.Journals;

public sealed class JournalDialogLauncher
{
    private readonly IJournalEntriesQuery _query;
    private readonly IPartyQuery _partyQuery;
    private readonly CreateJournalEntryService _createService;
    private readonly CreateDailyCashClosingService _cashClosingService;

    public JournalDialogLauncher(
        IJournalEntriesQuery query,
        IPartyQuery partyQuery,
        CreateJournalEntryService createService,
        CreateDailyCashClosingService cashClosingService)
    {
        _query = query;
        _partyQuery = partyQuery;
        _createService = createService;
        _cashClosingService = cashClosingService;
    }

    public Task<JournalDialogLaunchResult> OpenReceiptVoucherAsync(Guid companyId, XamlRoot xamlRoot)
        => OpenJournalDialogAsync(companyId, xamlRoot, JournalQuickAction.ReceiptVoucher);

    public Task<JournalDialogLaunchResult> OpenPaymentVoucherAsync(Guid companyId, XamlRoot xamlRoot)
        => OpenJournalDialogAsync(companyId, xamlRoot, JournalQuickAction.PaymentVoucher);

    public Task<JournalDialogLaunchResult> OpenTransferVoucherAsync(Guid companyId, XamlRoot xamlRoot)
        => OpenJournalDialogAsync(companyId, xamlRoot, JournalQuickAction.TransferVoucher);

    public Task<JournalDialogLaunchResult> OpenDailyJournalAsync(Guid companyId, XamlRoot xamlRoot)
        => OpenJournalDialogAsync(companyId, xamlRoot, JournalQuickAction.DailyJournal);

    private async Task<JournalDialogLaunchResult> OpenJournalDialogAsync(Guid companyId, XamlRoot xamlRoot, JournalQuickAction action)
    {
        try
        {
            var accounts = await LoadAccountsAsync(companyId);
            var parties = action is JournalQuickAction.TransferVoucher or JournalQuickAction.ReceiptVoucher or JournalQuickAction.PaymentVoucher or JournalQuickAction.DailyJournal
                ? await LoadPartiesAsync(companyId)
                : Array.Empty<PartyOptionVm>();
            var currencies = await LoadCurrenciesAsync(companyId);

            ContentDialog dialog = action switch
            {
                JournalQuickAction.ReceiptVoucher => new ReceiptVoucherDialog(companyId, accounts, currencies, parties, _query),
                JournalQuickAction.PaymentVoucher => new PaymentVoucherDialog(companyId, accounts, currencies, parties, _query),
                JournalQuickAction.TransferVoucher => new TransferVoucherDialog(companyId, accounts, currencies, parties, _query),
                JournalQuickAction.DailyJournal => new DailyJournalDialog(companyId, accounts, currencies, parties),
                _ => throw new InvalidOperationException("Unsupported journal action.")
            };

            dialog.XamlRoot = xamlRoot;
            await dialog.ShowAsync();

            return await PersistDialogResultAsync(dialog);
        }
        catch (Exception ex)
        {
            return JournalDialogLaunchResult.Failed(ex.Message);
        }
    }

    private async Task<JournalDialogLaunchResult> PersistDialogResultAsync(ContentDialog dialog)
    {
        if (dialog switch
            {
                ReceiptVoucherDialog receipt when receipt.ResultCommand is not null => receipt.ResultCommand,
                PaymentVoucherDialog payment when payment.ResultCommand is not null => payment.ResultCommand,
                TransferVoucherDialog transfer when transfer.ResultCommand is not null => transfer.ResultCommand,
                DailyJournalDialog dailyJournal when dailyJournal.ResultCommand is not null => dailyJournal.ResultCommand,
                _ => null
            } is not CreateJournalEntryCommand createCommand)
        {
            if (dialog is DailyCashClosingDialog closingDialog && closingDialog.ResultCommand is not null)
            {
                await _cashClosingService.CreateAsync(closingDialog.ResultCommand);
                return JournalDialogLaunchResult.Success();
            }

            return JournalDialogLaunchResult.FromCancel();
        }

        await _createService.CreateAsync(createCommand);
        return JournalDialogLaunchResult.Success();
    }

    private async Task<IReadOnlyList<JournalAccountOptionVm>> LoadAccountsAsync(Guid companyId)
    {
        var accounts = await _query.GetPostingAccountsAsync(companyId);
        return accounts
            .Select(account => new JournalAccountOptionVm(account.Id, account.Code, account.NameAr, account.Nature, account.SystemRole, account.IsLegacyPartyLinkedAccount))
            .ToList();
    }

    private async Task<IReadOnlyList<JournalCurrencyOptionVm>> LoadCurrenciesAsync(Guid companyId)
    {
        var currencies = await _query.GetActiveCurrenciesAsync(companyId);
        return currencies
            .Select(currency => new JournalCurrencyOptionVm(
                currency.CurrencyCode,
                currency.NameAr,
                currency.NameEn,
                currency.Symbol,
                currency.DecimalPlaces,
                currency.ExchangeRate,
                currency.IsBaseCurrency))
            .ToList();
    }

    private async Task<IReadOnlyList<PartyOptionVm>> LoadPartiesAsync(Guid companyId)
    {
        var parties = await _partyQuery.GetActivePartiesAsync(companyId);
        return parties
            .Select(x => new PartyOptionVm(x.Id, x.Code, x.NameAr, x.TypeFlags, x.LedgerMode, x.ReceivableAccountId, x.PayableAccountId, x.IsActive))
            .ToList();
    }
}

public enum JournalQuickAction
{
    ReceiptVoucher,
    PaymentVoucher,
    TransferVoucher,
    DailyJournal
}

public sealed record JournalDialogLaunchResult(bool Saved, bool Cancelled, string? ErrorMessage)
{
    public static JournalDialogLaunchResult Success() => new(true, false, null);
    public static JournalDialogLaunchResult FromCancel() => new(false, true, null);
    public static JournalDialogLaunchResult Failed(string message) => new(false, false, message);
}
