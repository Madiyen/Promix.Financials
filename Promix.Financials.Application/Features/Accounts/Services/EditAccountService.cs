using Promix.Financials.Application.Abstractions;   // ✅ نفس namespace الـ Interface
using Promix.Financials.Application.Features.Accounts.Commands;
using Promix.Financials.Domain.Exceptions;

namespace Promix.Financials.Application.Features.Accounts.Services;

public sealed class EditAccountService
{
    private readonly IAccountRepository _repo;
    private readonly AccountUsageRulesService _usageRules;

    public EditAccountService(IAccountRepository repo, AccountUsageRulesService usageRules)
    {
        _repo = repo;
        _usageRules = usageRules;
    }

    public async Task EditAsync(EditAccountCommand cmd, CancellationToken ct = default)
    {
        var account = await _repo.GetByIdAsync(cmd.AccountId, cmd.CompanyId, ct);

        if (account is null)
            throw new BusinessRuleException("الحساب غير موجود.");

        // ✅ القاعدة: لا تعديل على حسابات النظام
        if (account.SystemRole is not null)
            throw new BusinessRuleException("لا يمكن تعديل حساب النظام.");

        var normalizedName = AccountNameNormalizer.Normalize(cmd.ArabicName);
        var allAccounts = await _repo.GetAllAsync(cmd.CompanyId, ct);
        if (allAccounts.Any(x => x.Id != cmd.AccountId && AccountNameNormalizer.Normalize(x.NameAr) == normalizedName))
            throw new BusinessRuleException("يوجد حساب آخر بنفس الاسم داخل هذه الشركة.");

        if (account.IsActive && !cmd.IsActive)
            await _usageRules.EnsureCanDeactivateAsync(account, ct);

        account.Update(cmd.ArabicName, cmd.EnglishName, cmd.IsActive, cmd.Notes);

        await _repo.SaveChangesAsync(ct);
    }
}
