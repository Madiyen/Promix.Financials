using Promix.Financials.Application.Abstractions;
using Promix.Financials.Application.Features.Parties.Commands;
using Promix.Financials.Domain.Aggregates.Parties;
using Promix.Financials.Domain.Enums;
using Promix.Financials.Domain.Exceptions;

namespace Promix.Financials.Application.Features.Parties.Services;

public sealed class CreatePartyService
{
    private readonly IPartyRepository _parties;
    private readonly IUserContext _userContext;
    private readonly PartyAccountProvisioningService _accountProvisioning;

    public CreatePartyService(
        IPartyRepository parties,
        IUserContext userContext,
        PartyAccountProvisioningService accountProvisioning)
    {
        _parties = parties;
        _userContext = userContext;
        _accountProvisioning = accountProvisioning;
    }

    public async Task<Guid> CreateAsync(CreatePartyCommand command, CancellationToken ct = default)
    {
        if (!_userContext.IsAuthenticated)
            throw new BusinessRuleException("User is not authenticated.");

        var code = await ResolveCodeAsync(command.CompanyId, command.Code, ct);
        var provisionedAccounts = await _accountProvisioning.ProvisionLinkedAccountsAsync(
            command.CompanyId,
            command.NameAr,
            command.TypeFlags,
            excludePartyId: null,
            ct: ct);

        var party = new Party(
            command.CompanyId,
            code,
            command.NameAr,
            command.NameEn,
            command.TypeFlags,
            PartyLedgerMode.LegacyLinkedAccounts,
            provisionedAccounts.ReceivableAccountId,
            provisionedAccounts.PayableAccountId,
            command.Phone,
            command.Mobile,
            command.Email,
            command.TaxNo,
            command.Address,
            command.Notes,
            true);

        await _parties.AddAsync(party, ct);
        await _parties.SaveChangesAsync(ct);
        return party.Id;
    }

    private async Task<string> ResolveCodeAsync(Guid companyId, string requestedCode, CancellationToken ct)
    {
        var code = string.IsNullOrWhiteSpace(requestedCode)
            ? await GenerateNextCodeAsync(companyId, ct)
            : requestedCode.Trim().ToUpperInvariant();

        if (await _parties.CodeExistsAsync(companyId, code, null, ct))
            throw new BusinessRuleException("Party code already exists.");

        return code;
    }

    private async Task<string> GenerateNextCodeAsync(Guid companyId, CancellationToken ct)
    {
        for (var index = 1; index < 100000; index++)
        {
            var code = $"PRT{index:0000}";
            if (!await _parties.CodeExistsAsync(companyId, code, null, ct))
                return code;
        }

        throw new BusinessRuleException("Unable to generate the next party code.");
    }

    internal static (Guid? ReceivableAccountId, Guid? PayableAccountId) NormalizeLinkedAccounts(
        PartyTypeFlags typeFlags,
        PartyLedgerMode ledgerMode,
        Guid? receivableAccountId,
        Guid? payableAccountId)
    {
        if (ledgerMode == PartyLedgerMode.Subledger)
            return (null, null);

        return typeFlags switch
        {
            PartyTypeFlags.Customer => (receivableAccountId, null),
            PartyTypeFlags.Vendor => (null, payableAccountId),
            PartyTypeFlags.Both => (receivableAccountId, payableAccountId),
            _ => (receivableAccountId, payableAccountId)
        };
    }
}
