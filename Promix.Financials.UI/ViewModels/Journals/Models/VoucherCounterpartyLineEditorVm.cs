using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Microsoft.UI.Xaml;

namespace Promix.Financials.UI.ViewModels.Journals.Models;

public sealed class VoucherCounterpartyLineEditorVm : INotifyPropertyChanged
{
    private VoucherCounterpartyEntryKind _entryKind;
    private Guid? _selectedAccountId;
    private Guid? _selectedPartyId;
    private string _partyName = string.Empty;
    private string _description = string.Empty;
    private double _amount;
    private string _openItemsSummaryText = string.Empty;
    private string _automaticPartyAccountText = "سيظهر الحساب المرتبط بالطرف هنا بعد الاختيار.";

    public VoucherCounterpartyLineEditorVm()
    {
        _entryKind = VoucherCounterpartyEntryKind.GeneralAccount;
        OpenItems = new ObservableCollection<PartyOpenItemPreviewVm>();
        OpenItems.CollectionChanged += OpenItems_CollectionChanged;
    }

    public VoucherCounterpartyEntryKind EntryKind
    {
        get => _entryKind;
        set
        {
            if (_entryKind == value) return;
            _entryKind = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(EntryKindIndex));
            OnPropertyChanged(nameof(IsPartyEntry));
            OnPropertyChanged(nameof(IsGeneralAccountEntry));
            OnPropertyChanged(nameof(AccountLookupVisibility));
            OnPropertyChanged(nameof(PartyPickerVisibility));
            OnPropertyChanged(nameof(AutomaticPartyAccountVisibility));
            OnPropertyChanged(nameof(IsEmpty));
        }
    }

    public int EntryKindIndex
    {
        get => EntryKind == VoucherCounterpartyEntryKind.GeneralAccount ? 0 : 1;
        set => EntryKind = value == 1 ? VoucherCounterpartyEntryKind.Party : VoucherCounterpartyEntryKind.GeneralAccount;
    }

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
            OnPropertyChanged(nameof(IsPartySelected));
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

    public double Amount
    {
        get => _amount;
        set
        {
            var normalized = value < 0 ? 0 : value;
            if (Math.Abs(_amount - normalized) < 0.0001) return;
            _amount = normalized;
            OnPropertyChanged();
            OnPropertyChanged(nameof(IsEmpty));
        }
    }

    public ObservableCollection<PartyOpenItemPreviewVm> OpenItems { get; }

    public string OpenItemsSummaryText
    {
        get => _openItemsSummaryText;
        set
        {
            if (_openItemsSummaryText == value) return;
            _openItemsSummaryText = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(OpenItemsVisibility));
        }
    }

    public string AutomaticPartyAccountText
    {
        get => _automaticPartyAccountText;
        set
        {
            if (_automaticPartyAccountText == value) return;
            _automaticPartyAccountText = value;
            OnPropertyChanged();
        }
    }

    public Visibility OpenItemsVisibility => OpenItems.Count > 0 || !string.IsNullOrWhiteSpace(OpenItemsSummaryText)
        ? Visibility.Visible
        : Visibility.Collapsed;

    public bool IsPartySelected => SelectedPartyId is Guid;
    public bool IsPartyEntry => EntryKind == VoucherCounterpartyEntryKind.Party;
    public bool IsGeneralAccountEntry => EntryKind == VoucherCounterpartyEntryKind.GeneralAccount;
    public Visibility AccountLookupVisibility => IsGeneralAccountEntry ? Visibility.Visible : Visibility.Collapsed;
    public Visibility PartyPickerVisibility => IsPartyEntry ? Visibility.Visible : Visibility.Collapsed;
    public Visibility AutomaticPartyAccountVisibility => IsPartyEntry ? Visibility.Visible : Visibility.Collapsed;

    public bool IsEmpty =>
        SelectedAccountId is null &&
        SelectedPartyId is null &&
        string.IsNullOrWhiteSpace(PartyName) &&
        string.IsNullOrWhiteSpace(Description) &&
        Amount <= 0;

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OpenItems_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        => OnPropertyChanged(nameof(OpenItemsVisibility));

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}
