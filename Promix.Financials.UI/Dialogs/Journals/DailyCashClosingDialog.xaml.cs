using System;
using System.Collections.Generic;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Promix.Financials.Application.Features.Journals.Commands;
using Promix.Financials.UI.ViewModels.Journals;
using Promix.Financials.UI.ViewModels.Journals.Models;

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
    }

    public DailyCashClosingEditorViewModel ViewModel { get; }
    public CreateDailyCashClosingCommand? ResultCommand { get; private set; }

    private void OnPrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
    {
        ErrorBanner.Visibility = Visibility.Collapsed;
        ErrorText.Text = string.Empty;

        if (!ViewModel.TryBuildCommand(_companyId, out var command, out var error))
        {
            args.Cancel = true;
            ErrorText.Text = error;
            ErrorBanner.Visibility = Visibility.Visible;
            return;
        }

        ResultCommand = command;
    }
}
