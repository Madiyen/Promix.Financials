using System;
using System.Collections.Generic;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Promix.Financials.Application.Features.Journals.Commands;
using Promix.Financials.Application.Features.Journals.Queries;
using Promix.Financials.Application.Features.Parties.Queries;
using Promix.Financials.Domain.Enums;
using Promix.Financials.UI.Services.Journals;
using Promix.Financials.UI.ViewModels.Journals;
using Promix.Financials.UI.ViewModels.Journals.Models;
using Promix.Financials.UI.ViewModels.Parties.Models;
using Windows.Foundation;
using Windows.System;

namespace Promix.Financials.UI.Dialogs.Journals;

public sealed partial class PaymentVoucherDialog : ContentDialog
{
    private readonly Guid _companyId;

    public PaymentVoucherDialog(
        Guid companyId,
        IReadOnlyList<JournalAccountOptionVm> accounts,
        IReadOnlyList<JournalCurrencyOptionVm> currencies,
        IReadOnlyList<PartyOptionVm> parties,
        IJournalEntriesQuery query,
        Guid? defaultAccountId = null)
        : this(companyId, accounts, currencies, parties, query, detail: null, canManage: false)
    {
        if (defaultAccountId is Guid accountId && accountId != Guid.Empty)
            ViewModel.ApplyAccountStatementDefaults(accountId);
    }

    public PaymentVoucherDialog(
        Guid companyId,
        IReadOnlyList<JournalAccountOptionVm> accounts,
        IReadOnlyList<JournalCurrencyOptionVm> currencies,
        IReadOnlyList<PartyOptionVm> parties,
        IJournalEntriesQuery query,
        JournalEntryDetailDto? detail,
        bool canManage)
    {
        InitializeComponent();
        _companyId = companyId;
        var app = (App)Microsoft.UI.Xaml.Application.Current;
        var quickDefaultsStore = app.Services.GetService<IJournalQuickDefaultsStore>();
        var partyQuery = app.Services.GetRequiredService<IPartyQuery>();
        ViewModel = new SimpleVoucherEditorViewModel(
            companyId,
            JournalEntryType.PaymentVoucher,
            "سند صرف",
            "صرف نقدي أو من خزينة مع توليد القيد المقابل تلقائياً وقابلية تغيير الحساب النقدي متى لزم.",
            accounts,
            currencies,
            parties,
            query,
            partyQuery,
            quickDefaultsStore,
            detail,
            canManage);

        DataContext = ViewModel;
        RegisterKeyboardAccelerators();
    }

    public SimpleVoucherEditorViewModel ViewModel { get; }
    public CreateJournalEntryCommand? ResultCommand { get; private set; }
    public UpdateJournalEntryCommand? UpdateCommand { get; private set; }
    public DeleteJournalEntryCommand? DeleteCommand { get; private set; }

    private bool TryCompleteCreate(bool postNow)
        => TryComplete(
            buildCommand: () => ViewModel.TryBuildCommand(_companyId, postNow, out var command, out var error)
                ? (command, error)
                : (null, error));

    private bool TryCompleteUpdate(bool postNow)
        => TryComplete(
            buildCommand: () => ViewModel.TryBuildUpdateCommand(_companyId, postNow, out var command, out var error)
                ? (command, error)
                : (null, error),
            isUpdate: true);

    private bool TryComplete(Func<(object? Command, string Error)> buildCommand, bool isUpdate = false)
    {
        ErrorBanner.Visibility = Visibility.Collapsed;
        ErrorText.Text = string.Empty;

        var result = buildCommand();
        if (result.Command is null)
        {
            ErrorText.Text = result.Error;
            ErrorBanner.Visibility = Visibility.Visible;
            return false;
        }

        if (isUpdate)
            UpdateCommand = (UpdateJournalEntryCommand)result.Command;
        else
            ResultCommand = (CreateJournalEntryCommand)result.Command;

        return true;
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        ResultCommand = null;
        UpdateCommand = null;
        DeleteCommand = null;
        Hide();
    }

    private void EditButton_Click(object sender, RoutedEventArgs e) => ViewModel.BeginEdit();

    private void AddLineButton_Click(object sender, RoutedEventArgs e) => ViewModel.AddLine();

    private void RemoveLineButton_Click(object sender, RoutedEventArgs e)
    {
        if ((sender as FrameworkElement)?.DataContext is VoucherCounterpartyLineEditorVm line)
            ViewModel.RemoveLine(line);
    }

    private void DeleteButton_Click(object sender, RoutedEventArgs e)
    {
        ErrorBanner.Visibility = Visibility.Collapsed;
        if (!ViewModel.TryBuildDeleteCommand(_companyId, out var command, out var error))
        {
            ErrorText.Text = error;
            ErrorBanner.Visibility = Visibility.Visible;
            return;
        }

        DeleteCommand = command;
        Hide();
    }

    private void SaveDraftButton_Click(object sender, RoutedEventArgs e)
    {
        var success = ViewModel.IsExistingEntry
            ? TryCompleteUpdate(postNow: false)
            : TryCompleteCreate(postNow: false);

        if (success)
            Hide();
    }

    private void SaveAndPostButton_Click(object sender, RoutedEventArgs e)
    {
        var success = ViewModel.IsExistingEntry
            ? TryCompleteUpdate(postNow: true)
            : TryCompleteCreate(postNow: true);

        if (success)
            Hide();
    }

    private void SaveChangesButton_Click(object sender, RoutedEventArgs e)
    {
        if (!TryCompleteUpdate(postNow: false))
            return;

        Hide();
    }

    private void RegisterKeyboardAccelerators()
    {
        KeyboardAccelerators.Add(CreateAccelerator(VirtualKey.S, (_, args) =>
        {
            args.Handled = true;
            var success = ViewModel.SaveChangesButtonVisibility == Visibility.Visible
                ? TryCompleteUpdate(postNow: false)
                : ViewModel.IsExistingEntry
                    ? TryCompleteUpdate(postNow: false)
                    : TryCompleteCreate(postNow: false);
            if (success)
                Hide();
        }));

        KeyboardAccelerators.Add(CreateAccelerator(VirtualKey.Enter, (_, args) =>
        {
            if (ViewModel.SaveAndPostButtonVisibility != Visibility.Visible)
                return;

            args.Handled = true;
            var success = ViewModel.IsExistingEntry
                ? TryCompleteUpdate(postNow: true)
                : TryCompleteCreate(postNow: true);
            if (success)
                Hide();
        }));

        KeyboardAccelerators.Add(CreateAccelerator(VirtualKey.N, (_, args) =>
        {
            if (ViewModel.AddLineButtonVisibility != Visibility.Visible)
                return;

            args.Handled = true;
            ViewModel.AddLine();
        }, VirtualKeyModifiers.Control | VirtualKeyModifiers.Shift));
    }

    private static KeyboardAccelerator CreateAccelerator(
        VirtualKey key,
        TypedEventHandler<KeyboardAccelerator, KeyboardAcceleratorInvokedEventArgs> handler,
        VirtualKeyModifiers modifiers = VirtualKeyModifiers.Control)
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
