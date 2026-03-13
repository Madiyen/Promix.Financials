using Microsoft.UI.Xaml.Controls;
using Promix.Financials.UI.ViewModels.Accounts;

namespace Promix.Financials.UI.Dialogs.Accounts;

public sealed partial class NewAccountDialog : ContentDialog
{
    private readonly NewAccountDialogViewModel _vm;

    public NewAccountDialog(NewAccountDialogViewModel vm)
    {
        InitializeComponent();
        _vm = vm;
        DataContext = vm;

        // ✅ ربط CanSubmit يدوياً لأن WinUI 3 لا يُحدّث IsPrimaryButtonEnabled تلقائياً
        vm.PropertyChanged += (_, args) =>
        {
            if (args.PropertyName == nameof(vm.CanSubmit))
                IsPrimaryButtonEnabled = vm.CanSubmit;
        };

        // ✅ تعيين القيمة الابتدائية
        IsPrimaryButtonEnabled = vm.CanSubmit;

        // ✅ منع إغلاق الـ Dialog إذا لم يكن صالحاً
        PrimaryButtonClick += (_, args) =>
        {
            vm.Validate();
            if (!vm.CanSubmit)
                args.Cancel = true;
        };
    }
}