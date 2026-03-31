using Microsoft.UI.Xaml.Controls;
using System;

namespace Promix.Financials.UI.Dialogs.Ledger;

public sealed partial class FinancialYearDialog : ContentDialog
{
    public FinancialYearDialog()
    {
        InitializeComponent();

        var today = DateTime.Today;
        StartDatePicker.Date = new DateTimeOffset(new DateTime(today.Year, 1, 1));
        EndDatePicker.Date = new DateTimeOffset(new DateTime(today.Year, 12, 31));
    }

    public string Code => CodeTextBox.Text?.Trim() ?? string.Empty;
    public string YearName => NameTextBox.Text?.Trim() ?? string.Empty;
    public DateOnly StartDate => DateOnly.FromDateTime(StartDatePicker.Date.Date);
    public DateOnly EndDate => DateOnly.FromDateTime(EndDatePicker.Date.Date);
    public bool SetActive => SetActiveCheckBox.IsChecked == true;

    public void ConfigureForCreate(string suggestedCode, string suggestedName, DateOnly startDate, DateOnly endDate, bool setActiveByDefault)
    {
        Title = "سنة مالية جديدة";
        SubtitleText.Text = "أدخل بيانات السنة المالية الجديدة. يمكنك تفعيلها مباشرة بعد الحفظ.";
        CodeTextBox.Text = suggestedCode;
        NameTextBox.Text = suggestedName;
        StartDatePicker.Date = new DateTimeOffset(startDate.ToDateTime(TimeOnly.MinValue));
        EndDatePicker.Date = new DateTimeOffset(endDate.ToDateTime(TimeOnly.MinValue));
        SetActiveCheckBox.IsChecked = setActiveByDefault;
    }

    public void ConfigureForEdit(string code, string name, DateOnly startDate, DateOnly endDate, bool isActive)
    {
        Title = "تعديل السنة المالية";
        SubtitleText.Text = "عدّل بيانات السنة المالية الحالية. التفعيل يتم من الصفحة الرئيسية إذا لزم.";
        CodeTextBox.Text = code;
        NameTextBox.Text = name;
        StartDatePicker.Date = new DateTimeOffset(startDate.ToDateTime(TimeOnly.MinValue));
        EndDatePicker.Date = new DateTimeOffset(endDate.ToDateTime(TimeOnly.MinValue));
        SetActiveCheckBox.IsChecked = isActive;
        SetActiveCheckBox.Visibility = Microsoft.UI.Xaml.Visibility.Collapsed;
    }

    private void ContentDialog_PrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
    {
        if (string.IsNullOrWhiteSpace(Code))
        {
            args.Cancel = true;
            ValidationHintText.Text = "الرمز مطلوب.";
            ValidationHintText.Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.DarkRed);
            return;
        }

        if (string.IsNullOrWhiteSpace(YearName))
        {
            args.Cancel = true;
            ValidationHintText.Text = "الاسم مطلوب.";
            ValidationHintText.Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.DarkRed);
            return;
        }

        if (EndDate < StartDate)
        {
            args.Cancel = true;
            ValidationHintText.Text = "تاريخ النهاية يجب أن يكون بعد تاريخ البداية.";
            ValidationHintText.Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.DarkRed);
        }
    }
}
