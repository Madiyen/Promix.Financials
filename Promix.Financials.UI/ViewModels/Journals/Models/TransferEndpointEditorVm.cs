using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Microsoft.UI.Xaml;
using Promix.Financials.Domain.Enums;

namespace Promix.Financials.UI.ViewModels.Journals.Models;

public sealed class TransferEndpointEditorVm : INotifyPropertyChanged
{
    private TransferEndpointMode _mode;
    private Guid? _selectedAccountId;
    private Guid? _selectedPartyId;
    private PartyLedgerSide? _selectedPartySide;
    private bool _requiresPartySide;
    private string _resolvedAccountText = "سيظهر الحساب المرتبط بالطرف هنا بعد الاختيار.";

    public TransferEndpointMode Mode
    {
        get => _mode;
        set
        {
            if (_mode == value) return;
            _mode = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(ModeIndex));
            OnPropertyChanged(nameof(IsPartyMode));
            OnPropertyChanged(nameof(IsGeneralAccountMode));
            OnPropertyChanged(nameof(GeneralAccountVisibility));
            OnPropertyChanged(nameof(PartyPickerVisibility));
            OnPropertyChanged(nameof(PartySideVisibility));
            OnPropertyChanged(nameof(ResolvedAccountVisibility));
        }
    }

    public int ModeIndex
    {
        get => Mode == TransferEndpointMode.Party ? 1 : 0;
        set => Mode = value == 1 ? TransferEndpointMode.Party : TransferEndpointMode.GeneralAccount;
    }

    public Guid? SelectedAccountId
    {
        get => _selectedAccountId;
        set
        {
            if (_selectedAccountId == value) return;
            _selectedAccountId = value;
            OnPropertyChanged();
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
        }
    }

    public PartyLedgerSide? SelectedPartySide
    {
        get => _selectedPartySide;
        set
        {
            if (_selectedPartySide == value) return;
            _selectedPartySide = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(PartySideIndex));
        }
    }

    public int PartySideIndex
    {
        get => SelectedPartySide switch
        {
            PartyLedgerSide.Customer => 0,
            PartyLedgerSide.Vendor => 1,
            _ => -1
        };
        set => SelectedPartySide = value switch
        {
            0 => PartyLedgerSide.Customer,
            1 => PartyLedgerSide.Vendor,
            _ => null
        };
    }

    public bool RequiresPartySide
    {
        get => _requiresPartySide;
        set
        {
            if (_requiresPartySide == value) return;
            _requiresPartySide = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(PartySideVisibility));
        }
    }

    public string ResolvedAccountText
    {
        get => _resolvedAccountText;
        set
        {
            if (_resolvedAccountText == value) return;
            _resolvedAccountText = value;
            OnPropertyChanged();
        }
    }

    public bool IsPartyMode => Mode == TransferEndpointMode.Party;
    public bool IsGeneralAccountMode => Mode == TransferEndpointMode.GeneralAccount;
    public Visibility GeneralAccountVisibility => IsGeneralAccountMode ? Visibility.Visible : Visibility.Collapsed;
    public Visibility PartyPickerVisibility => IsPartyMode ? Visibility.Visible : Visibility.Collapsed;
    public Visibility PartySideVisibility => IsPartyMode && RequiresPartySide ? Visibility.Visible : Visibility.Collapsed;
    public Visibility ResolvedAccountVisibility => IsPartyMode ? Visibility.Visible : Visibility.Collapsed;

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}
