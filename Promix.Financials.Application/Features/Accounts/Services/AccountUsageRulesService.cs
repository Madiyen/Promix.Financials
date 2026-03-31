using Promix.Financials.Application.Features.Accounts.Queries;
using Promix.Financials.Domain.Aggregates.Accounts;
using Promix.Financials.Domain.Enums;
using Promix.Financials.Domain.Exceptions;

namespace Promix.Financials.Application.Features.Accounts.Services;

public sealed class AccountUsageRulesService
{
    private readonly IChartOfAccountsQuery _query;

    public AccountUsageRulesService(IChartOfAccountsQuery query)
    {
        _query = query;
    }

    public async Task<AccountUsageSummaryDto> GetSummaryAsync(Guid companyId, Guid accountId, CancellationToken ct = default)
    {
        var details = await _query.GetAccountDetailsAsync(companyId, accountId);
        if (details is null)
            throw new BusinessRuleException("الحساب غير موجود.");

        return details.UsageSummary;
    }

    public async Task EnsureCanDeleteAsync(Account account, CancellationToken ct = default)
    {
        if (account.Origin == AccountOrigin.Template || account.SystemRole is not null)
            throw new BusinessRuleException("لا يمكن حذف حساب افتراضي من شجرة الحسابات.");

        var summary = await GetSummaryAsync(account.CompanyId, account.Id, ct);
        if (summary.CanDelete)
            return;

        throw new BusinessRuleException(BuildDeleteMessage(summary));
    }

    public async Task EnsureCanDeactivateAsync(Account account, CancellationToken ct = default)
    {
        if (account.Origin == AccountOrigin.Template || account.SystemRole is not null)
            throw new BusinessRuleException("لا يمكن إيقاف حساب افتراضي أو نظامي.");

        var summary = await GetSummaryAsync(account.CompanyId, account.Id, ct);
        if (summary.CanDeactivate)
            return;

        throw new BusinessRuleException(BuildDeactivateMessage(summary));
    }

    private static string BuildDeleteMessage(AccountUsageSummaryDto summary)
    {
        if (summary.BlockingReasons.Count == 0)
            return "لا يمكن حذف الحساب بسبب ارتباطاته الحالية.";

        return "لا يمكن حذف الحساب للأسباب التالية:\n- " + string.Join("\n- ", summary.BlockingReasons);
    }

    private static string BuildDeactivateMessage(AccountUsageSummaryDto summary)
    {
        if (summary.BlockingReasons.Count == 0)
            return "لا يمكن إيقاف الحساب بسبب ارتباطاته الحالية.";

        return "لا يمكن إيقاف الحساب للأسباب التالية:\n- " + string.Join("\n- ", summary.BlockingReasons);
    }
}
