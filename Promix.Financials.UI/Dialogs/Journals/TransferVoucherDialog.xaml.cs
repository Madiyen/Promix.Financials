using System;
using System.Collections.Generic;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Promix.Financials.Application.Features.Journals.Commands;
using Promix.Financials.Application.Features.Journals.Queries;
using Promix.Financials.UI.ViewModels.Journals;
using Promix.Financials.UI.ViewModels.Journals.Models;

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
        ViewModel = new TransferVoucherEditorViewModel(companyId, accounts, currencies, query);
        DataContext = ViewModel;

        PrimaryButtonClick += OnPrimaryButtonClick;
        SecondaryButtonClick += OnSecondaryButtonClick;
    }

    public TransferVoucherEditorViewModel ViewModel { get; }
    public CreateJournalEntryCommand? ResultCommand { get; private set; }

    private void OnPrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        => Validate(postNow: false, args);

    private void OnSecondaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        => Validate(postNow: true, args);

    private void Validate(bool postNow, ContentDialogButtonClickEventArgs args)
    {
        ErrorBanner.Visibility = Visibility.Collapsed;
        ErrorText.Text = string.Empty;

        if (!ViewModel.TryBuildCommand(_companyId, postNow, out var command, out var error))
        {
            args.Cancel = true;
            ErrorText.Text = error;
            ErrorBanner.Visibility = Visibility.Visible;
            return;
        }

        ResultCommand = command;
    }
}
