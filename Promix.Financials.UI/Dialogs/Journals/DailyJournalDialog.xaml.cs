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

public sealed partial class DailyJournalDialog : ContentDialog
{
    private readonly Guid _companyId;

    public DailyJournalDialog(Guid companyId, IReadOnlyList<JournalAccountOptionVm> accounts, IReadOnlyList<PartyOptionVm> parties)
    {
        InitializeComponent();
        _companyId = companyId;
        ViewModel = new JournalEntryEditorViewModel(
            accounts,
            parties,
            JournalEntryType.DailyJournal,
            "قيد يومية",
            "قيد متعدد الأسطر للتسويات والتصحيحات والعمليات غير المغطاة بسندات القبض والصرف والتحويل.",
            "يمكن حفظ القيد كمسودة للمراجعة، لكن يجب أن يبقى متوازناً قبل الترحيل.");
        DataContext = ViewModel;
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

    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        ResultCommand = null;
        Hide();
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

    private void SaveDraftButton_Click(object sender, RoutedEventArgs e)
    {
        if (!TryComplete(postNow: false))
            return;

        Hide();
    }

    private void SaveAndPostButton_Click(object sender, RoutedEventArgs e)
    {
        if (!TryComplete(postNow: true))
            return;

        Hide();
    }

    private void RegisterKeyboardAccelerators()
    {
        KeyboardAccelerators.Add(CreateAccelerator(
            VirtualKey.N,
            VirtualKeyModifiers.Control | VirtualKeyModifiers.Shift,
            (_, args) =>
            {
                args.Handled = true;
                ViewModel.AddLine();
            }));

        KeyboardAccelerators.Add(CreateAccelerator(
            VirtualKey.S,
            VirtualKeyModifiers.Control,
            (_, args) =>
            {
                args.Handled = true;
                if (TryComplete(postNow: false))
                    Hide();
            }));

        KeyboardAccelerators.Add(CreateAccelerator(
            VirtualKey.Enter,
            VirtualKeyModifiers.Control,
            (_, args) =>
            {
                args.Handled = true;
                if (TryComplete(postNow: true))
                    Hide();
            }));
    }

    private static KeyboardAccelerator CreateAccelerator(
        VirtualKey key,
        VirtualKeyModifiers modifiers,
        TypedEventHandler<KeyboardAccelerator, KeyboardAcceleratorInvokedEventArgs> handler)
    {
        var accelerator = new KeyboardAccelerator
        {
            Key = key,
            Modifiers = modifiers
        };
        accelerator.Invoked += handler;
        return accelerator;
    }
}
