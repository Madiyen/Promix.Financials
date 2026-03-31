using Microsoft.UI.Xaml;
using Promix.Financials.Application.Features.FinancialYears.Commands;
using Promix.Financials.Application.Features.FinancialYears.Queries;
using Promix.Financials.Application.Features.FinancialYears.Services;
using Promix.Financials.UI.ViewModels.Ledger.Models;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Linq;

namespace Promix.Financials.UI.ViewModels.Ledger;

public sealed class FinancialYearsViewModel : INotifyPropertyChanged
{
    private readonly IFinancialYearQuery _query;
    private readonly CreateFinancialYearService _createService;
    private readonly EditFinancialYearService _editService;
    private readonly ActivateFinancialYearService _activateService;
    private Guid _companyId;
    private FinancialYearRowVm? _selectedYear;
    private bool _isBusy;
    private string? _errorMessage;
    private string? _successMessage;

    public FinancialYearsViewModel(
        IFinancialYearQuery query,
        CreateFinancialYearService createService,
        EditFinancialYearService editService,
        ActivateFinancialYearService activateService)
    {
        _query = query;
        _createService = createService;
        _editService = editService;
        _activateService = activateService;
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public ObservableCollection<FinancialYearRowVm> Years { get; } = new();

    public FinancialYearRowVm? SelectedYear
    {
        get => _selectedYear;
        set
        {
            if (ReferenceEquals(_selectedYear, value))
                return;

            _selectedYear = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(CanEditSelected));
            OnPropertyChanged(nameof(CanActivateSelected));
            OnPropertyChanged(nameof(SelectedTitleText));
            OnPropertyChanged(nameof(SelectedRangeText));
            OnPropertyChanged(nameof(SelectedStatusText));
        }
    }

    public bool IsBusy
    {
        get => _isBusy;
        private set
        {
            if (_isBusy == value)
                return;

            _isBusy = value;
            OnPropertyChanged();
        }
    }

    public string? ErrorMessage
    {
        get => _errorMessage;
        private set
        {
            if (_errorMessage == value)
                return;

            _errorMessage = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(HasError));
            OnPropertyChanged(nameof(ErrorVisibility));
        }
    }

    public string? SuccessMessage
    {
        get => _successMessage;
        private set
        {
            if (_successMessage == value)
                return;

            _successMessage = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(HasSuccess));
            OnPropertyChanged(nameof(SuccessVisibility));
        }
    }

    public bool HasError => !string.IsNullOrWhiteSpace(ErrorMessage);
    public bool HasSuccess => !string.IsNullOrWhiteSpace(SuccessMessage);
    public Visibility ErrorVisibility => HasError ? Visibility.Visible : Visibility.Collapsed;
    public Visibility SuccessVisibility => HasSuccess ? Visibility.Visible : Visibility.Collapsed;
    public bool CanEditSelected => SelectedYear is not null && !IsBusy;
    public bool CanActivateSelected => SelectedYear is { IsActive: false } && !IsBusy;
    public string SummaryText => Years.Count == 0 ? "لا توجد سنوات مالية معرّفة بعد." : $"{Years.Count} سنة مالية";
    public string SelectedTitleText => SelectedYear?.DisplayName ?? "اختر سنة مالية";
    public string SelectedRangeText => SelectedYear?.RangeText ?? "النطاق سيظهر هنا بعد اختيار سنة مالية.";
    public string SelectedStatusText => SelectedYear?.StatusText ?? "لم يتم اختيار سنة مالية بعد.";

    public async Task InitializeAsync(Guid companyId)
    {
        _companyId = companyId;
        await RefreshAsync();
    }

    public async Task RefreshAsync()
    {
        if (_companyId == Guid.Empty)
            return;

        IsBusy = true;
        ErrorMessage = null;
        SuccessMessage = null;

        try
        {
            var years = await _query.GetFinancialYearsAsync(_companyId);
            var selectedYearId = SelectedYear?.Id;

            Years.Clear();
            foreach (var year in years)
            {
                Years.Add(new FinancialYearRowVm(
                    year.Id,
                    year.Code,
                    year.Name,
                    year.StartDate,
                    year.EndDate,
                    year.IsActive));
            }

            SelectedYear = selectedYearId is Guid id
                ? Years.FirstOrDefault(x => x.Id == id)
                : Years.FirstOrDefault(x => x.IsActive) ?? Years.FirstOrDefault();

            OnPropertyChanged(nameof(SummaryText));
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }
        finally
        {
            IsBusy = false;
        }
    }

    public async Task CreateAsync(CreateFinancialYearCommand command)
    {
        await ExecuteAsync(async () =>
        {
            await _createService.CreateAsync(command);
            SuccessMessage = "تم إنشاء السنة المالية بنجاح.";
            await RefreshAsync();
        });
    }

    public async Task EditAsync(EditFinancialYearCommand command)
    {
        await ExecuteAsync(async () =>
        {
            await _editService.EditAsync(command);
            SuccessMessage = "تم تحديث السنة المالية بنجاح.";
            await RefreshAsync();
            SelectedYear = Years.FirstOrDefault(x => x.Id == command.FinancialYearId);
        });
    }

    public async Task ActivateSelectedAsync()
    {
        if (SelectedYear is null)
            return;

        await ExecuteAsync(async () =>
        {
            var selectedYearId = SelectedYear!.Id;
            await _activateService.ActivateAsync(new ActivateFinancialYearCommand(_companyId, selectedYearId));
            SuccessMessage = "تم تفعيل السنة المالية المختارة.";
            await RefreshAsync();
            SelectedYear = Years.FirstOrDefault(x => x.Id == selectedYearId) ?? Years.FirstOrDefault(x => x.IsActive);
        });
    }

    private async Task ExecuteAsync(Func<Task> action)
    {
        IsBusy = true;
        ErrorMessage = null;
        SuccessMessage = null;

        try
        {
            await action();
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }
        finally
        {
            IsBusy = false;
        }
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}
