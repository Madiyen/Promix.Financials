using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Promix.Financials.Application.Features.Parties.Commands;
using Promix.Financials.Application.Features.Parties.Queries;
using Promix.Financials.Application.Features.Parties.Services;
using Promix.Financials.Domain.Enums;
using Promix.Financials.UI.ViewModels.Parties.Models;

namespace Promix.Financials.UI.ViewModels.Parties;

public sealed class PartiesPageViewModel : INotifyPropertyChanged
{
    private readonly IPartyQuery _query;
    private readonly CreatePartyService _createPartyService;
    private readonly EditPartyService _editPartyService;
    private readonly ActivatePartyService _activatePartyService;
    private readonly DeactivatePartyService _deactivatePartyService;
    private Guid _companyId;
    private IReadOnlyList<PartyListItemDto> _allParties = Array.Empty<PartyListItemDto>();
    private PartyRowVm? _selectedParty;
    private string _searchText = string.Empty;
    private PartyFilterMode _selectedTypeFilter;
    private PartyStatusFilterMode _selectedStatusFilter = PartyStatusFilterMode.ActiveOnly;
    private DateTimeOffset _fromDate = new(DateTime.Now.Year, 1, 1, 0, 0, 0, TimeSpan.Zero);
    private DateTimeOffset _toDate = DateTimeOffset.Now.Date;
    private string? _errorMessage;
    private string? _successMessage;
    private bool _isInitialized;

    public PartiesPageViewModel(
        IPartyQuery query,
        CreatePartyService createPartyService,
        EditPartyService editPartyService,
        ActivatePartyService activatePartyService,
        DeactivatePartyService deactivatePartyService)
    {
        _query = query;
        _createPartyService = createPartyService;
        _editPartyService = editPartyService;
        _activatePartyService = activatePartyService;
        _deactivatePartyService = deactivatePartyService;
    }

    public ObservableCollection<PartyRowVm> Parties { get; } = [];
    public ObservableCollection<PartyStatementMovementVm> Movements { get; } = [];
    public ObservableCollection<PartyOpenItemRowVm> OpenItems { get; } = [];
    public ObservableCollection<PartySettlementRowVm> Settlements { get; } = [];
    public ObservableCollection<PartyAgingBucketVm> AgingBuckets { get; } = [];

