using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Promix.Financials.UI.ViewModels.Accounts.Models;

public sealed class AccountGroupVm : INotifyPropertyChanged
{
    private bool _isExpanded = true;

    public Guid ParentId { get; init; }
    public string ParentCode { get; init; } = "—";
    public string ParentName { get; init; } = "—";
    public string HeaderBalanceText { get; init; } = "0.00";
    public string AccountCountText { get; init; } = "0";
    public ObservableCollection<AccountListRowVm> Accounts { get; } = [];

    public string HeaderTitle => $"{ParentCode} - {ParentName}";

    public bool IsExpanded
    {
        get => _isExpanded;
        set
        {
            if (_isExpanded == value) return;
            _isExpanded = value;
            OnPropertyChanged();
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}
