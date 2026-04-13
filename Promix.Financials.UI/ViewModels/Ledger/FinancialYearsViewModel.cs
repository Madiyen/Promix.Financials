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
    private readonly IFinancialPeriodQuery _periodQuery;
    private readonly CreateFinancialYearService _createService;
    private readonly EditFinancialYearService _editService;
    private readonly ActivateFinancialYearService _activateService;
    private readonly SetFinancialPeriodStatusService _setFinancialPeriodStatusService;
    private Guid _companyId;
    private FinancialYearRowVm? _selectedYear;
    private FinancialPeriodRowVm? _selectedPeriod;
    private bool _isBusy;
    private string? _errorMessage;
    private string? _successMessage;

    public FinancialYearsViewModel(
        IFinancialYearQuery query,
        IFinancialPeriodQuery periodQuery,
        CreateFinancialYearService createService,
        EditFinancialYearService editService,
        ActivateFinancialYearService activateService,
        SetFinancialPeriodStatusService setFinancialPeriodStatusService)
    {
        _query = query;
        _periodQuery = periodQuery;
        _createService = createService;
        _editService = editService;
        _activateService = activateService;
        _setFinancialPeriodStatusService = setFinancialPeriodStatusService;
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public ObservableCollection<FinancialYearRowVm> Years { get; } = new();
    public ObservableCollection<FinancialPeriodRowVm> Periods { get; } = new();

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
            OnPropertyChanged(nameof(CanToggleSelectedPeriod));
            OnPropertyChanged(nameof(PeriodsSummaryText));
        }
    }

    public FinancialPeriodRowVm? SelectedPeriod
    {
        get => _selectedPeriod;
        set
        {
            if (ReferenceEquals(_selectedPeriod, value))
                return;

            _selectedPeriod = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(CanToggleSelectedPeriod));
            OnPropertyChanged(nameof(SelectedPeriodTitleText));
            OnPropertyChanged(nameof(SelectedPeriodRangeText));
            OnPropertyChanged(nameof(SelectedPeriodStatusText));
            OnPropertyChanged(nameof(SelectedPeriodActionText));
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
    public bool CanToggleSelectedPeriod => SelectedYear is not null && SelectedPeriod is not null && !IsBusy;
    public string SummaryText => Years.Count == 0 ? "لا توجد سنوات مالية معرّفة بعد." : $"{Years.Count} سنة مالية";
    public string PeriodsSummaryText => SelectedYear is null
        ? "اختر سنة مالية لعرض فتراتها."
        : Periods.Count == 0
            ? "لا توجد فترات مالية لهذه السنة."
            : $"{Periods.Count} فترة مالية";
    public string SelectedTitleText => SelectedYear?.DisplayName ?? "اختر سنة مالية";
    public string SelectedRangeText => SelectedYear?.RangeText ?? "النطاق سيظهر هنا بعد اختيار سنة مالية.";
    public string SelectedStatusText => SelectedYear?.StatusText ?? "لم يتم اختيار سنة مالية بعد.";
    public string SelectedPeriodTitleText => SelectedPeriod?.DisplayName ?? "اختر فترة مالية";
    public string SelectedPeriodRangeText => SelectedPeriod?.RangeText ?? "سيظهر نطاق الفترة المالية هنا.";
    public string SelectedPeriodStatusText => SelectedPeriod is null
        ? "لم يتم اختيار فترة مالية بعد."
        : $"{SelectedPeriod.StatusText} · {SelectedPeriod.EntryCountText}";
    public string SelectedPeriodActionText => SelectedPeriod?.IsClosed == true ? "إعادة فتح الفترة" : "إقفال الفترة";

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
            await LoadPeriodsAsync();
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

    public async Task SelectYearAsync(FinancialYearRowVm? year)
    {
        SelectedYear = year;
        await LoadPeriodsAsync();
    }

    public void SelectPeriod(FinancialPeriodRowVm? period)
        => SelectedPeriod = period;

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

    public async Task ToggleSelectedPeriodStatusAsync()
    {
        if (SelectedYear is null || SelectedPeriod is null)
            return;

        await ExecuteAsync(async () =>
        {
            var closing = !SelectedPeriod.IsClosed;
            var nextStatus = closing ? Domain.Enums.FinancialPeriodStatus.Closed : Domain.Enums.FinancialPeriodStatus.Open;
            await _setFinancialPeriodStatusService.SetStatusAsync(_companyId, SelectedPeriod.Id, nextStatus);
            SuccessMessage = closing ? "تم إقفال الفترة المالية." : "تمت إعادة فتح الفترة المالية.";
            await LoadPeriodsAsync(SelectedPeriod.Id);
        });
    }

    private async Task LoadPeriodsAsync(Guid? selectedPeriodId = null)
    {
        Periods.Clear();
        SelectedPeriod = null;

        if (_companyId == Guid.Empty || SelectedYear is null)
        {
            OnPropertyChanged(nameof(PeriodsSummaryText));
            return;
        }

        var periods = await _periodQuery.GetFinancialPeriodsAsync(_companyId, SelectedYear.Id);
        foreach (var period in periods)
        {
            Periods.Add(new FinancialPeriodRowVm(
                period.Id,
                period.Code,
                period.Name,
                period.StartDate,
                period.EndDate,
                period.Status,
                period.IsAdjustmentPeriod,
                period.EntryCount));
        }

        SelectedPeriod = selectedPeriodId is Guid id
            ? Periods.FirstOrDefault(x => x.Id == id)
            : Periods.FirstOrDefault();

        OnPropertyChanged(nameof(PeriodsSummaryText));
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
