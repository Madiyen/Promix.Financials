using Promix.Financials.Application.Abstractions;
using Promix.Financials.Domain.Exceptions;

namespace Promix.Financials.Application.Features.Accounts.Services;

public sealed class DeleteAccountService
{
    private readonly IAccountRepository _accounts;
    private readonly AccountUsageRulesService _usageRules;

    public DeleteAccountService(IAccountRepository accounts, AccountUsageRulesService usageRules)
    {
        _accounts = accounts;
        _usageRules = usageRules;
    }

    public async Task DeleteAsync(Guid accountId, Guid companyId, CancellationToken ct = default)
    {
        var account = await _accounts.GetByIdAsync(accountId, companyId, ct);

        if (account is null)
            throw new BusinessRuleException("الحساب غير موجود.");

        await _usageRules.EnsureCanDeleteAsync(account, ct);

        _accounts.Remove(account);
        await _accounts.SaveChangesAsync(ct);
    }
}
