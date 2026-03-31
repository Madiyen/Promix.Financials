using Promix.Financials.Application.Abstractions;
using Promix.Financials.Application.Features.Accounts;
using Promix.Financials.Domain.Aggregates.Accounts;
using Promix.Financials.Domain.Enums;
using Promix.Financials.Domain.Exceptions;

namespace Promix.Financials.Application.Features.Parties.Services;

public sealed class PartyAccountProvisioningService
{
    private readonly IAccountRepository _accounts;
    private readonly IPartyRepository _parties;

    public PartyAccountProvisioningService(IAccountRepository accounts, IPartyRepository parties)
    {
        _accounts = accounts;
        _parties = parties;
    }

    public async Task<ProvisionedPartyAccounts> ProvisionLinkedAccountsAsync(
        Guid companyId,
        string partyNameAr,
        PartyTypeFlags typeFlags,
        Guid? existingReceivableAccountId = null,
        Guid? existingPayableAccountId = null,
        Guid? excludePartyId = null,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(partyNameAr))
            throw new BusinessRuleException("اسم الطرف مطلوب قبل تجهيز حساباته.");

        var allAccounts = await _accounts.GetAllAsync(companyId, ct);

        var receivableAccount = typeFlags.HasFlag(PartyTypeFlags.Customer)
            ? await ResolveOrCreateAccountAsync(
                companyId,
                allAccounts,
                ResolveParent(allAccounts, "121", "حسابات العملاء"),
                partyNameAr,
                BuildNameCandidates(partyNameAr, "عميل", typeFlags == PartyTypeFlags.Both),
                AccountNature.Debit,
                existingReceivableAccountId,
                excludePartyId,
                ct)
            : null;

        var payableAccount = typeFlags.HasFlag(PartyTypeFlags.Vendor)
            ? await ResolveOrCreateAccountAsync(
                companyId,
                allAccounts,
                ResolveParent(allAccounts, "221", "حسابات الموردين"),
                partyNameAr,
                BuildNameCandidates(partyNameAr, "مورد", typeFlags == PartyTypeFlags.Both),
                AccountNature.Credit,
                existingPayableAccountId,
                excludePartyId,
                ct)
            : null;

        return new ProvisionedPartyAccounts(receivableAccount?.Id, payableAccount?.Id);
    }

    private async Task<Account> ResolveOrCreateAccountAsync(
        Guid companyId,
        IReadOnlyList<Account> allAccounts,
        Account parent,
        string partyNameAr,
        IReadOnlyList<string> nameCandidates,
        AccountNature nature,
        Guid? existingAccountId,
        Guid? excludePartyId,
        CancellationToken ct)
    {
        if (existingAccountId is Guid currentAccountId)
        {
            var existing = allAccounts.FirstOrDefault(x => x.Id == currentAccountId)
                ?? throw new BusinessRuleException("الحساب المرتبط الحالي لم يعد موجودًا.");

            await EnsureAccountCanBeOwnedAsync(companyId, existing, excludePartyId, ct);
            return existing;
        }

        foreach (var candidate in nameCandidates)
        {
            var normalizedCandidate = AccountNameNormalizer.Normalize(candidate);
            if (string.IsNullOrWhiteSpace(normalizedCandidate))
                continue;

            var underParent = allAccounts.FirstOrDefault(x =>
                x.ParentId == parent.Id &&
                x.IsPosting &&
                AccountNameNormalizer.Normalize(x.NameAr) == normalizedCandidate);

            if (underParent is not null)
            {
                await EnsureAccountCanBeOwnedAsync(companyId, underParent, excludePartyId, ct);
                return underParent;
            }

            var duplicateElsewhere = allAccounts.Any(x =>
                x.Id != parent.Id &&
                AccountNameNormalizer.Normalize(x.NameAr) == normalizedCandidate);

            if (duplicateElsewhere)
                continue;

            var created = new Account(
                companyId: companyId,
                code: GenerateNextChildCode(parent.Id, parent.Code, allAccounts),
                nameAr: candidate,
                nameEn: null,
                nature: nature,
                isPosting: true,
                parentId: parent.Id,
                currencyCode: null,
                systemRole: null,
                notes: "أُنشئ تلقائياً عند إضافة طرف جديد.",
                isActive: true,
                origin: AccountOrigin.PartyGenerated);

            await _accounts.AddAsync(created, ct);
            return created;
        }

        throw new BusinessRuleException(
            $"تعذر إنشاء حساب مناسب للطرف \"{partyNameAr}\" دون الوقوع في تعارض بالأسماء داخل هذه الشركة.");
    }

    private async Task EnsureAccountCanBeOwnedAsync(Guid companyId, Account account, Guid? excludePartyId, CancellationToken ct)
    {
        if (!account.IsPosting)
            throw new BusinessRuleException($"الحساب {account.Code} ليس حساباً نهائياً.");

        if (!account.IsActive)
            throw new BusinessRuleException($"الحساب {account.Code} غير نشط.");

        var linkedParties = await _parties.GetByLinkedAccountAsync(companyId, account.Id, excludePartyId, ct);
        if (linkedParties.Count > 0)
        {
            var owner = linkedParties[0];
            throw new BusinessRuleException($"الحساب {account.Code} مرتبط مسبقاً بالطرف {owner.NameAr} ولا يمكن إعادة استخدامه.");
        }
    }

    private static Account ResolveParent(IReadOnlyList<Account> accounts, string code, string label)
    {
        var parent = accounts.FirstOrDefault(x => x.Code == code)
            ?? throw new BusinessRuleException($"تعذر العثور على الأصل {code} الخاص بـ {label}.");

        if (parent.IsPosting)
            throw new BusinessRuleException($"الحساب {code} يجب أن يكون أصلاً تجميعياً لا حساباً نهائياً.");

        return parent;
    }

    private static IReadOnlyList<string> BuildNameCandidates(string partyNameAr, string roleSuffix, bool suffixOnly)
    {
        var baseName = partyNameAr.Trim();
        var suffixed = $"{baseName} - {roleSuffix}";

        return suffixOnly
            ? [suffixed]
            : [baseName, suffixed];
    }

    private static string GenerateNextChildCode(Guid parentId, string parentCode, IReadOnlyList<Account> allAccounts)
    {
        var maxNumber = allAccounts
            .Where(x => x.ParentId == parentId && x.Code.StartsWith(parentCode, StringComparison.Ordinal))
            .Select(x => x.Code[parentCode.Length..])
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(x => int.TryParse(x, out var value) ? value : 0)
            .DefaultIfEmpty(0)
            .Max();

        return $"{parentCode}{maxNumber + 1}";
    }
}

public sealed record ProvisionedPartyAccounts(Guid? ReceivableAccountId, Guid? PayableAccountId);
