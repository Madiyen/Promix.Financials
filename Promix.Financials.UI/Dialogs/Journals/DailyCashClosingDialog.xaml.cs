using System;
using System.Collections.Generic;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Promix.Financials.Application.Features.Journals.Commands;
using Promix.Financials.UI.ViewModels.Journals;
using Promix.Financials.UI.ViewModels.Journals.Models;
using Windows.System;

namespace Promix.Financials.UI.Dialogs.Journals;

public sealed partial class DailyCashClosingDialog : ContentDialog
{
    private readonly Guid _companyId;

    public DailyCashClosingDialog(Guid companyId, IReadOnlyList<JournalAccountOptionVm> accounts)
    {
        InitializeComponent();
        _companyId = companyId;
        ViewModel = new DailyCashClosingEditorViewModel(accounts);
        DataContext = ViewModel;
        PrimaryButtonClick += OnPrimaryButtonClick;
        RegisterKeyboardAccelerators();
    }

    public DailyCashClosingEditorViewModel ViewModel { get; }
    public CreateDailyCashClosingCommand? ResultCommand { get; private set; }

    private void OnPrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
    {
        if (TryComplete())
            return;

        args.Cancel = true;
    }

    private bool TryComplete()
    {
        ErrorBanner.Visibility = Visibility.Collapsed;
        ErrorText.Text = string.Empty;

        if (!ViewModel.TryBuildCommand(_companyId, out var command, out var error))
        {
            ErrorText.Text = error;
            ErrorBanner.Visibility = Visibility.Visible;
            return false;
        }

        ResultCommand = command;
        return true;
    }

    private void RegisterKeyboardAccelerators()
    {
        var accelerator = new KeyboardAccelerator
        {
            Key = VirtualKey.Enter,
            Modifiers = VirtualKeyModifiers.Control
        };
        accelerator.Invoked += (_, args) =>
        {
            args.Handled = true;
            if (TryComplete())
                Hide();
        };
        KeyboardAccelerators.Add(accelerator);
    }
}
