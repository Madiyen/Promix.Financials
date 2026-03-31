using System;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using Promix.Financials.Application.Abstractions;
using Promix.Financials.Domain.Enums;
using Promix.Financials.UI.Dialogs.Parties;
using Promix.Financials.UI.ViewModels.Parties;
using Promix.Financials.UI.ViewModels.Parties.Models;

namespace Promix.Financials.UI.Views.Parties;

public sealed partial class PartiesPage : Page
{
    private readonly IServiceScope _scope;
    private readonly PartiesPageViewModel _vm;
    private readonly IUserContext _userContext;
    private bool _isViewReady;

    public PartiesPage()
    {
        var app = (App)Microsoft.UI.Xaml.Application.Current;
        _scope = app.Services.CreateScope();
        _vm = _scope.ServiceProvider.GetRequiredService<PartiesPageViewModel>();
        _userContext = _scope.ServiceProvider.GetRequiredService<IUserContext>();

        InitializeComponent();
        DataContext = _vm;
        _isViewReady = true;

        _vm.PropertyChanged += ViewModel_PropertyChanged;
        _vm.Parties.CollectionChanged += ViewModelCollectionChanged;

        Unloaded += PartiesPage_Unloaded;
        UpdateMessageBanners();
        UpdateVisualState();
    }

