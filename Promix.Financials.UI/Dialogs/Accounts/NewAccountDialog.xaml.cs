using Microsoft.UI.Xaml.Controls;
using Promix.Financials.UI.ViewModels.Accounts;

namespace Promix.Financials.UI.Dialogs.Accounts;

public sealed partial class NewAccountDialog : ContentDialog
{
    public NewAccountDialog(NewAccountDialogViewModel vm)
    {
        InitializeComponent();
        DataContext = vm;

        // ✅ تحديث زر الإنشاء عند كل تغيير في CanSubmit
        vm.PropertyChanged += (_, args) =>
        {
            if (args.PropertyName is nameof(vm.CanSubmit) or null)
                IsPrimaryButtonEnabled = vm.CanSubmit;
        };

        // ✅ القيمة الابتدائية
        IsPrimaryButtonEnabled = vm.CanSubmit;

        // ✅ منع الإغلاق إذا كان غير صالح
        PrimaryButtonClick += (_, args) =>
        {
            vm.Validate();
            if (!vm.CanSubmit)
                args.Cancel = true;
        };
    }
}