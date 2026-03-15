using Promix.Financials.Application.Features.Currencies.Commands;
using Promix.Financials.Application.Features.Currencies.Queries;
using Promix.Financials.Application.Features.Currencies.Services;
using Promix.Financials.UI.ViewModels.Accounts;
using Promix.Financials.UI.ViewModels.Currencies.Models;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows.Input;

namespace Promix.Financials.UI.ViewModels.Currencies;

public sealed class CompanyCurrenciesViewModel : INotifyPropertyChanged
{
    private readonly CompanyCurrenciesQuery _query;
    private readonly CreateCompanyCurrencyService _createService;
    private readonly EditCompanyCurrencyService _editService;
    private readonly DeactivateCompanyCurrencyService _deactivateService;

    private Guid _companyId;

    public ObservableCollection<CompanyCurrencyRowVm> Currencies { get; } = new();

    // ─── IsBusy ───────────────────────────────────────────────────
    private bool _isBusy;
    public bool IsBusy
    {
        get => _isBusy;
        private set { if (_isBusy == value) return; _isBusy = value; OnPropertyChanged(); }
    }

    // ─── ErrorMessage ─────────────────────────────────────────────
    private string? _errorMessage;
    public string? ErrorMessage
    {
        get => _errorMessage;
        private set { if (_errorMessage == value) return; _errorMessage = value; OnPropertyChanged(); OnPropertyChanged(nameof(HasError)); }
    }
    public bool HasError => !string.IsNullOrWhiteSpace(ErrorMessage);

    // ─── SuccessMessage ───────────────────────────────────────────
    private string? _successMessage;
    public string? SuccessMessage
    {
        get => _successMessage;
        private set { if (_successMessage == value) return; _successMessage = value; OnPropertyChanged(); OnPropertyChanged(nameof(HasSuccess)); }
    }
    public bool HasSuccess => !string.IsNullOrWhiteSpace(SuccessMessage);

    // ─── Selected ─────────────────────────────────────────────────
    private CompanyCurrencyRowVm? _selectedCurrency;
    public CompanyCurrencyRowVm? SelectedCurrency
    {
        get => _selectedCurrency;
        set
        {
            if (_selectedCurrency == value) return;
            _selectedCurrency = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(CanEdit));
            OnPropertyChanged(nameof(CanDeactivate));
        }
    }

    public bool CanEdit => SelectedCurrency is not null;
    public bool CanDeactivate => SelectedCurrency is { IsActive: true, IsBaseCurrency: false };

    // ─── KPIs ─────────────────────────────────────────────────────
    private string _totalCurrenciesText = "—";
    public string TotalCurrenciesText
    {
        get => _totalCurrenciesText;
        private set { if (_totalCurrenciesText == value) return; _totalCurrenciesText = value; OnPropertyChanged(); }
    }

    private string _activeCurrenciesText = "—";
    public string ActiveCurrenciesText
    {
        get => _activeCurrenciesText;
        private set { if (_activeCurrenciesText == value) return; _activeCurrenciesText = value; OnPropertyChanged(); }
    }

    private string _baseCurrencyText = "—";
    public string BaseCurrencyText
    {
        get => _baseCurrencyText;
        private set { if (_baseCurrencyText == value) return; _baseCurrencyText = value; OnPropertyChanged(); }
    }

    // ─── Commands ─────────────────────────────────────────────────
    public AsyncRelayCommand RefreshCommand { get; }

    public CompanyCurrenciesViewModel(
        CompanyCurrenciesQuery query,
        CreateCompanyCurrencyService createService,
        EditCompanyCurrencyService editService,
        DeactivateCompanyCurrencyService deactivateService)
    {
        _query = query;
        _createService = createService;
        _editService = editService;
        _deactivateService = deactivateService;

        RefreshCommand = new AsyncRelayCommand(RefreshAsync, () => !IsBusy);
    }

    public async Task InitializeAsync(Guid companyId)
    {
        _companyId = companyId;
        await LoadAsync();
    }

    private async Task RefreshAsync()
    {
        if (IsBusy) return;
        await LoadAsync();
    }

    private async Task LoadAsync()
    {
        IsBusy = true;
        ErrorMessage = null;
        SuccessMessage = null;

        try
        {
            var list = await _query.GetAllAsync(_companyId);
            Currencies.Clear();
            foreach (var c in list)
            {
                Currencies.Add(new CompanyCurrencyRowVm(
                    c.Id, c.CurrencyCode, c.NameAr, c.NameEn,
                    c.Symbol, c.DecimalPlaces, c.ExchangeRate,
                    c.IsBaseCurrency, c.IsActive));
            }
            UpdateKpis();
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

    private void UpdateKpis()
    {
        TotalCurrenciesText = Currencies.Count.ToString();
        ActiveCurrenciesText = Currencies.Count(c => c.IsActive).ToString();
        BaseCurrencyText = Currencies.FirstOrDefault(c => c.IsBaseCurrency)?.CurrencyCode ?? "—";
    }

    // ─── Create ───────────────────────────────────────────────────
    public async Task<bool> CreateAsync(CreateCompanyCurrencyCommand cmd)
    {
        try
        {
            ErrorMessage = null;
            await _createService.CreateAsync(cmd);
            SuccessMessage = "تمت إضافة العملة بنجاح ✔";
            await LoadAsync();
            return true;
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
            return false;
        }
    }

    // ─── Edit ─────────────────────────────────────────────────────
    public async Task<bool> EditAsync(EditCompanyCurrencyCommand cmd)
    {
        try
        {
            ErrorMessage = null;
            await _editService.EditAsync(cmd);
            SuccessMessage = "تم تعديل العملة بنجاح ✔";
            await LoadAsync();
            return true;
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
            return false;
        }
    }

    // ─── Deactivate ───────────────────────────────────────────────
    public async Task<bool> DeactivateAsync()
    {
        if (SelectedCurrency is null) return false;
        try
        {
            ErrorMessage = null;
            await _deactivateService.DeactivateAsync(
                new DeactivateCompanyCurrencyCommand(SelectedCurrency.Id, _companyId));
            SuccessMessage = "تم إيقاف العملة ✔";
            await LoadAsync();
            return true;
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
            return false;
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    private void OnPropertyChanged([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}

// ─── AsyncRelayCommand (نفس النمط الموجود في المشروع) ────────────
public sealed class CurrencyAsyncRelayCommand : ICommand
{
    private readonly Func<Task> _execute;
    private readonly Func<bool>? _canExecute;
    private bool _isExecuting;

    public CurrencyAsyncRelayCommand(Func<Task> execute, Func<bool>? canExecute = null)
    {
        _execute = execute;
        _canExecute = canExecute;
    }

    public bool CanExecute(object? parameter) => !_isExecuting && (_canExecute?.Invoke() ?? true);

    public async void Execute(object? parameter) => await ExecuteAsync();

    public async Task ExecuteAsync()
    {
        if (!CanExecute(null)) return;
        try { _isExecuting = true; RaiseCanExecuteChanged(); await _execute(); }
        finally { _isExecuting = false; RaiseCanExecuteChanged(); }
    }

    public event EventHandler? CanExecuteChanged;
    public void RaiseCanExecuteChanged() => CanExecuteChanged?.Invoke(this, EventArgs.Empty);
}