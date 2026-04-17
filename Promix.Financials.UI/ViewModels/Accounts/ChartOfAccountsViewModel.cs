using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using Promix.Financials.Application.Features.Accounts.Queries;
using Promix.Financials.Domain.Enums;
using Promix.Financials.UI.ViewModels.Accounts.Models;

namespace Promix.Financials.UI.ViewModels.Accounts;

public sealed class ChartOfAccountsViewModel : INotifyPropertyChanged
{
    private const string AllFilter = "الكل";

    private readonly IChartOfAccountsQuery _query;
    private readonly Dictionary<Guid, bool> _groupExpansionState = [];
    private List<AccountFlatDto> _flatAccounts = [];
    private List<AccountNodeVm> _fullTree = [];
    private List<AccountWorkspaceRowDto> _workspaceRows = [];
    private Guid? _companyId;
    private Guid? _selectedAccountId;
    private readonly SemaphoreSlim _selectionGate = new(1, 1);
    private int _selectionRequestVersion;
    private bool _isBusy;
    private string? _errorMessage;
    private string _searchText = string.Empty;
    private string _selectedOriginFilter = AllFilter;
    private string _selectedPostingFilter = AllFilter;
    private string _selectedClassificationFilter = AllFilter;
    private bool _showSystemAccountsOnly;
    private bool _showInactiveAccountsOnly;
    private bool _showPartyAccountsOnly;
    private AccountDetailPanelVm? _selectedAccountDetail;
    private AccountWorkspaceTab _selectedTab = AccountWorkspaceTab.Accounts;

    public ObservableCollection<AccountGroupVm> AccountGroups { get; } = [];
    public ObservableCollection<AccountNodeVm> AccountTree { get; } = [];
    public ObservableCollection<string> OriginFilters { get; } =
    [
        AllFilter,
        "افتراضي",
        "طرف",
        "يدوي"
    ];
    public ObservableCollection<string> PostingFilters { get; } =
    [
        AllFilter,
        "حركي",
        "تجميعي"
    ];
    public ObservableCollection<string> ClassificationFilters { get; } =
    [
        AllFilter,
        "أصول",
        "خصوم",
        "حقوق ملكية",
        "إيرادات",
        "مصروفات"
    ];

    public AccountsSummaryVm Summary { get; } = new();

