using Promix.Financials.Application.Abstractions;
using Promix.Financials.Application.Features.Parties.Commands;
using Promix.Financials.Application.Features.Parties.Queries;
using Promix.Financials.Domain.Enums;
using Promix.Financials.Domain.Exceptions;

namespace Promix.Financials.Application.Features.Parties.Services;

public sealed class EditPartyService
{
    private readonly IPartyRepository _parties;
    private readonly IPartyQuery _partyQuery;
    private readonly ICompanyJournalLockRepository _companies;
    private readonly IUserContext _userContext;
    private readonly IDateTimeProvider _clock;
    private readonly PartyAccountProvisioningService _accountProvisioning;

    public EditPartyService(
        IPartyRepository parties,
        IPartyQuery partyQuery,
        ICompanyJournalLockRepository companies,
        IUserContext userContext,
        IDateTimeProvider clock,
        PartyAccountProvisioningService accountProvisioning)
    {
        _parties = parties;
        _partyQuery = partyQuery;
        _companies = companies;
        _userContext = userContext;
        _clock = clock;
        _accountProvisioning = accountProvisioning;
    }

    public async Task EditAsync(EditPartyCommand command, CancellationToken ct = default)
    {
        if (!_userContext.IsAuthenticated)
            throw new BusinessRuleException("User is not authenticated.");

        var party = await _parties.GetByIdAsync(command.CompanyId, command.PartyId, ct)
            ?? throw new BusinessRuleException("Party was not found.");

        var normalizedCode = command.Code.Trim().ToUpperInvariant();
        if (await _parties.CodeExistsAsync(command.CompanyId, normalizedCode, command.PartyId, ct))
            throw new BusinessRuleException("Party code already exists.");

        var requestedLedgerMode = party.LedgerMode == PartyLedgerMode.Subledger
            ? PartyLedgerMode.Subledger
            : PartyLedgerMode.LegacyLinkedAccounts;

        if (requestedLedgerMode != party.LedgerMode)
            throw new BusinessRuleException("تحويل الطرف بين الربط القديم والدفتر الفرعي ليس متاحاً من شاشة التعديل الحالية.");

        var requestedTypeFlags = command.TypeFlags;
        Guid? receivableAccountId = party.ReceivableAccountId;
        Guid? payableAccountId = party.PayableAccountId;

        if (party.LedgerMode == PartyLedgerMode.LegacyLinkedAccounts)
        {
            var requiresAccountChange =
                party.TypeFlags != requestedTypeFlags ||
                party.ReceivableAccountId != command.ReceivableAccountId ||
                party.PayableAccountId != command.PayableAccountId;

            if (requiresAccountChange)
                await EnsurePartyHasNoPostedHistoryAsync(command.CompanyId, party.Id, ct);

            var provisionedAccounts = await _accountProvisioning.ProvisionLinkedAccountsAsync(
                command.CompanyId,
                command.NameAr,
                requestedTypeFlags,
                party.ReceivableAccountId,
                party.PayableAccountId,
                party.Id,
                ct);

            receivableAccountId = provisionedAccounts.ReceivableAccountId;
            payableAccountId = provisionedAccounts.PayableAccountId;
        }
        else
        {
            receivableAccountId = null;
            payableAccountId = null;
        }

        party.Update(
            normalizedCode,
            command.NameAr,
            command.NameEn,
            requestedTypeFlags,
            requestedLedgerMode,
            receivableAccountId,
            payableAccountId,
            command.Phone,
            command.Mobile,
            command.Email,
            command.TaxNo,
            command.Address,
            command.Notes,
            command.IsActive);

        await _parties.SaveChangesAsync(ct);
    }

    private async Task EnsurePartyHasNoPostedHistoryAsync(Guid companyId, Guid partyId, CancellationToken ct)
    {
        var company = await _companies.GetByIdAsync(companyId, ct)
            ?? throw new BusinessRuleException("Company was not found.");

        var statement = await _partyQuery.GetStatementAsync(
            companyId,
            partyId,
            company.AccountingStartDate,
            DateOnly.FromDateTime(_clock.UtcNow.Date),
            ct);

        if (statement is null)
            return;

        var hasPostedMovements = statement.Movements.Count > 0;
        var hasOpenItems = statement.OpenItems.Count > 0;
        var hasOutstandingBalance = statement.OpeningBalance != 0m || statement.ClosingBalance != 0m;

        if (hasPostedMovements || hasOpenItems || hasOutstandingBalance)
        {
            throw new BusinessRuleException(
                "لا يمكن تغيير نوع الطرف أو حساباته المرتبطة لأنه يملك حركات مرحّلة أو ذمة مفتوحة أو رصيداً قائماً.");
        }
    }
}
