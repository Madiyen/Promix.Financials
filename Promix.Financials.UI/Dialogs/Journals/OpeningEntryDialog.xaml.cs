using System;
using System.Collections.Generic;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Promix.Financials.Application.Features.Journals.Commands;
using Promix.Financials.Domain.Enums;
using Promix.Financials.UI.ViewModels.Journals;
using Promix.Financials.UI.ViewModels.Journals.Models;
using Promix.Financials.UI.ViewModels.Parties.Models;
using Windows.Foundation;
using Windows.System;

namespace Promix.Financials.UI.Dialogs.Journals;

public sealed partial class OpeningEntryDialog : ContentDialog
{
    private readonly Guid _companyId;

    public OpeningEntryDialog(Guid companyId, IReadOnlyList<JournalAccountOptionVm> accounts, IReadOnlyList<PartyOptionVm> parties)
    {
        InitializeComponent();
        _companyId = companyId;
        ViewModel = new JournalEntryEditorViewModel(
            accounts,
            null,
            parties,
            JournalEntryType.OpeningEntry,
            "قيد افتتاحي",
            "قيد مخصص لفتح السنة المالية أو معالجة حالات استثنائية معتمدة من الإدارة.",
            "هذا القيد يُنشأ تلقائياً عند افتتاح السنة المالية الجديدة بناءً على أرصدة الإقفال للسنة السابقة. يُستخدم هذا النوع لأغراض خاصة أو استثنائية وفق صلاحيات الإدارة، ولا يُنصح بإنشائه يدوياً إلا عند الحاجة المحاسبية المعتمدة.");
        DataContext = ViewModel;

        PrimaryButtonClick += OnPrimaryButtonClick;
        SecondaryButtonClick += OnSecondaryButtonClick;
        RegisterKeyboardAccelerators();
    }

    public JournalEntryEditorViewModel ViewModel { get; }
    public CreateJournalEntryCommand? ResultCommand { get; private set; }

    private void AddLine_Click(object sender, RoutedEventArgs e)
        => ViewModel.AddLine();

    private void RemoveLine_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button button || button.DataContext is not JournalEntryLineEditorVm line)
            return;

        ViewModel.RemoveLine(line);
    }

    private void OnPrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        => Validate(postNow: false, args);

    private void OnSecondaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        => Validate(postNow: true, args);

    private void Validate(bool postNow, ContentDialogButtonClickEventArgs args)
    {
        if (TryComplete(postNow))
            return;

        args.Cancel = true;
    }

    private bool TryComplete(bool postNow)
    {
        ErrorBanner.Visibility = Visibility.Collapsed;
        ErrorText.Text = string.Empty;

        if (!ViewModel.TryBuildCommand(_companyId, postNow, out var command, out var error))
        {
            ErrorText.Text = error;
            ErrorBanner.Visibility = Visibility.Visible;
            return false;
        }

        ResultCommand = command;
        return true;
    }

    private void QuickDateInputBox_LostFocus(object sender, RoutedEventArgs e)
        => ApplyQuickDateInput();

    private void QuickDateInputBox_KeyDown(object sender, KeyRoutedEventArgs e)
    {
        if (e.Key is not (VirtualKey.Enter or VirtualKey.Tab))
            return;

        e.Handled = true;
        ApplyQuickDateInput();
    }

    private void SmartAmountBox_LostFocus(object sender, RoutedEventArgs e)
        => DialogSmartInputHelper.TryApplyAmount(sender as NumberBox, ShowValidationError);

    private void SmartAmountBox_KeyDown(object sender, KeyRoutedEventArgs e)
    {
        if (e.Key is not (VirtualKey.Enter or VirtualKey.Tab))
            return;

        e.Handled = true;
        DialogSmartInputHelper.TryApplyAmount(sender as NumberBox, ShowValidationError);
    }

    private void RegisterKeyboardAccelerators()
    {
        KeyboardAccelerators.Add(DialogSmartInputHelper.CreateAccelerator(
            VirtualKey.N,
            (_, args) =>
            {
                args.Handled = true;
                ViewModel.AddLine();
            },
            VirtualKeyModifiers.Control | VirtualKeyModifiers.Shift));

        KeyboardAccelerators.Add(DialogSmartInputHelper.CreateAccelerator(
            VirtualKey.S,
            (_, args) =>
            {
                args.Handled = true;
                if (TryComplete(postNow: false))
                    Hide();
            }));

        KeyboardAccelerators.Add(DialogSmartInputHelper.CreateAccelerator(
            VirtualKey.Enter,
            (_, args) =>
            {
                args.Handled = true;
                if (TryComplete(postNow: true))
                    Hide();
            }));

        KeyboardAccelerators.Add(DialogSmartInputHelper.CreateAccelerator(
            VirtualKey.Escape,
            (_, args) =>
            {
                args.Handled = true;
                Hide();
            },
            VirtualKeyModifiers.None));
    }

    private void ApplyQuickDateInput()
        => DialogSmartInputHelper.TryApplyDate(QuickDateInputBox, value => ViewModel.EntryDate = value, ShowValidationError);

    private void ShowValidationError(string message)
    {
        ErrorText.Text = message;
        ErrorBanner.Visibility = Visibility.Visible;
    }
}
