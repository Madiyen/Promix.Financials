using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Promix.Financials.UI.ViewModels.Accounts.Models;

public sealed class AccountsSummaryVm : INotifyPropertyChanged
{
    private string _totalAccountsText = "0";
    private string _templateAccountsText = "0";
    private string _partyAccountsText = "0";
    private string _manualAccountsText = "0";
    private string _activeAccountsText = "0";
    private string _assetsBalanceText = "0.00";
    private string _liabilitiesBalanceText = "0.00";
    private string _equityBalanceText = "0.00";

    public string TotalAccountsText { get => _totalAccountsText; set => SetField(ref _totalAccountsText, value); }
    public string TemplateAccountsText { get => _templateAccountsText; set => SetField(ref _templateAccountsText, value); }
    public string PartyAccountsText { get => _partyAccountsText; set => SetField(ref _partyAccountsText, value); }
    public string ManualAccountsText { get => _manualAccountsText; set => SetField(ref _manualAccountsText, value); }
    public string ActiveAccountsText { get => _activeAccountsText; set => SetField(ref _activeAccountsText, value); }
    public string AssetsBalanceText { get => _assetsBalanceText; set => SetField(ref _assetsBalanceText, value); }
    public string LiabilitiesBalanceText { get => _liabilitiesBalanceText; set => SetField(ref _liabilitiesBalanceText, value); }
    public string EquityBalanceText { get => _equityBalanceText; set => SetField(ref _equityBalanceText, value); }

    public ObservableCollection<AccountClassBreakdownVm> ClassBreakdown { get; } = [];

    public event PropertyChangedEventHandler? PropertyChanged;

    private void SetField(ref string field, string value, [CallerMemberName] string? propertyName = null)
    {
        if (field == value) return;
        field = value;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
