using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Promix.Financials.Domain.Enums;

namespace Promix.Financials.Application.Features.Accounts.Queries;

public sealed record AccountFlatDto(
    Guid Id,
    Guid? ParentId,
    string Code,
    string ArabicName,
    AccountNature Nature,
    AccountClass Classification,
    AccountCloseBehavior CloseBehavior,
    bool IsPosting,
    bool AllowManualPosting,
    bool AllowChildren,
    bool IsSystem,
    AccountOrigin Origin,
    bool IsActive,
    string? CurrencyCode,
    string? SystemRole);

public sealed record AccountChildPreviewDto(
    Guid Id,
    string Code,
    string ArabicName,
    bool IsPosting,
    bool IsActive,
    AccountOrigin Origin);

public sealed record AccountUsageSummaryDto(
    int ChildAccountsCount,
    int PostedMovementLinesCount,
    int LinkedPartiesCount,
    decimal CurrentBalance,
    bool IsSalesLinked,
    bool IsInventoryLinked,
    bool IsTaxLinked,
    bool IsYearCloseLinked,
    IReadOnlyList<string> LinkedPartyNames,
    IReadOnlyList<string> BlockingReasons,
    bool CanDelete,
    bool CanDeactivate);

public sealed record AccountDetailDto(
    Guid Id,
    Guid? ParentId,
    string Code,
    string ArabicName,
    string? EnglishName,
    AccountNature Nature,
    AccountClass Classification,
    AccountCloseBehavior CloseBehavior,
    bool IsPosting,
    bool AllowManualPosting,
    bool AllowChildren,
    bool IsSystem,
    AccountOrigin Origin,
    bool IsActive,
    string? CurrencyCode,
    string? SystemRole,
    string? Notes,
    int Level,
    string? ParentCode,
    string? ParentName,
    IReadOnlyList<AccountChildPreviewDto> Children,
    AccountUsageSummaryDto UsageSummary);

public sealed record AccountWorkspaceRowDto(
    Guid Id,
    Guid? ParentId,
    string Code,
    string ArabicName,
    AccountNature Nature,
    AccountClass Classification,
    AccountCloseBehavior CloseBehavior,
    bool IsPosting,
    bool AllowManualPosting,
    bool AllowChildren,
    bool IsSystem,
    AccountOrigin Origin,
    bool IsActive,
    string? CurrencyCode,
    string? SystemRole,
    string? ParentCode,
    string? ParentName,
    decimal Balance,
    DateOnly? LastMovementDate,
    int ChildAccountsCount);

public sealed record AccountClassBreakdownDto(
    AccountClass Classification,
    int AccountsCount,
    decimal TotalBalance);

public sealed record AccountsWorkspaceSummaryDto(
    int TotalAccounts,
    int TemplateAccounts,
    int PartyAccounts,
    int ManualAccounts,
    int ActiveAccounts,
    IReadOnlyList<AccountClassBreakdownDto> ClassBreakdown);

public sealed record AccountsWorkspaceDto(
    IReadOnlyList<AccountWorkspaceRowDto> Rows,
    AccountsWorkspaceSummaryDto Summary);

public interface IChartOfAccountsQuery
{
    Task<IReadOnlyList<AccountFlatDto>> GetAccountsAsync(Guid companyId);
    Task<AccountDetailDto?> GetAccountDetailsAsync(Guid companyId, Guid accountId);
    Task<AccountsWorkspaceDto> GetAccountsWorkspaceAsync(Guid companyId);
}
