using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Promix.Financials.UI.ViewModels.Accounts;

namespace Promix.Financials.UI.Dialogs.Accounts;

public sealed partial class NewAccountDialog : ContentDialog
{
    private readonly NewAccountDialogViewModel _vm;

    public bool IsSubmitted { get; private set; }

    public NewAccountDialog(NewAccountDialogViewModel vm)
    {
        InitializeComponent();
        _vm = vm;
        DataContext = vm;

        vm.PropertyChanged += (_, args) =>
        {
            if (args.PropertyName is nameof(vm.CanSubmit) or null)
                UpdateSaveButtonState();

            if (args.PropertyName is nameof(vm.IsActive) or null)
                UpdateStateButtons();
        };

        Loaded += (_, _) =>
        {
            UpdateStateButtons();
            UpdateSaveButtonState();
        };
    }

    private void NewAccountActiveStateButton_Click(object sender, RoutedEventArgs e)
    {
        _vm.IsActive = true;
        UpdateStateButtons();
    }

    private void NewAccountInactiveStateButton_Click(object sender, RoutedEventArgs e)
    {
        _vm.IsActive = false;
        UpdateStateButtons();
    }

    private void SaveButton_Click(object sender, RoutedEventArgs e)
    {
        IsSubmitted = false;
        _vm.Validate();
        UpdateSaveButtonState();

        if (!_vm.CanSubmit)
            return;

        IsSubmitted = true;
        Hide();
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        IsSubmitted = false;
        Hide();
    }

    private void UpdateStateButtons()
    {
        if (NewAccountActiveStateButton is null || NewAccountInactiveStateButton is null)
            return;

        ApplyStateButtonStyle(NewAccountActiveStateButton, _vm.IsActive);
        ApplyStateButtonStyle(NewAccountInactiveStateButton, !_vm.IsActive);
    }

    private void UpdateSaveButtonState()
    {
        if (SaveFooterButton is not null)
            SaveFooterButton.IsEnabled = _vm.CanSubmit;
    }

    private void ApplyStateButtonStyle(Button button, bool isSelected)
    {
        if (((App)Microsoft.UI.Xaml.Application.Current).Resources[isSelected
                ? "FilterChipButtonSelectedStyle"
                : "FilterChipButtonStyle"] is Style style)
        {
            button.Style = style;
        }
    }
}
