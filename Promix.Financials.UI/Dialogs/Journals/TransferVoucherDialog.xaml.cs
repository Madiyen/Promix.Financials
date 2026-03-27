using System;
using System.Collections.Generic;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Promix.Financials.Application.Features.Journals.Commands;
using Promix.Financials.Application.Features.Journals.Queries;
using Promix.Financials.UI.Services.Journals;
using Promix.Financials.UI.ViewModels.Journals;
using Promix.Financials.UI.ViewModels.Journals.Models;
using Windows.Foundation;
using Windows.System;

namespace Promix.Financials.UI.Dialogs.Journals;

public sealed partial class TransferVoucherDialog : ContentDialog
{
    private readonly Guid _companyId;

    public TransferVoucherDialog(
        Guid companyId,
        IReadOnlyList<JournalAccountOptionVm> accounts,
        IReadOnlyList<JournalCurrencyOptionVm> currencies,
        IJournalEntriesQuery query)
    {
        InitializeComponent();
        _companyId = companyId;
        var app = (App)Microsoft.UI.Xaml.Application.Current;
        var quickDefaultsStore = app.Services.GetService<IJournalQuickDefaultsStore>();
        ViewModel = new TransferVoucherEditorViewModel(companyId, accounts, currencies, query, quickDefaultsStore);
        DataContext = ViewModel;

        PrimaryButtonClick += OnPrimaryButtonClick;
        SecondaryButtonClick += OnSecondaryButtonClick;
        RegisterKeyboardAccelerators();
    }

    public TransferVoucherEditorViewModel ViewModel { get; }
    public CreateJournalEntryCommand? ResultCommand { get; private set; }

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

    private void RegisterKeyboardAccelerators()
    {
        KeyboardAccelerators.Add(CreateAccelerator(VirtualKey.S, (_, args) =>
        {
            args.Handled = true;
            if (TryComplete(postNow: false))
                Hide();
        }));

        KeyboardAccelerators.Add(CreateAccelerator(VirtualKey.Enter, (_, args) =>
        {
            args.Handled = true;
            if (TryComplete(postNow: true))
                Hide();
        }));
    }

    private static KeyboardAccelerator CreateAccelerator(
        VirtualKey key,
        TypedEventHandler<KeyboardAccelerator, KeyboardAcceleratorInvokedEventArgs> handler)
    {
        var accelerator = new KeyboardAccelerator
        {
            Key = key,
            Modifiers = VirtualKeyModifiers.Control
        };
        accelerator.Invoked += handler;
        return accelerator;
    }
}
