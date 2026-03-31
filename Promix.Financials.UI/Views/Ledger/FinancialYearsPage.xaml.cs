using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Promix.Financials.Application.Abstractions;
using Promix.Financials.Application.Features.FinancialYears.Commands;
using Promix.Financials.UI.Dialogs.Ledger;
using Promix.Financials.UI.ViewModels.Ledger;
using Promix.Financials.UI.ViewModels.Ledger.Models;
using System;

namespace Promix.Financials.UI.Views.Ledger;

public sealed partial class FinancialYearsPage : Page
{
    private readonly IServiceScope _scope;
    private readonly FinancialYearsViewModel _vm;
    private readonly IUserContext _userContext;

    public FinancialYearsPage()
    {
        var app = (App)Microsoft.UI.Xaml.Application.Current;
        _scope = app.Services.CreateScope();
        _vm = _scope.ServiceProvider.GetRequiredService<FinancialYearsViewModel>();
        _userContext = _scope.ServiceProvider.GetRequiredService<IUserContext>();

        InitializeComponent();
        DataContext = _vm;

        Loaded += OnLoaded;
        Unloaded += (_, _) => _scope.Dispose();
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (_userContext.CompanyId is not Guid companyId)
            return;

        await _vm.InitializeAsync(companyId);
    }

    private async void RefreshFinancialYears_Click(object sender, RoutedEventArgs e)
    {
        await _vm.RefreshAsync();
    }

    private void FinancialYearsListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (FinancialYearsListView.SelectedItem is FinancialYearRowVm row)
            _vm.SelectedYear = row;
    }

    private async void CreateFinancialYear_Click(object sender, RoutedEventArgs e)
    {
        if (_userContext.CompanyId is not Guid companyId)
            return;

        var today = DateTime.Today;
        var dialog = new FinancialYearDialog
        {
            XamlRoot = XamlRoot
        };

        dialog.ConfigureForCreate(
            suggestedCode: $"FY-{today.Year}",
            suggestedName: $"السنة المالية {today.Year}",
            startDate: new DateOnly(today.Year, 1, 1),
            endDate: new DateOnly(today.Year, 12, 31),
            setActiveByDefault: _vm.Years.Count == 0);

        if (await dialog.ShowAsync() != ContentDialogResult.Primary)
            return;

        await _vm.CreateAsync(new CreateFinancialYearCommand(
            companyId,
            dialog.Code,
            dialog.YearName,
            dialog.StartDate,
            dialog.EndDate,
            dialog.SetActive));
    }

    private async void EditFinancialYear_Click(object sender, RoutedEventArgs e)
    {
        if (_userContext.CompanyId is not Guid companyId || _vm.SelectedYear is not { } selectedYear)
            return;

        var dialog = new FinancialYearDialog
        {
            XamlRoot = XamlRoot
        };

        dialog.ConfigureForEdit(
            selectedYear.Code,
            selectedYear.Name,
            selectedYear.StartDate,
            selectedYear.EndDate,
            selectedYear.IsActive);

        if (await dialog.ShowAsync() != ContentDialogResult.Primary)
            return;

        await _vm.EditAsync(new EditFinancialYearCommand(
            companyId,
            selectedYear.Id,
            dialog.Code,
            dialog.YearName,
            dialog.StartDate,
            dialog.EndDate));
    }

    private async void ActivateFinancialYear_Click(object sender, RoutedEventArgs e)
    {
        if (_vm.SelectedYear is null)
            return;

        var confirm = new ContentDialog
        {
            Title = "تفعيل السنة المالية",
            Content = $"سيتم اعتماد السنة {_vm.SelectedYear.DisplayName} كسنة مالية نشطة. هل تريد المتابعة؟",
            PrimaryButtonText = "تفعيل",
            CloseButtonText = "إلغاء",
            XamlRoot = XamlRoot
        };

        if (await confirm.ShowAsync() != ContentDialogResult.Primary)
            return;

        await _vm.ActivateSelectedAsync();
    }
}
