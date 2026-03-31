using System;
using System.Collections.ObjectModel;

namespace Promix.Financials.UI.ViewModels.Accounts.Models;

public sealed class AccountDetailPanelVm
{
    public Guid Id { get; init; }
    public string Code { get; init; } = "—";
    public string ArabicName { get; init; } = "—";
    public string EnglishName { get; init; } = "—";
    public string ParentDisplay { get; init; } = "حساب رئيسي";
    public string TypeText { get; init; } = "—";
    public string NatureText { get; init; } = "—";
    public string ClassificationText { get; init; } = "—";
    public string CloseBehaviorText { get; init; } = "—";
    public string OriginText { get; init; } = "—";
    public string StatusText { get; init; } = "—";
    public string CurrencyText { get; init; } = "—";
    public string SystemRoleText { get; init; } = "بدون";
    public string PostingModeText { get; init; } = "—";
    public string LevelText { get; init; } = "—";
    public string NotesText { get; init; } = "لا توجد ملاحظات.";
    public string UsageHeadline { get; init; } = "لا توجد ارتباطات تشغيلية تمنع إدارة هذا الحساب حالياً.";
    public string BalanceText { get; init; } = "0.00";
    public string MovementLinesText { get; init; } = "0";
    public string LinkedPartiesText { get; init; } = "لا يوجد";
    public string DeleteRuleText { get; init; } = "يمكن حذف الحساب";
    public string DeactivateRuleText { get; init; } = "يمكن إيقاف الحساب";
    public bool CanDelete { get; init; }
    public bool CanDeactivate { get; init; }
    public bool IsSystem { get; init; }
    public bool IsActive { get; init; }
    public ObservableCollection<string> UsageBadges { get; } = [];
    public ObservableCollection<string> BlockingReasons { get; } = [];
    public ObservableCollection<AccountDetailChildVm> Children { get; } = [];
}

public sealed class AccountDetailChildVm
{
    public Guid Id { get; init; }
    public string Code { get; init; } = "—";
    public string ArabicName { get; init; } = "—";
    public string TypeText { get; init; } = "—";
    public string OriginText { get; init; } = "—";
    public string StatusText { get; init; } = "—";
}