    public bool IsBusy
    {
        get => _isBusy;
        private set
        {
            if (_isBusy == value) return;
            _isBusy = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(DetailPlaceholderText));
            RefreshCommand.RaiseCanExecuteChanged();
        }
    }

    public string? ErrorMessage
    {
        get => _errorMessage;
        private set
        {
            if (_errorMessage == value) return;
            _errorMessage = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(HasErrorMessage));
        }
    }

    public bool HasErrorMessage => !string.IsNullOrWhiteSpace(ErrorMessage);
    public bool HasAnyAccounts => AccountGroups.Count > 0 || AccountTree.Count > 0;

    public string SearchText
    {
        get => _searchText;
        set
        {
            if (_searchText == value) return;
            _searchText = value;
            OnPropertyChanged();
            ApplyFilters();
        }
    }

    public string SelectedOriginFilter
    {
        get => _selectedOriginFilter;
        set
        {
            if (_selectedOriginFilter == value) return;
            _selectedOriginFilter = value;
            OnPropertyChanged();
            ApplyFilters();
        }
    }

    public string SelectedPostingFilter
    {
        get => _selectedPostingFilter;
        set
        {
            if (_selectedPostingFilter == value) return;
            _selectedPostingFilter = value;
            OnPropertyChanged();
            ApplyFilters();
        }
    }

    public string SelectedClassificationFilter
    {
        get => _selectedClassificationFilter;
        set
        {
            if (_selectedClassificationFilter == value) return;
            _selectedClassificationFilter = value;
            OnPropertyChanged();
            ApplyFilters();
        }
    }

    public bool ShowSystemAccountsOnly
    {
        get => _showSystemAccountsOnly;
        set
        {
            if (_showSystemAccountsOnly == value) return;
            _showSystemAccountsOnly = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(SystemFilterLabel));
            ApplyFilters();
        }
    }

    public bool ShowInactiveAccountsOnly
    {
        get => _showInactiveAccountsOnly;
        set
        {
            if (_showInactiveAccountsOnly == value) return;
            _showInactiveAccountsOnly = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(InactiveFilterLabel));
            ApplyFilters();
        }
    }

    public bool ShowPartyAccountsOnly
    {
        get => _showPartyAccountsOnly;
        set
        {
            if (_showPartyAccountsOnly == value) return;
            _showPartyAccountsOnly = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(PartyFilterLabel));
            ApplyFilters();
        }
    }

    public AccountDetailPanelVm? SelectedAccountDetail
    {
        get => _selectedAccountDetail;
        private set
        {
            if (_selectedAccountDetail == value) return;
            _selectedAccountDetail = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(HasSelectedAccount));
            OnPropertyChanged(nameof(DetailPlaceholderText));
        }
    }

    public bool HasSelectedAccount => SelectedAccountDetail is not null;

    public int SelectedWorkspaceTabIndex
    {
        get => (int)_selectedTab;
        set
        {
            var next = (AccountWorkspaceTab)value;
            if (_selectedTab == next) return;
            _selectedTab = next;
            OnPropertyChanged();
        }
    }

    public string SystemFilterLabel => ShowSystemAccountsOnly ? "الحسابات النظامية فقط" : "النظام";
    public string InactiveFilterLabel => ShowInactiveAccountsOnly ? "الموقوفة فقط" : "غير النشطة";
    public string PartyFilterLabel => ShowPartyAccountsOnly ? "حسابات الأطراف فقط" : "الأطراف";
    public string DetailPlaceholderText => IsBusy
        ? "جاري تحميل بيانات الحساب..."
        : "اختر حسابًا من تبويب الحسابات أو من العرض الشجري لعرض التفاصيل والاستخدامات.";

    public AsyncRelayCommand RefreshCommand { get; }

    public ChartOfAccountsViewModel(IChartOfAccountsQuery query)
    {
        _query = query;
        RefreshCommand = new AsyncRelayCommand(RefreshAsync, () => !IsBusy && _companyId is not null);
        UpdateSummary(Array.Empty<AccountWorkspaceRowDto>());
    }

    public async Task InitializeAsync(Guid companyId)
    {
        _companyId = companyId;
        RefreshCommand.RaiseCanExecuteChanged();
        await LoadAsync(companyId);
    }

    public void HighlightAccount(Guid accountId)
    {
        if (accountId == Guid.Empty)
            return;

        _selectedAccountId = accountId;
        ApplySelectionState(AccountTree, _selectedAccountId);
        ApplySelectionState(AccountGroups, _selectedAccountId);
    }

    public async Task<AccountDetailPanelVm?> SelectAccountAsync(Guid accountId)
    {
        if (_companyId is null || accountId == Guid.Empty)
            return null;

        var requestVersion = Interlocked.Increment(ref _selectionRequestVersion);
        HighlightAccount(accountId);

        await _selectionGate.WaitAsync();
        try
        {
            var details = await _query.GetAccountDetailsAsync(_companyId.Value, accountId);
            var panel = details is null ? null : MapToDetailPanel(details);

            if (requestVersion == _selectionRequestVersion)
                SelectedAccountDetail = panel;

            return panel;
        }
        finally
        {
            _selectionGate.Release();
        }
    }

    private async Task RefreshAsync()
    {
        if (IsBusy || _companyId is null)
            return;

        await LoadAsync(_companyId.Value);
    }

    private async Task LoadAsync(Guid companyId)
    {
        IsBusy = true;
        ErrorMessage = null;

        try
        {
            var workspace = await _query.GetAccountsWorkspaceAsync(companyId);
            _workspaceRows = workspace.Rows.OrderBy(x => x.Code, StringComparer.OrdinalIgnoreCase).ToList();
            _flatAccounts = _workspaceRows
                .Select(x => new AccountFlatDto(
                    x.Id,
                    x.ParentId,
                    x.Code,
                    x.ArabicName,
                    x.Nature,
                    x.Classification,
                    x.CloseBehavior,
                    x.IsPosting,
                    x.AllowManualPosting,
                    x.AllowChildren,
                    x.IsSystem,
                    x.Origin,
                    x.IsActive,
                    x.CurrencyCode,
                    x.SystemRole))
                .ToList();

            _fullTree = BuildTree(_flatAccounts);
            ApplyFilters();

            var firstVisibleId = AccountGroups
                .SelectMany(x => x.Accounts)
                .Select(x => (Guid?)x.Id)
                .FirstOrDefault()
                ?? AccountTree.Select(x => (Guid?)x.Id).FirstOrDefault();

            if (_selectedAccountId.HasValue && _flatAccounts.Any(x => x.Id == _selectedAccountId.Value))
            {
                await SelectAccountAsync(_selectedAccountId.Value);
            }
            else if (firstVisibleId.HasValue)
            {
                await SelectAccountAsync(firstVisibleId.Value);
            }
            else
            {
                SelectedAccountDetail = null;
            }
        }
        catch (Exception ex)
        {
            AccountGroups.Clear();
            AccountTree.Clear();
            OnPropertyChanged(nameof(HasAnyAccounts));
            _flatAccounts = [];
            _workspaceRows = [];
            _fullTree = [];
            SelectedAccountDetail = null;
            UpdateSummary(Array.Empty<AccountWorkspaceRowDto>());
            ErrorMessage = ex.Message;
        }
        finally
        {
            IsBusy = false;
        }
    }

    private void ApplyFilters()
    {
        var filteredRows = _workspaceRows
            .Where(RowMatchesActiveFilters)
            .OrderBy(x => x.ParentCode ?? x.Code, StringComparer.OrdinalIgnoreCase)
            .ThenBy(x => x.Code, StringComparer.OrdinalIgnoreCase)
            .ToList();

        RebindGroups(filteredRows);

        if (_fullTree.Count == 0)
        {
            AccountTree.Clear();
            OnPropertyChanged(nameof(HasAnyAccounts));
        }
        else
        {
            var filteredTree = FilterTree(_fullTree, NodeMatchesActiveFilters);
            RebindTree(filteredTree);
        }

        UpdateSummary(filteredRows);
    }

    private bool RowMatchesActiveFilters(AccountWorkspaceRowDto row)
    {
        if (!string.IsNullOrWhiteSpace(SearchText))
        {
            var search = SearchText.Trim();
            var selfMatch = row.Code.Contains(search, StringComparison.OrdinalIgnoreCase)
                            || row.ArabicName.Contains(search, StringComparison.OrdinalIgnoreCase)
                            || (row.ParentCode?.Contains(search, StringComparison.OrdinalIgnoreCase) ?? false)
                            || (row.ParentName?.Contains(search, StringComparison.OrdinalIgnoreCase) ?? false)
                            || (row.SystemRole?.Contains(search, StringComparison.OrdinalIgnoreCase) ?? false);
            if (!selfMatch)
                return false;
        }

        if (SelectedOriginFilter != AllFilter)
        {
            var requiredOrigin = SelectedOriginFilter switch
            {
                "افتراضي" => AccountOrigin.Template,
                "طرف" => AccountOrigin.PartyGenerated,
                _ => AccountOrigin.Manual
            };

            if (row.Origin != requiredOrigin)
                return false;
        }

        if (SelectedPostingFilter == "حركي" && !row.IsPosting)
            return false;

        if (SelectedPostingFilter == "تجميعي" && row.IsPosting)
            return false;

        if (SelectedClassificationFilter != AllFilter)
        {
            var requiredClass = SelectedClassificationFilter switch
            {
                "أصول" => AccountClass.Assets,
                "خصوم" => AccountClass.Liabilities,
                "حقوق ملكية" => AccountClass.Equity,
                "إيرادات" => AccountClass.Revenue,
                _ => AccountClass.Expenses
            };

            if (row.Classification != requiredClass)
                return false;
        }

        if (ShowSystemAccountsOnly && !row.IsSystem)
            return false;

        if (ShowInactiveAccountsOnly && row.IsActive)
            return false;

        if (ShowPartyAccountsOnly && row.Origin != AccountOrigin.PartyGenerated)
            return false;

        return true;
    }

    private bool NodeMatchesActiveFilters(AccountNodeVm node)
        => RowMatchesActiveFilters(new AccountWorkspaceRowDto(
            node.Id,
            node.ParentId,
            node.Code,
            node.ArabicName,
            node.Nature,
            node.Classification,
            node.CloseBehavior,
            node.IsPosting,
            node.AllowManualPosting,
            node.AllowChildren,
            node.IsSystem,
            node.Origin,
            node.IsActive,
            node.CurrencyCode,
            node.SystemRole,
            null,
            null,
            0m,
            null,
            node.Children.Count));

    private void RebindGroups(IReadOnlyList<AccountWorkspaceRowDto> filteredRows)
    {
        CaptureGroupExpansionState();
        AccountGroups.Clear();

        var grouped = filteredRows
            .GroupBy(x => x.ParentId ?? x.Id)
            .Select(group =>
            {
                var first = group.First();
                var parentCode = first.ParentId.HasValue ? first.ParentCode ?? first.Code : first.Code;
                var parentName = first.ParentId.HasValue ? first.ParentName ?? first.ArabicName : first.ArabicName;

                var vm = new AccountGroupVm
                {
                    ParentId = group.Key,
                    ParentCode = parentCode,
                    ParentName = parentName,
                    HeaderBalanceText = group.Sum(x => x.Balance).ToString("+#,##0.00;-#,##0.00;0.00", CultureInfo.InvariantCulture),
                    AccountCountText = group.Count().ToString("N0", CultureInfo.InvariantCulture),
                    IsExpanded = !_groupExpansionState.TryGetValue(group.Key, out var expanded) || expanded
                };

                foreach (var row in group.OrderBy(x => x.Code, StringComparer.OrdinalIgnoreCase))
                    vm.Accounts.Add(MapToRowVm(row));

                return vm;
            })
            .OrderBy(x => x.ParentCode, StringComparer.OrdinalIgnoreCase)
            .ToList();

        foreach (var group in grouped)
            AccountGroups.Add(group);

        ApplySelectionState(AccountGroups, _selectedAccountId);
        OnPropertyChanged(nameof(HasAnyAccounts));
    }

    private void CaptureGroupExpansionState()
    {
        foreach (var group in AccountGroups)
            _groupExpansionState[group.ParentId] = group.IsExpanded;
    }

    private static AccountListRowVm MapToRowVm(AccountWorkspaceRowDto row)
        => new()
        {
            Id = row.Id,
            ParentId = row.ParentId,
            ParentCode = row.ParentCode ?? "—",
            ParentName = row.ParentName ?? "—",
            Code = row.Code,
            ArabicName = row.ArabicName,
            Nature = row.Nature,
            Classification = row.Classification,
            CloseBehavior = row.CloseBehavior,
            Origin = row.Origin,
            IsPosting = row.IsPosting,
            AllowManualPosting = row.AllowManualPosting,
            AllowChildren = row.AllowChildren,
            IsSystem = row.IsSystem,
            IsActive = row.IsActive,
            CurrencyCode = row.CurrencyCode,
            SystemRole = row.SystemRole,
            Balance = row.Balance,
            LastMovementDate = row.LastMovementDate,
            ChildAccountsCount = row.ChildAccountsCount
        };

    private static List<AccountNodeVm> BuildTree(IReadOnlyList<AccountFlatDto> flat)
    {
        var map = flat.ToDictionary(
            x => x.Id,
            x => new AccountNodeVm(
                x.Id,
                x.Code,
                string.IsNullOrWhiteSpace(x.ArabicName) ? "—" : x.ArabicName,
                x.Nature,
                x.Classification,
                x.CloseBehavior,
                x.IsPosting,
                x.AllowManualPosting,
                x.AllowChildren,
                x.IsSystem,
                x.Origin,
                x.IsActive,
                x.CurrencyCode,
                x.SystemRole,
                x.ParentId));

        foreach (var node in map.Values)
        {
            if (node.ParentId is Guid parentId && map.TryGetValue(parentId, out var parent))
                parent.Children.Add(node);
        }

        var roots = map.Values
            .Where(x => x.ParentId is null)
            .OrderBy(x => x.Code, StringComparer.OrdinalIgnoreCase)
            .ToList();

        SortRecursively(roots);
        return roots;

        static void SortRecursively(IList<AccountNodeVm> nodes)
        {
            foreach (var node in nodes)
            {
                if (node.Children.Count == 0)
                    continue;

                var sorted = node.Children
                    .OrderBy(x => x.Code, StringComparer.OrdinalIgnoreCase)
                    .ToList();

                node.Children.Clear();
                foreach (var child in sorted)
                    node.Children.Add(child);

                SortRecursively(sorted);
            }
        }
    }

    private static List<AccountNodeVm> FilterTree(IEnumerable<AccountNodeVm> source, Func<AccountNodeVm, bool> predicate)
    {
        var result = new List<AccountNodeVm>();

        foreach (var node in source)
        {
            var childMatches = FilterTree(node.Children, predicate);
            var selfMatches = predicate(node);

            if (!selfMatches && childMatches.Count == 0)
                continue;

            var copy = new AccountNodeVm(
                node.Id,
                node.Code,
                node.ArabicName,
                node.Nature,
                node.Classification,
                node.CloseBehavior,
                node.IsPosting,
                node.AllowManualPosting,
                node.AllowChildren,
                node.IsSystem,
                node.Origin,
                node.IsActive,
                node.CurrencyCode,
                node.SystemRole,
                node.ParentId);

            IEnumerable<AccountNodeVm> childrenToShow = selfMatches ? node.Children : childMatches;
            foreach (var child in childrenToShow)
                copy.Children.Add(child);

            result.Add(copy);
        }

        return result;
    }

    private void RebindTree(IEnumerable<AccountNodeVm> nodes)
    {
        AccountTree.Clear();
        foreach (var node in nodes)
            AccountTree.Add(node);

        ApplySelectionState(AccountTree, _selectedAccountId);
        OnPropertyChanged(nameof(HasAnyAccounts));
    }

    private void UpdateSummary(IReadOnlyList<AccountWorkspaceRowDto> rows)
    {
        Summary.TotalAccountsText = rows.Count.ToString("N0", CultureInfo.InvariantCulture);
        Summary.TemplateAccountsText = rows.Count(x => x.Origin == AccountOrigin.Template).ToString("N0", CultureInfo.InvariantCulture);
        Summary.PartyAccountsText = rows.Count(x => x.Origin == AccountOrigin.PartyGenerated).ToString("N0", CultureInfo.InvariantCulture);
        Summary.ManualAccountsText = rows.Count(x => x.Origin == AccountOrigin.Manual).ToString("N0", CultureInfo.InvariantCulture);
        Summary.ActiveAccountsText = rows.Count(x => x.IsActive).ToString("N0", CultureInfo.InvariantCulture);
        Summary.AssetsBalanceText = rows.Where(x => x.Classification == AccountClass.Assets).Sum(x => x.Balance).ToString("#,##0.00", CultureInfo.InvariantCulture);
        Summary.LiabilitiesBalanceText = rows.Where(x => x.Classification == AccountClass.Liabilities).Sum(x => Math.Abs(x.Balance)).ToString("#,##0.00", CultureInfo.InvariantCulture);
        Summary.EquityBalanceText = rows.Where(x => x.Classification == AccountClass.Equity).Sum(x => Math.Abs(x.Balance)).ToString("#,##0.00", CultureInfo.InvariantCulture);

        Summary.ClassBreakdown.Clear();
        foreach (var group in rows
                     .GroupBy(x => x.Classification)
                     .OrderBy(x => x.Key))
        {
            Summary.ClassBreakdown.Add(AccountClassBreakdownVm.Create(
                group.Key,
                group.Count(),
                group.Sum(x => x.Balance),
                rows.Count));
        }
    }

    private static void ApplySelectionState(IEnumerable<AccountNodeVm> nodes, Guid? selectedAccountId)
    {
        foreach (var node in nodes)
        {
            node.IsSelected = selectedAccountId.HasValue && node.Id == selectedAccountId.Value;
            ApplySelectionState(node.Children, selectedAccountId);
        }
    }

    private static void ApplySelectionState(IEnumerable<AccountGroupVm> groups, Guid? selectedAccountId)
    {
        foreach (var group in groups)
        {
            foreach (var row in group.Accounts)
                row.IsSelected = selectedAccountId.HasValue && row.Id == selectedAccountId.Value;
        }
    }

    private static AccountDetailPanelVm MapToDetailPanel(AccountDetailDto details)
    {
        var panel = new AccountDetailPanelVm
        {
            Id = details.Id,
            Code = details.Code,
            ArabicName = details.ArabicName,
            EnglishName = string.IsNullOrWhiteSpace(details.EnglishName) ? "—" : details.EnglishName,
            ParentDisplay = string.IsNullOrWhiteSpace(details.ParentCode)
                ? "حساب رئيسي"
                : $"{details.ParentCode} - {details.ParentName}",
            TypeText = details.IsPosting ? "حركي" : "تجميعي",
            NatureText = details.Nature == AccountNature.Debit ? "مدين" : "دائن",
            ClassificationText = details.Classification switch
            {
                AccountClass.Assets => "أصول",
                AccountClass.Liabilities => "خصوم",
                AccountClass.Equity => "حقوق ملكية",
                AccountClass.Revenue => "إيرادات",
                _ => "مصروفات"
            },
            CloseBehaviorText = details.CloseBehavior switch
            {
                AccountCloseBehavior.Permanent => "دائم",
                AccountCloseBehavior.Temporary => "مؤقت",
                _ => "يقفل آخر السنة"
            },
            OriginText = details.Origin switch
            {
                AccountOrigin.Template => "افتراضي",
                AccountOrigin.PartyGenerated => "مولد لطرف",
                _ => "يدوي"
            },
            StatusText = details.IsActive ? "نشط" : "موقوف",
            CurrencyText = string.IsNullOrWhiteSpace(details.CurrencyCode) ? "—" : details.CurrencyCode,
            SystemRoleText = string.IsNullOrWhiteSpace(details.SystemRole) ? "بدون" : details.SystemRole,
            PostingModeText = details.AllowManualPosting ? "يسمح بالترحيل اليدوي" : "لا يقبل الترحيل اليدوي",
            LevelText = $"المستوى {details.Level}",
            NotesText = string.IsNullOrWhiteSpace(details.Notes) ? "لا توجد ملاحظات." : details.Notes,
            UsageHeadline = BuildUsageHeadline(details),
            BalanceText = details.UsageSummary.CurrentBalance.ToString("+#,##0.00;-#,##0.00;0.00", CultureInfo.InvariantCulture),
            MovementLinesText = details.UsageSummary.PostedMovementLinesCount.ToString("N0", CultureInfo.InvariantCulture),
            LinkedPartiesText = details.UsageSummary.LinkedPartiesCount == 0
                ? "لا يوجد"
                : string.Join("، ", details.UsageSummary.LinkedPartyNames),
            DeleteRuleText = details.UsageSummary.CanDelete ? "يمكن حذف الحساب إذا لم تطرأ عليه استخدامات جديدة." : "الحساب محمي من الحذف حاليًا.",
            DeactivateRuleText = details.UsageSummary.CanDeactivate ? "يمكن إيقاف الحساب إذا توقف استخدامه تشغيليًا." : "الحساب محمي من الإيقاف حاليًا.",
            CanDelete = details.UsageSummary.CanDelete,
            CanDeactivate = details.UsageSummary.CanDeactivate,
            IsSystem = details.IsSystem,
            IsActive = details.IsActive
        };

        panel.UsageBadges.Add(panel.ClassificationText);
        panel.UsageBadges.Add(panel.NatureText);
        panel.UsageBadges.Add(panel.OriginText);
        if (details.IsSystem)
            panel.UsageBadges.Add("حساب نظامي");
        if (details.UsageSummary.IsSalesLinked)
            panel.UsageBadges.Add("مرتبط بالمبيعات");
        if (details.UsageSummary.IsInventoryLinked)
            panel.UsageBadges.Add("مرتبط بالمخزون");
        if (details.UsageSummary.IsTaxLinked)
            panel.UsageBadges.Add("مرتبط بالضرائب");
        if (details.UsageSummary.IsYearCloseLinked)
            panel.UsageBadges.Add("مرتبط بإقفال السنة");
        if (details.UsageSummary.LinkedPartiesCount > 0)
            panel.UsageBadges.Add($"أطراف مرتبطة: {details.UsageSummary.LinkedPartiesCount}");

        foreach (var reason in details.UsageSummary.BlockingReasons)
            panel.BlockingReasons.Add(reason);

        foreach (var child in details.Children)
        {
            panel.Children.Add(new AccountDetailChildVm
            {
                Id = child.Id,
                Code = child.Code,
                ArabicName = child.ArabicName,
                TypeText = child.IsPosting ? "حركي" : "تجميعي",
                OriginText = child.Origin switch
                {
                    AccountOrigin.Template => "افتراضي",
                    AccountOrigin.PartyGenerated => "طرف",
                    _ => "يدوي"
                },
                StatusText = child.IsActive ? "نشط" : "موقوف"
            });
        }

        return panel;
    }

    private static string BuildUsageHeadline(AccountDetailDto details)
    {
        var segments = new List<string>();

        if (details.UsageSummary.PostedMovementLinesCount > 0)
            segments.Add($"مرتبط بـ {details.UsageSummary.PostedMovementLinesCount:N0} سطور قيود مرحلة");

        if (details.UsageSummary.LinkedPartiesCount > 0)
            segments.Add($"مربوط بـ {details.UsageSummary.LinkedPartiesCount:N0} أطراف");

        if (details.UsageSummary.IsSalesLinked)
            segments.Add("مستخدم في إعدادات المبيعات");

        if (details.UsageSummary.IsInventoryLinked)
            segments.Add("مستخدم في إعدادات المخزون");

        if (details.UsageSummary.IsTaxLinked)
            segments.Add("مستخدم في الضرائب");

        if (details.UsageSummary.IsYearCloseLinked)
            segments.Add("مستخدم في إقفال السنة");

        if (segments.Count == 0)
            return "لا توجد ارتباطات تشغيلية تمنع إدارة هذا الحساب حالياً.";

        var prefix = details.UsageSummary.BlockingReasons.Count == 0
            ? "هذا الحساب مستخدم حالياً في"
            : "هذا الحساب مستخدم حالياً في";

        var suffix = details.UsageSummary.BlockingReasons.Count == 0
            ? "، لكنه ما يزال قابلاً للإدارة ضمن القيود المحاسبية الحالية."
            : "، لذلك تظهر أدناه القيود التي تمنع بعض الإجراءات عليه.";

        return $"{prefix} {string.Join("، ", segments)}{suffix}";
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}

