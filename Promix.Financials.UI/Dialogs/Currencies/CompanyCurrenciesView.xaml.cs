using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using Promix.Financials.Application.Abstractions;
using Promix.Financials.UI.Dialogs.Currencies;
using Promix.Financials.UI.ViewModels.Currencies;
using Promix.Financials.UI.ViewModels.Currencies.Models;
using System;
using WinUIApplication = Microsoft.UI.Xaml.Application; // 🆕 alias لتجنب التعارض

namespace Promix.Financials.UI.Views.Currencies;

public sealed partial class CompanyCurrenciesView : Page
{
    private readonly CompanyCurrenciesViewModel _vm;
    private readonly IUserContext _userContext;

    public CompanyCurrenciesView()
    {
        InitializeComponent();

        var services = ((App)WinUIApplication.Current).Services; // ✅ alias
        _vm = services.GetRequiredService<CompanyCurrenciesViewModel>();
        _userContext = services.GetRequiredService<IUserContext>();

        DataContext = _vm;

        // ربط الـ Banners بالـ ViewModel
        _vm.PropertyChanged += (_, args) =>
        {
            if (args.PropertyName is nameof(_vm.ErrorMessage) or null)
            {
                ErrorBannerText.Text = _vm.ErrorMessage ?? "";
                ErrorBanner.Visibility = _vm.HasError
                    ? Visibility.Visible
                    : Visibility.Collapsed;
            }
            if (args.PropertyName is nameof(_vm.SuccessMessage) or null)
            {
                SuccessBannerText.Text = _vm.SuccessMessage ?? "";
                SuccessBanner.Visibility = _vm.HasSuccess
                    ? Visibility.Visible
                    : Visibility.Collapsed;
            }
        };
    }

    protected override async void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);

        if (_userContext.CompanyId is null) return;
        await _vm.InitializeAsync(_userContext.CompanyId.Value);
    }

    // ─── إضافة عملة جديدة ─────────────────────────────────────────
    private async void AddButton_Click(object sender, RoutedEventArgs e)
    {
        if (_userContext.CompanyId is null) return;

        var dialog = new CompanyCurrencyDialog(_userContext.CompanyId.Value);
        dialog.XamlRoot = XamlRoot;

        var result = await dialog.ShowAsync();
        if (result != ContentDialogResult.Primary) return;

        var cmd = dialog.BuildCreateCommand();
        await _vm.CreateAsync(cmd);
    }

    // ─── تعديل عملة ───────────────────────────────────────────────
    private async void EditRowButton_Click(object sender, RoutedEventArgs e)
    {
        if (_userContext.CompanyId is null) return;

        if (sender is Button btn && btn.DataContext is CompanyCurrencyRowVm row)
        {
            var dialog = new CompanyCurrencyDialog(_userContext.CompanyId.Value, row);
            dialog.XamlRoot = XamlRoot;

            var result = await dialog.ShowAsync();
            if (result != ContentDialogResult.Primary) return;

            var cmd = dialog.BuildEditCommand();
            await _vm.EditAsync(cmd);
        }
    }

    // ─── إيقاف عملة ───────────────────────────────────────────────
    private async void DeactivateButton_Click(object sender, RoutedEventArgs e)
    {
        if (_vm.SelectedCurrency is null) return;

        var confirm = new ContentDialog
        {
            Title = "تأكيد الإيقاف",
            Content = $"هل تريد إيقاف عملة ({_vm.SelectedCurrency.CurrencyCode} — {_vm.SelectedCurrency.NameAr})؟",
            PrimaryButtonText = "نعم، إيقاف",
            CloseButtonText = "إلغاء",
            DefaultButton = ContentDialogButton.Close,
            XamlRoot = XamlRoot
        };

        var result = await confirm.ShowAsync();
        if (result != ContentDialogResult.Primary) return;

        await _vm.DeactivateAsync();
    }
}