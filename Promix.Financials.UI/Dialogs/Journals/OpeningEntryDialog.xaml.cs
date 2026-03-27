using System;
using System.Collections.Generic;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Promix.Financials.Application.Features.Journals.Commands;
using Promix.Financials.Domain.Enums;
using Promix.Financials.UI.ViewModels.Journals;
using Promix.Financials.UI.ViewModels.Journals.Models;

namespace Promix.Financials.UI.Dialogs.Journals;

public sealed partial class OpeningEntryDialog : ContentDialog
{
    private readonly Guid _companyId;

    public OpeningEntryDialog(Guid companyId, IReadOnlyList<JournalAccountOptionVm> accounts)
    {
        InitializeComponent();
        _companyId = companyId;
        ViewModel = new JournalEntryEditorViewModel(
            accounts,
            JournalEntryType.OpeningEntry,
            "قيد افتتاحي",
            "قيد مخصص لفتح السنة المالية أو معالجة حالات استثنائية معتمدة من الإدارة.",
            "هذا القيد يُنشأ تلقائياً عند افتتاح السنة المالية الجديدة بناءً على أرصدة الإقفال للسنة السابقة. يُستخدم هذا النوع لأغراض خاصة أو استثنائية وفق صلاحيات الإدارة، ولا يُنصح بإنشائه يدوياً إلا عند الحاجة المحاسبية المعتمدة.");
        DataContext = ViewModel;

        PrimaryButtonClick += OnPrimaryButtonClick;
        SecondaryButtonClick += OnSecondaryButtonClick;
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