    public PartyRowVm? SelectedParty
    {
        get => _selectedParty;
        private set
        {
            if (_selectedParty == value) return;
            _selectedParty = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(HasSelectedParty));
            OnPropertyChanged(nameof(SelectedPartyTitle));
            OnPropertyChanged(nameof(SelectedPartySubtitle));
            OnPropertyChanged(nameof(CanEditSelectedParty));
            OnPropertyChanged(nameof(CanDeactivateSelectedParty));
            OnPropertyChanged(nameof(CanActivateSelectedParty));
        }
    }

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

    public PartyFilterMode SelectedTypeFilter
    {
        get => _selectedTypeFilter;
        set
        {
            if (_selectedTypeFilter == value) return;
            _selectedTypeFilter = value;
            OnPropertyChanged();
            ApplyFilters();
        }
    }

    public PartyStatusFilterMode SelectedStatusFilter
    {
        get => _selectedStatusFilter;
        set
        {
            if (_selectedStatusFilter == value) return;
            _selectedStatusFilter = value;
            OnPropertyChanged();
            ApplyFilters();
        }
    }

    public DateTimeOffset FromDate
    {
        get => _fromDate;
        set
        {
            if (_fromDate == value) return;
            _fromDate = value;
            OnPropertyChanged();
        }
    }

    public DateTimeOffset ToDate
    {
        get => _toDate;
        set
        {
            if (_toDate == value) return;
            _toDate = value;
            OnPropertyChanged();
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
            OnPropertyChanged(nameof(HasError));
        }
    }

    public string? SuccessMessage
    {
        get => _successMessage;
        private set
        {
            if (_successMessage == value) return;
            _successMessage = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(HasSuccess));
        }
    }

    public bool HasError => !string.IsNullOrWhiteSpace(ErrorMessage);
    public bool HasSuccess => !string.IsNullOrWhiteSpace(SuccessMessage);
    public bool HasSelectedParty => SelectedParty is not null;
    public bool CanEditSelectedParty => SelectedParty is not null;
    public bool CanDeactivateSelectedParty => SelectedParty is { IsActive: true };
    public bool CanActivateSelectedParty => SelectedParty is { IsActive: false };
    public string SelectedPartyTitle => SelectedParty is null ? "اختر طرفاً" : $"{SelectedParty.NameAr} · {SelectedParty.Code}";
    public string SelectedPartySubtitle => SelectedParty is null
        ? "اختر عميلًا أو موردًا من القائمة لعرض كشفه وبنوده المفتوحة وتسوياته."
        : $"{SelectedParty.TypeText} • {SelectedParty.StatusText}";
    public string TotalPartiesText => _allParties.Count.ToString("N0");
    public string ActivePartiesText => _allParties.Count(x => x.IsActive).ToString("N0");
    public string ReceivableOpenText => _allParties.Sum(x => x.ReceivableOpenBalance).ToString("N2");
    public string PayableOpenText => _allParties.Sum(x => x.PayableOpenBalance).ToString("N2");
    public string OpeningBalanceText { get; private set; } = "0.00";
    public string ClosingBalanceText { get; private set; } = "0.00";

    public async Task InitializeAsync(Guid companyId)
    {
        if (_isInitialized && _companyId == companyId)
            return;

        _companyId = companyId;
        _isInitialized = true;
        await RefreshAsync();
    }

    public async Task RefreshAsync()
    {
        try
        {
            ErrorMessage = null;
            SuccessMessage = null;
            _allParties = await _query.GetPartiesAsync(_companyId);
            ApplyFilters();

            if (SelectedParty is null && Parties.Count > 0)
                await SelectPartyAsync(Parties[0]);
            else if (SelectedParty is not null)
                await SelectPartyAsync(SelectedParty);

            NotifySummaryChanged();
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }
    }

    public async Task SelectPartyAsync(PartyRowVm? party)
    {
        SelectedParty = party;
        Movements.Clear();
        OpenItems.Clear();
        Settlements.Clear();
        AgingBuckets.Clear();
        OpeningBalanceText = "0.00";
        ClosingBalanceText = "0.00";
        OnPropertyChanged(nameof(OpeningBalanceText));
        OnPropertyChanged(nameof(ClosingBalanceText));

        if (party is null)
            return;

        var statement = await _query.GetStatementAsync(
            _companyId,
            party.Id,
            DateOnly.FromDateTime(FromDate.Date),
            DateOnly.FromDateTime(ToDate.Date));

        if (statement is null)
            return;

        OpeningBalanceText = statement.OpeningBalance.ToString("N2");
        ClosingBalanceText = statement.ClosingBalance.ToString("N2");
        OnPropertyChanged(nameof(OpeningBalanceText));
        OnPropertyChanged(nameof(ClosingBalanceText));

        foreach (var movement in statement.Movements)
        {
            Movements.Add(new PartyStatementMovementVm(
                movement.EntryNumber,
                movement.EntryDate,
                movement.AccountNameAr,
                movement.Debit,
                movement.Credit,
                movement.RunningBalance,
                movement.Description));
        }

        foreach (var openItem in statement.OpenItems)
        {
            OpenItems.Add(new PartyOpenItemRowVm(
                openItem.EntryNumber,
                openItem.EntryDate,
                openItem.AccountNameAr,
                openItem.OpenAmount,
                openItem.AgeDays,
                openItem.Debit > 0 ? "مدين" : "دائن",
                openItem.Description));
        }

        foreach (var settlement in statement.Settlements)
        {
            Settlements.Add(new PartySettlementRowVm(
                settlement.SettledOn,
                settlement.DebitEntryNumber,
                settlement.CreditEntryNumber,
                settlement.Amount));
        }

        foreach (var bucket in statement.AgingBuckets)
        {
            AgingBuckets.Add(new PartyAgingBucketVm(
                bucket.Label,
                bucket.ReceivableAmount,
                bucket.PayableAmount));
        }
    }

    public async Task CreateAsync(CreatePartyCommand command)
    {
        await _createPartyService.CreateAsync(command);
        SuccessMessage = "تمت إضافة الطرف بنجاح.";
        await RefreshAsync();
    }

    public async Task EditAsync(EditPartyCommand command)
    {
        await _editPartyService.EditAsync(command);
        SuccessMessage = "تم تحديث بيانات الطرف.";
        await RefreshAsync();
        await SelectPartyByIdAsync(command.PartyId);
    }

    public async Task DeactivateSelectedAsync()
    {
        if (SelectedParty is null)
            return;

        await _deactivatePartyService.DeactivateAsync(new DeactivatePartyCommand(_companyId, SelectedParty.Id));
        SuccessMessage = "تم إيقاف التعامل مع الطرف المحدد.";
        await RefreshAsync();
    }

    public async Task ActivateSelectedAsync()
    {
        if (SelectedParty is null)
            return;

        await _activatePartyService.ActivateAsync(new ActivatePartyCommand(_companyId, SelectedParty.Id));
        SuccessMessage = "تمت إعادة تنشيط الطرف المحدد.";
        await RefreshAsync();
    }

    public async Task ReloadSelectedStatementAsync()
    {
        if (SelectedParty is not null)
            await SelectPartyByIdAsync(SelectedParty.Id);
    }

    private async Task SelectPartyByIdAsync(Guid partyId)
    {
        var party = Parties.FirstOrDefault(x => x.Id == partyId);
        if (party is not null)
            await SelectPartyAsync(party);
    }

    private void ApplyFilters()
    {
        var filtered = _allParties
            .Where(MatchesTypeFilter)
            .Where(MatchesStatusFilter)
            .Where(MatchesSearch)
            .Select(x => new PartyRowVm(
                x.Id,
                x.Code,
                x.NameAr,
                x.NameEn,
                x.TypeFlags,
                x.Phone,
                x.Mobile,
                x.Email,
                x.TaxNo,
                x.Address,
                x.Notes,
                x.IsActive,
                x.LedgerMode,
                x.ReceivableAccountId,
                x.PayableAccountId,
                x.ReceivableOpenBalance,
                x.PayableOpenBalance))
            .ToList();

        var selectedId = SelectedParty?.Id;
        Parties.Clear();
        foreach (var item in filtered)
            Parties.Add(item);

        if (selectedId.HasValue)
            SelectedParty = Parties.FirstOrDefault(x => x.Id == selectedId.Value);
        else if (Parties.Count == 0)
            SelectedParty = null;
    }

    private bool MatchesTypeFilter(PartyListItemDto item)
        => SelectedTypeFilter switch
        {
            PartyFilterMode.Customers => item.TypeFlags == PartyTypeFlags.Customer,
            PartyFilterMode.Vendors => item.TypeFlags == PartyTypeFlags.Vendor,
            PartyFilterMode.Both => item.TypeFlags == PartyTypeFlags.Both,
            _ => true
        };

    private bool MatchesStatusFilter(PartyListItemDto item)
        => SelectedStatusFilter switch
        {
            PartyStatusFilterMode.ActiveOnly => item.IsActive,
            PartyStatusFilterMode.InactiveOnly => !item.IsActive,
            _ => true
        };

    private bool MatchesSearch(PartyListItemDto item)
    {
        if (string.IsNullOrWhiteSpace(SearchText))
            return true;

        var search = SearchText.Trim();
        return (item.Code?.Contains(search, StringComparison.CurrentCultureIgnoreCase) ?? false)
            || (item.NameAr?.Contains(search, StringComparison.CurrentCultureIgnoreCase) ?? false)
            || (item.NameEn?.Contains(search, StringComparison.CurrentCultureIgnoreCase) ?? false)
            || (item.Phone?.Contains(search, StringComparison.CurrentCultureIgnoreCase) ?? false)
            || (item.Mobile?.Contains(search, StringComparison.CurrentCultureIgnoreCase) ?? false)
            || (item.TaxNo?.Contains(search, StringComparison.CurrentCultureIgnoreCase) ?? false);
    }

    private void NotifySummaryChanged()
    {
        OnPropertyChanged(nameof(TotalPartiesText));
        OnPropertyChanged(nameof(ActivePartiesText));
        OnPropertyChanged(nameof(ReceivableOpenText));
        OnPropertyChanged(nameof(PayableOpenText));
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}

public enum PartyFilterMode
{
    All,
    Customers,
    Vendors,
    Both
}

public enum PartyStatusFilterMode
{
    All,
    ActiveOnly,
    InactiveOnly
}
