using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Promix.Financials.UI.ViewModels.Accounts;

namespace Promix.Financials.UI.Dialogs.Accounts;

public sealed partial class EditAccountDialog : ContentDialog
{
    private readonly EditAccountDialogViewModel _vm;

    public EditAccountDialog(EditAccountDialogViewModel vm)
    {
        InitializeComponent();
        _vm = vm;
        DataContext = vm;

        // ✅ ربط زر الحفظ بـ CanSave
        Loaded += (_, _) =>
        {
            UpdatePrimaryButton();
            UpdateStateButtons();
        };
        vm.PropertyChanged += (_, args) =>
        {
            UpdatePrimaryButton();
            if (args.PropertyName is nameof(vm.IsActive) or null)
                UpdateStateButtons();
        };
    }

    private void UpdatePrimaryButton()
        => IsPrimaryButtonEnabled = _vm.CanSave;

    private void EditAccountActiveStateButton_Click(object sender, RoutedEventArgs e)
    {
        _vm.IsActive = true;
        UpdateStateButtons();
    }

    private void EditAccountInactiveStateButton_Click(object sender, RoutedEventArgs e)
    {
        _vm.IsActive = false;
        UpdateStateButtons();
    }

    private void UpdateStateButtons()
    {
        ApplyStateButtonStyle(EditAccountActiveStateButton, _vm.IsActive);
        ApplyStateButtonStyle(EditAccountInactiveStateButton, !_vm.IsActive);
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