internal sealed class RelayCommand : ICommand
{
    private readonly Action _execute;
    private readonly Func<bool>? _canExecute;

    public RelayCommand(Action execute, Func<bool>? canExecute = null)
    {
        _execute = execute;
        _canExecute = canExecute;
    }

    public bool CanExecute(object? parameter) => _canExecute?.Invoke() ?? true;

    public void Execute(object? parameter) => _execute();

    public event EventHandler? CanExecuteChanged;
    public void RaiseCanExecuteChanged() => CanExecuteChanged?.Invoke(this, EventArgs.Empty);
}

public sealed class AsyncRelayCommand : ICommand
{
    private readonly Func<Task> _execute;
    private readonly Func<bool>? _canExecute;
    private bool _isExecuting;

    public AsyncRelayCommand(Func<Task> execute, Func<bool>? canExecute = null)
    {
        _execute = execute;
        _canExecute = canExecute;
    }

    public bool CanExecute(object? parameter) => !_isExecuting && (_canExecute?.Invoke() ?? true);

    public async void Execute(object? parameter) => await ExecuteAsync();

    public async Task ExecuteAsync()
    {
        if (!CanExecute(null)) return;

        try
        {
            _isExecuting = true;
            RaiseCanExecuteChanged();
            await _execute();
        }
        finally
        {
            _isExecuting = false;
            RaiseCanExecuteChanged();
        }
    }

    public event EventHandler? CanExecuteChanged;

    public void RaiseCanExecuteChanged() => CanExecuteChanged?.Invoke(this, EventArgs.Empty);
}