    protected override async void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);

        if (_userContext.CompanyId is not Guid companyId)
            return;

        await RunSafeAsync(() => _vm.InitializeAsync(companyId));
    }

    private async void Refresh_Click(object sender, RoutedEventArgs e)
        => await RunSafeAsync(() => _vm.RefreshAsync());

    private async void RefreshStatement_Click(object sender, RoutedEventArgs e)
        => await RunSafeAsync(() => _vm.ReloadSelectedStatementAsync());

    private async void AddParty_Click(object sender, RoutedEventArgs e)
    {
        if (_userContext.CompanyId is not Guid companyId)
            return;

        await OpenPartyDialogFlowAsync(companyId, null);
    }

    private async void EditParty_Click(object sender, RoutedEventArgs e)
    {
        if (_userContext.CompanyId is not Guid companyId || _vm.SelectedParty is not { } selectedParty)
            return;

        await OpenPartyDialogFlowAsync(companyId, selectedParty);
    }

    private async void DeactivateParty_Click(object sender, RoutedEventArgs e)
    {
        if (_vm.SelectedParty is null)
            return;

        var confirm = new ContentDialog
        {
            Title = "إيقاف التعامل مع الطرف",
            Content = $"سيبقى تاريخ الطرف {_vm.SelectedParty.NameAr} ظاهرًا في الكشوف والتقارير، لكن لن يظهر في السندات الجديدة. هل تريد المتابعة؟",
            PrimaryButtonText = "إيقاف التعامل",
            CloseButtonText = "إلغاء",
            DefaultButton = ContentDialogButton.Close,
            XamlRoot = XamlRoot
        };

        if (await confirm.ShowAsync() != ContentDialogResult.Primary)
            return;

        await RunSafeAsync(() => _vm.DeactivateSelectedAsync());
    }

    private async void ActivateParty_Click(object sender, RoutedEventArgs e)
    {
        if (_vm.SelectedParty is null)
            return;

        var confirm = new ContentDialog
        {
            Title = "إعادة تنشيط الطرف",
            Content = $"سيعود الطرف {_vm.SelectedParty.NameAr} للظهور داخل السندات والقيود الجديدة. هل تريد المتابعة؟",
            PrimaryButtonText = "إعادة التنشيط",
            CloseButtonText = "إلغاء",
            DefaultButton = ContentDialogButton.Primary,
            XamlRoot = XamlRoot
        };

        if (await confirm.ShowAsync() != ContentDialogResult.Primary)
            return;

        await RunSafeAsync(() => _vm.ActivateSelectedAsync());
    }

    private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        _vm.SearchText = SearchBox.Text ?? string.Empty;
        UpdateVisualState();
    }

    private void TypeFilterComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        _vm.SelectedTypeFilter = (PartyFilterMode)TypeFilterComboBox.SelectedIndex;
        UpdateVisualState();
    }

    private void StatusFilterComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        _vm.SelectedStatusFilter = (PartyStatusFilterMode)StatusFilterComboBox.SelectedIndex;
        UpdateVisualState();
    }

    private async void PartiesListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (ReferenceEquals(PartiesListView.SelectedItem, _vm.SelectedParty))
        {
            UpdateVisualState();
            return;
        }

        await RunSafeAsync(() => _vm.SelectPartyAsync(PartiesListView.SelectedItem as PartyRowVm));
    }

    private void PartiesPage_Unloaded(object sender, RoutedEventArgs e)
    {
        _vm.PropertyChanged -= ViewModel_PropertyChanged;
        _vm.Parties.CollectionChanged -= ViewModelCollectionChanged;
        Unloaded -= PartiesPage_Unloaded;
        _scope.Dispose();
    }

    private void ViewModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(_vm.ErrorMessage) or nameof(_vm.SuccessMessage) or nameof(_vm.SelectedParty) or null)
        {
            UpdateMessageBanners();
            UpdateVisualState();
        }
    }

    private void ViewModelCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e) => UpdateVisualState();

    private async Task OpenPartyDialogFlowAsync(Guid companyId, PartyRowVm? existingParty)
    {
        var dialog = new PartyDialog(companyId, existingParty) { XamlRoot = XamlRoot };
        await dialog.ShowAsync();

        if (!dialog.IsSubmitted)
            return;

        if (existingParty is null)
            await RunSafeAsync(() => _vm.CreateAsync(dialog.BuildCreateCommand()));
        else
            await RunSafeAsync(() => _vm.EditAsync(dialog.BuildEditCommand()));

        return;
    }

    private async Task RunSafeAsync(Func<Task> action)
    {
        try
        {
            await action();
            UpdateMessageBanners();
            UpdateVisualState();
        }
        catch (Exception ex)
        {
            if (_isViewReady && ErrorBannerText is not null && ErrorBanner is not null)
            {
                ErrorBannerText.Text = ex.Message;
                ErrorBanner.Visibility = Visibility.Visible;
            }
        }
    }

    private void UpdateMessageBanners()
    {
        if (!_isViewReady || ErrorBannerText is null || ErrorBanner is null || SuccessBannerText is null || SuccessBanner is null)
            return;

        ErrorBannerText.Text = _vm.ErrorMessage ?? string.Empty;
        ErrorBanner.Visibility = string.IsNullOrWhiteSpace(_vm.ErrorMessage) ? Visibility.Collapsed : Visibility.Visible;

        SuccessBannerText.Text = _vm.SuccessMessage ?? string.Empty;
        SuccessBanner.Visibility = string.IsNullOrWhiteSpace(_vm.SuccessMessage) ? Visibility.Collapsed : Visibility.Visible;
    }

    private void UpdateVisualState()
    {
        if (!_isViewReady
            || PartiesListView is null
            || NoPartiesState is null
            || NoDetailsState is null
            || DetailsScrollViewer is null
            || DeactivatePartyButton is null
            || ActivatePartyButton is null)
            return;

        if (!ReferenceEquals(PartiesListView.SelectedItem, _vm.SelectedParty))
            PartiesListView.SelectedItem = _vm.SelectedParty;

        var hasParties = _vm.Parties.Count > 0;
        NoPartiesState.Visibility = hasParties ? Visibility.Collapsed : Visibility.Visible;
        PartiesListView.Visibility = hasParties ? Visibility.Visible : Visibility.Collapsed;

        var hasSelectedParty = _vm.SelectedParty is not null;
        NoDetailsState.Visibility = hasSelectedParty ? Visibility.Collapsed : Visibility.Visible;
        DetailsScrollViewer.Visibility = hasSelectedParty ? Visibility.Visible : Visibility.Collapsed;

        DeactivatePartyButton.Visibility = _vm.SelectedParty is { IsActive: true }
            ? Visibility.Visible
            : Visibility.Collapsed;
        ActivatePartyButton.Visibility = _vm.SelectedParty is { IsActive: false }
            ? Visibility.Visible
            : Visibility.Collapsed;
    }

}
