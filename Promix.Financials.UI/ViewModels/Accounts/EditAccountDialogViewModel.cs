using Promix.Financials.Application.Abstractions;
using Promix.Financials.Application.Features.Accounts.Commands;
using Promix.Financials.Application.Features.Accounts.Queries;
using Promix.Financials.Domain.Enums;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace Promix.Financials.UI.ViewModels.Accounts;

public sealed class EditAccountDialogViewModel : INotifyPropertyChanged
{
    private readonly IAccountRepository _repo;
    private readonly IChartOfAccountsQuery _query;

    public EditAccountDialogViewModel(IAccountRepository repo, IChartOfAccountsQuery query)
    {
        _repo = repo;
        _query = query;
    }

    public Guid AccountId { get; private set; }
    public Guid CompanyId { get; private set; }
    public string Code { get; private set; } = "";
    public string TypeText { get; private set; } = "";
    public string CurrencyDisplay { get; private set; } = "";
    public string ClassificationText { get; private set; } = "—";
    public string NatureText { get; private set; } = "—";
    public string CloseBehaviorText { get; private set; } = "—";
    public string ParentDisplay { get; private set; } = "حساب رئيسي";
    public string OriginText { get; private set; } = "—";
    public string SystemRoleText { get; private set; } = "بدون";
    public string LevelText { get; private set; } = "المستوى 1";
    public string BalanceText { get; private set; } = "0.00";
    public string UsageHeadline { get; private set; } = "لا توجد ارتباطات تشغيلية حالياً.";
    public bool IsSystemAccount { get; private set; }

    public ObservableCollection<string> UsageBadges { get; } = [];
    public ObservableCollection<string> BlockingReasons { get; } = [];

    private string _arabicName = "";
    public string ArabicName
    {
        get => _arabicName;
        set
        {
            if (_arabicName == value) return;
            _arabicName = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(CanSave));
            OnPropertyChanged(nameof(ValidationError));
        }
    }

    private string _englishName = "";
    public string EnglishName
    {
        get => _englishName;
        set { if (_englishName == value) return; _englishName = value; OnPropertyChanged(); }
    }

    private bool _isActive = true;
    public bool IsActive
    {
        get => _isActive;
        set { if (_isActive == value) return; _isActive = value; OnPropertyChanged(); }
    }

    private string _notes = "";
    public string Notes
    {
        get => _notes;
        set { if (_notes == value) return; _notes = value; OnPropertyChanged(); }
    }

    public bool CanSave => !string.IsNullOrWhiteSpace(ArabicName);
    public string? ValidationError => CanSave ? null : "الاسم العربي مطلوب";

    public event PropertyChangedEventHandler? PropertyChanged;

    public async Task InitializeAsync(Guid accountId, Guid companyId)
    {
        AccountId = accountId;
        CompanyId = companyId;

        var account = await _repo.GetByIdAsync(accountId, companyId);
        var details = await _query.GetAccountDetailsAsync(companyId, accountId);
        if (account is null || details is null)
            return;

        Code = account.Code;
        TypeText = account.IsPosting ? "حركي" : "تجميعي";
        CurrencyDisplay = account.CurrencyCode ?? "—";
        IsSystemAccount = account.SystemRole is not null || account.Origin == AccountOrigin.Template;

        ArabicName = account.NameAr;
        EnglishName = account.NameEn ?? "";
        IsActive = account.IsActive;
        Notes = account.Notes ?? "";

        ClassificationText = details.Classification switch
        {
            AccountClass.Assets => "أصول",
            AccountClass.Liabilities => "خصوم",
            AccountClass.Equity => "حقوق ملكية",
            AccountClass.Revenue => "إيرادات",
            _ => "مصروفات"
        };
        NatureText = details.Nature == AccountNature.Debit ? "مدين" : "دائن";
        CloseBehaviorText = details.CloseBehavior switch
        {
            AccountCloseBehavior.Permanent => "دائم",
            AccountCloseBehavior.Temporary => "مؤقت",
            _ => "يقفل آخر السنة"
        };
        ParentDisplay = string.IsNullOrWhiteSpace(details.ParentCode)
            ? "حساب رئيسي"
            : $"{details.ParentCode} - {details.ParentName}";
        OriginText = details.Origin switch
        {
            AccountOrigin.Template => "افتراضي",
            AccountOrigin.PartyGenerated => "مولد لطرف",
            _ => "يدوي"
        };
        SystemRoleText = string.IsNullOrWhiteSpace(details.SystemRole) ? "بدون" : details.SystemRole;
        LevelText = $"المستوى {details.Level}";
        BalanceText = details.UsageSummary.CurrentBalance.ToString("+#,##0.00;-#,##0.00;0.00", CultureInfo.InvariantCulture);
        UsageHeadline = details.UsageSummary.BlockingReasons.Count == 0
            ? "الحساب في وضع صحي من ناحية الارتباطات الحالية."
            : "هذا الحساب يحمل ارتباطات محاسبية أو تشغيلية يجب الانتباه لها.";

        UsageBadges.Clear();
        UsageBadges.Add(ClassificationText);
        UsageBadges.Add(NatureText);
        UsageBadges.Add(OriginText);
        if (details.UsageSummary.IsSalesLinked)
            UsageBadges.Add("مرتبط بالمبيعات");
        if (details.UsageSummary.IsInventoryLinked)
            UsageBadges.Add("مرتبط بالمخزون");
        if (details.UsageSummary.IsTaxLinked)
            UsageBadges.Add("مرتبط بالضرائب");
        if (details.UsageSummary.IsYearCloseLinked)
            UsageBadges.Add("مرتبط بإقفال السنة");

        BlockingReasons.Clear();
        foreach (var reason in details.UsageSummary.BlockingReasons)
            BlockingReasons.Add(reason);

        RaiseReadOnlyPropertiesChanged();
    }

    public EditAccountCommand BuildCommand() => new(
        AccountId: AccountId,
        CompanyId: CompanyId,
        ArabicName: ArabicName.Trim(),
        EnglishName: string.IsNullOrWhiteSpace(EnglishName) ? null : EnglishName.Trim(),
        IsActive: IsActive,
        Notes: string.IsNullOrWhiteSpace(Notes) ? null : Notes.Trim()
    );

    private void RaiseReadOnlyPropertiesChanged()
    {
        OnPropertyChanged(nameof(Code));
        OnPropertyChanged(nameof(TypeText));
        OnPropertyChanged(nameof(CurrencyDisplay));
        OnPropertyChanged(nameof(ClassificationText));
        OnPropertyChanged(nameof(NatureText));
        OnPropertyChanged(nameof(CloseBehaviorText));
        OnPropertyChanged(nameof(ParentDisplay));
        OnPropertyChanged(nameof(OriginText));
        OnPropertyChanged(nameof(SystemRoleText));
        OnPropertyChanged(nameof(LevelText));
        OnPropertyChanged(nameof(BalanceText));
        OnPropertyChanged(nameof(UsageHeadline));
        OnPropertyChanged(nameof(IsSystemAccount));
    }

    private void OnPropertyChanged([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
