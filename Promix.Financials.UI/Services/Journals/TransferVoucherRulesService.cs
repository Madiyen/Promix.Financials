using System;
using System.Collections.Generic;
using System.Linq;
using Promix.Financials.Domain.Enums;
using Promix.Financials.UI.ViewModels.Journals.Models;
using Promix.Financials.UI.ViewModels.Parties.Models;

namespace Promix.Financials.UI.Services.Journals;

public sealed class TransferVoucherRulesService
{
    private readonly Dictionary<Guid, JournalAccountOptionVm> _accountsById;
    private readonly Dictionary<Guid, PartyOptionVm> _partiesById;

    public TransferVoucherRulesService(
        IEnumerable<JournalAccountOptionVm> accounts,
        IEnumerable<PartyOptionVm> parties)
    {
        var accountList = accounts.ToList();
        var partyList = parties.ToList();
        _accountsById = accountList.ToDictionary(x => x.Id);
        _partiesById = partyList.ToDictionary(x => x.Id);
        GeneralAccountOptions = accountList
            .Where(x => !x.IsLegacyPartyLinkedAccount && !x.IsPartyControlAccount)
            .OrderBy(x => x.Code)
            .ToList();
        PartyOptions = partyList
            .Where(x => x.IsActive)
            .OrderBy(x => x.Code)
            .ToList();
        ReceivableControlAccountId = accountList.FirstOrDefault(x => x.IsReceivableControl)?.Id;
        PayableControlAccountId = accountList.FirstOrDefault(x => x.IsPayableControl)?.Id;
    }

    public IReadOnlyList<JournalAccountOptionVm> GeneralAccountOptions { get; }
    public IReadOnlyList<PartyOptionVm> PartyOptions { get; }
    public Guid? ReceivableControlAccountId { get; }
    public Guid? PayableControlAccountId { get; }

    public JournalAccountOptionVm? GetAccount(Guid? accountId)
        => accountId is Guid resolvedAccountId && _accountsById.TryGetValue(resolvedAccountId, out var account)
            ? account
            : null;

    public PartyOptionVm? GetParty(Guid? partyId)
        => partyId is Guid resolvedPartyId && _partiesById.TryGetValue(resolvedPartyId, out var party)
            ? party
            : null;

    public PartyLedgerSide? NormalizePartySide(PartyOptionVm? party, PartyLedgerSide? requestedSide)
    {
        if (party is null)
            return null;

        var singleSide = party.GetSingleAvailableSide();
        if (singleSide.HasValue)
            return singleSide.Value;

        return requestedSide;
    }

    public bool TryResolveEndpoint(
        TransferEndpointEditorVm endpoint,
        string endpointLabel,
        out ResolvedTransferEndpoint? resolvedEndpoint,
        out string error)
    {
        resolvedEndpoint = null;
        error = string.Empty;

        if (endpoint.Mode == TransferEndpointMode.GeneralAccount)
        {
            if (endpoint.SelectedAccountId is not Guid generalAccountId)
            {
                error = $"اختر {endpointLabel}.";
                return false;
            }

            var generalAccount = GeneralAccountOptions.FirstOrDefault(x => x.Id == generalAccountId);
            if (generalAccount is null)
            {
                error = $"اختر {endpointLabel} من الحسابات العامة فقط.";
                return false;
            }

            resolvedEndpoint = new ResolvedTransferEndpoint(
                generalAccount.Id,
                generalAccount.DisplayText,
                null,
                null,
                null,
                $"حساب عام · {generalAccount.DisplayText}");
            return true;
        }

        if (endpoint.SelectedPartyId is not Guid partyId)
        {
            error = $"اختر الطرف في جهة {endpointLabel}.";
            return false;
        }

        var party = GetParty(partyId);
        if (party is null || !party.IsActive)
        {
            error = $"الطرف المختار في جهة {endpointLabel} غير متاح.";
            return false;
        }

        var side = NormalizePartySide(party, endpoint.SelectedPartySide);
        if (side is null)
        {
            error = $"حدد هل الطرف في جهة {endpointLabel} سيعامل كعميل أم كمورد.";
            return false;
        }

        if (!party.SupportsSide(side.Value))
        {
            error = $"الطرف {party.NameAr} لا يدعم الجهة المختارة في {endpointLabel}.";
            return false;
        }

        var accountId = party.ResolveTransferAccountId(side.Value, ReceivableControlAccountId, PayableControlAccountId);
        var account = GetAccount(accountId);
        if (account is null)
        {
            error = $"تعذر العثور على الحساب المرتبط بالطرف {party.NameAr} في جهة {endpointLabel}.";
            return false;
        }

        resolvedEndpoint = new ResolvedTransferEndpoint(
            account.Id,
            account.DisplayText,
            party.Id,
            party.NameAr,
            side,
            $"{party.DisplayText} · {(side == PartyLedgerSide.Customer ? "عميل" : "مورد")}");
        return true;
    }
}

public sealed record ResolvedTransferEndpoint(
    Guid AccountId,
    string AccountDisplayText,
    Guid? PartyId,
    string? PartyName,
    PartyLedgerSide? PartySide,
    string DisplayText);
