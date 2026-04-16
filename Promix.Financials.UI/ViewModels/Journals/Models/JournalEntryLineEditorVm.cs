using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Microsoft.UI.Xaml;

namespace Promix.Financials.UI.ViewModels.Journals.Models;

public sealed class JournalEntryLineEditorVm : INotifyPropertyChanged
{
    private Guid? _selectedAccountId;
    private Guid? _selectedPartyId;
    private string _partyName = string.Empty;
    private string _description = string.Empty;
    private double _debit;
    private double _credit;
    private bool _requiresPartySelection;

    public Guid? SelectedAccountId
    {
        get => _selectedAccountId;
        set
        {
            if (_selectedAccountId == value) return;
            _selectedAccountId = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(IsEmpty));
        }
    }

    public Guid? SelectedPartyId
    {
        get => _selectedPartyId;
        set
        {
            if (_selectedPartyId == value) return;
            _selectedPartyId = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(ShowPartySelection));
            OnPropertyChanged(nameof(PartySelectionVisibility));
            OnPropertyChanged(nameof(IsEmpty));
        }
    }

    public string PartyName
    {
        get => _partyName;
        set
        {
            if (_partyName == value) return;
            _partyName = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(IsEmpty));
        }
    }

    public string Description
    {
        get => _description;
        set
        {
            if (_description == value) return;
            _description = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(IsEmpty));
        }
    }

    public double Debit
    {
        get => _debit;
        set
        {
            var normalized = value < 0 ? 0 : value;
            if (Math.Abs(_debit - normalized) < 0.0001) return;
            _debit = normalized;
            if (_debit > 0 && _credit > 0)
            {
                _credit = 0;
                OnPropertyChanged(nameof(Credit));
            }
            OnPropertyChanged();
            OnPropertyChanged(nameof(IsEmpty));
        }
    }

    public double Credit
    {
        get => _credit;
        set
        {
            var normalized = value < 0 ? 0 : value;
            if (Math.Abs(_credit - normalized) < 0.0001) return;
            _credit = normalized;
            if (_credit > 0 && _debit > 0)
            {
                _debit = 0;
                OnPropertyChanged(nameof(Debit));
            }
            OnPropertyChanged();
            OnPropertyChanged(nameof(IsEmpty));
        }
    }

    public bool RequiresPartySelection
    {
        get => _requiresPartySelection;
        set
        {
            if (_requiresPartySelection == value) return;
            _requiresPartySelection = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(ShowPartySelection));
            OnPropertyChanged(nameof(PartySelectionVisibility));
        }
    }

    public bool ShowPartySelection => RequiresPartySelection || SelectedPartyId is Guid;

    public Visibility PartySelectionVisibility => ShowPartySelection ? Visibility.Visible : Visibility.Collapsed;

    public bool IsEmpty =>
        SelectedAccountId is null &&
        SelectedPartyId is null &&
        string.IsNullOrWhiteSpace(PartyName) &&
        string.IsNullOrWhiteSpace(Description) &&
        Debit <= 0 &&
        Credit <= 0;

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
