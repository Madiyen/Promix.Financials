using Promix.Financials.Application.Abstractions;
using Promix.Financials.Domain.Aggregates.Accounts;
using Promix.Financials.Domain.Aggregates.Parties;
using Promix.Financials.Domain.Enums;
using Promix.Financials.Domain.Exceptions;

namespace Promix.Financials.Application.Features.Parties.Services;

public sealed class PartyPostingRulesService
{
    public const string ReceivableControlRole = "ARControl";
    public const string PayableControlRole = "APControl";

    private readonly IAccountRepository _accounts;
    private readonly IPartyRepository _parties;

    public PartyPostingRulesService(IAccountRepository accounts, IPartyRepository parties)
    {
        _accounts = accounts;
        _parties = parties;
    }

    public async Task<PartyControlAccounts> GetControlAccountsAsync(Guid companyId, CancellationToken ct = default)
    {
        var receivable = await _accounts.GetBySystemRoleAsync(companyId, ReceivableControlRole, ct)
            ?? throw new BusinessRuleException("تعذر العثور على حساب ضبط العملاء ARControl.");
        var payable = await _accounts.GetBySystemRoleAsync(companyId, PayableControlRole, ct)
            ?? throw new BusinessRuleException("تعذر العثور على حساب ضبط الموردين APControl.");

        return new PartyControlAccounts(receivable.Id, payable.Id, receivable, payable);
    }

    public async Task<string?> ResolvePartyNameAsync(
        Guid companyId,
        Guid accountId,
        Guid? partyId,
        string? partyName,
        CancellationToken ct = default)
    {
        var account = await _accounts.GetByIdAsync(accountId, companyId, ct)
            ?? throw new BusinessRuleException("One of the selected accounts was not found.");

        var isReceivableControl = string.Equals(account.SystemRole, ReceivableControlRole, StringComparison.OrdinalIgnoreCase);
        var isPayableControl = string.Equals(account.SystemRole, PayableControlRole, StringComparison.OrdinalIgnoreCase);
        var linkedOwners = await _parties.GetByLinkedAccountAsync(companyId, accountId, null, ct);
        var linkedOwner = linkedOwners.FirstOrDefault();

        if (partyId is not Guid resolvedPartyId || resolvedPartyId == Guid.Empty)
        {
            if (isReceivableControl || isPayableControl)
                throw new BusinessRuleException("لا يمكن التقييد على حسابات ضبط العملاء أو الموردين بدون اختيار طرف.");

            if (linkedOwner is not null && linkedOwner.LedgerMode == PartyLedgerMode.LegacyLinkedAccounts)
                throw new BusinessRuleException("هذا الحساب مرتبط بطرف. اختر الطرف مباشرة أو استخدم حساباً عاماً آخر.");

            return partyName;
        }

        var party = await _parties.GetByIdAsync(companyId, resolvedPartyId, ct)
            ?? throw new BusinessRuleException("The selected party was not found.");

        if (!party.IsActive)
            throw new BusinessRuleException("The selected party is inactive.");

        if (party.LedgerMode == PartyLedgerMode.LegacyLinkedAccounts)
        {
            var matchesReceivable = party.IsCustomer && party.ReceivableAccountId == account.Id;
            var matchesPayable = party.IsVendor && party.PayableAccountId == account.Id;

            if (matchesReceivable || matchesPayable)
                return party.NameAr;

            if (isReceivableControl || isPayableControl)
            {
                throw new BusinessRuleException(
                    "هذا الطرف يعمل بالحسابات المرتبطة داخل شجرة الحسابات. استخدم الحساب المرتبط به مباشرة، وليس حساب الضبط.");
            }

            if (linkedOwner is not null && linkedOwner.Id != party.Id)
            {
                throw new BusinessRuleException(
                    $"الحساب المختار مرتبط بالطرف {linkedOwner.NameAr} ولا يمكن استخدامه مع طرف آخر.");
            }

            throw new BusinessRuleException("عند اختيار طرف يجب استخدام الحساب المرتبط بهذا الطرف فقط.");
        }

        if (linkedOwner is not null)
        {
            throw new BusinessRuleException(
                "هذا الحساب مرتبط بطرف من النموذج القديم. استخدم الطرف نفسه أو اختر حساباً عاماً آخر.");
        }

        if (isReceivableControl)
        {
            if (!party.IsCustomer)
                throw new BusinessRuleException("الطرف المختار ليس من نوع عميل ولا يمكن ربطه بحساب ضبط العملاء.");

            return party.NameAr;
        }

        if (isPayableControl)
        {
            if (!party.IsVendor)
                throw new BusinessRuleException("الطرف المختار ليس من نوع مورد ولا يمكن ربطه بحساب ضبط الموردين.");

            return party.NameAr;
        }

        throw new BusinessRuleException("عند اختيار طرف يجب أن يكون الحساب المختار هو الحساب المرتبط به أو حساب الضبط المناسب لوضعه الحالي.");
    }

    public async Task<bool> IsLegacyLinkedAccountAsync(Guid companyId, Guid accountId, CancellationToken ct = default)
    {
        var owners = await _parties.GetByLinkedAccountAsync(companyId, accountId, null, ct);
        return owners.Any(x => x.LedgerMode == PartyLedgerMode.LegacyLinkedAccounts);
    }
}

public sealed record PartyControlAccounts(
    Guid ReceivableAccountId,
    Guid PayableAccountId,
    Account ReceivableAccount,
    Account PayableAccount);
