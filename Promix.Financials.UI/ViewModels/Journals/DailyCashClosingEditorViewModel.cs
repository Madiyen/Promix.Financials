using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using Promix.Financials.Application.Features.Journals.Commands;
using Promix.Financials.UI.ViewModels.Journals.Models;

namespace Promix.Financials.UI.ViewModels.Journals;

public sealed class DailyCashClosingEditorViewModel : INotifyPropertyChanged
{
    private Guid? _selectedSourceAccountId;
    private Guid? _selectedTargetAccountId;
    private DateTimeOffset _entryDate = DateTimeOffset.Now;
    private string _referenceNo = string.Empty;
    private string _description = string.Empty;
    private bool _lockThroughEntryDate = true;

    public DailyCashClosingEditorViewModel(IEnumerable<JournalAccountOptionVm> accounts)
    {
        AccountOptions = new ObservableCollection<JournalAccountOptionVm>(accounts.OrderBy(x => x.Code));
        _selectedSourceAccountId = JournalAccountDefaultsResolver.ResolvePreferredCashAccount(AccountOptions);
        _selectedTargetAccountId = JournalAccountDefaultsResolver.ResolveMainTreasuryAccount(AccountOptions);

        if (_selectedSourceAccountId == _selectedTargetAccountId)
            _selectedTargetAccountId = AccountOptions.FirstOrDefault(x => x.Id != _selectedSourceAccountId)?.Id;
    }

    public ObservableCollection<JournalAccountOptionVm> AccountOptions { get; }

    public Guid? SelectedSourceAccountId
    {
        get => _selectedSourceAccountId;
        set
        {
            if (_selectedSourceAccountId == value) return;
            _selectedSourceAccountId = value;
            OnPropertyChanged();
        }
    }

    public Guid? SelectedTargetAccountId
    {
        get => _selectedTargetAccountId;
        set
        {
            if (_selectedTargetAccountId == value) return;
            _selectedTargetAccountId = value;
            OnPropertyChanged();
        }
    }

    public DateTimeOffset EntryDate
    {
        get => _entryDate;
        set
        {
            if (_entryDate == value) return;
            _entryDate = value;
            OnPropertyChanged();
        }
    }

    public string ReferenceNo
    {
        get => _referenceNo;
        set
        {
            if (_referenceNo == value) return;
            _referenceNo = value;
            OnPropertyChanged();
        }
    }

    public string Description
    {
        get => _description;
        set
        {
            if (_description == value) return;
            _description = value;
            OnPropertyChanged();
        }
    }

    public string HintText => "سيقوم النظام بحساب صافي الحركة المرحلة على الحساب المحدد في هذا التاريخ، ثم يولد قيد إقفال يصفّر الحساب التشغيلي وينقل الرصيد إلى الحساب المقابل.";
    public string LockPeriodHintText => LockThroughEntryDate
        ? "سيعتبر هذا التاريخ آخر يوم مفتوح، ولن يسمح النظام بإنشاء أو ترحيل سندات داخل هذه الفترة بعد حفظ الإقفال."
        : "سيُنشأ سند الإقفال فقط بدون قفل الفترة، ويمكنك متابعة الإدخال على نفس التاريخ لاحقاً.";

    public bool LockThroughEntryDate
    {
        get => _lockThroughEntryDate;
        set
        {
            if (_lockThroughEntryDate == value) return;
            _lockThroughEntryDate = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(LockPeriodHintText));
        }
    }

    public bool TryBuildCommand(Guid companyId, out CreateDailyCashClosingCommand? command, out string error)
    {
        command = null;
        error = string.Empty;

        if (SelectedSourceAccountId is null || SelectedTargetAccountId is null)
        {
            error = "اختر حساب الإقفال والحساب المقابل.";
            return false;
        }

        if (SelectedSourceAccountId == SelectedTargetAccountId)
        {
            error = "يجب أن يختلف حساب الإقفال عن الحساب المقابل.";
            return false;
        }

        command = new CreateDailyCashClosingCommand(
            companyId,
            DateOnly.FromDateTime(EntryDate.Date),
            SelectedSourceAccountId.Value,
            SelectedTargetAccountId.Value,
            ReferenceNo,
            Description,
            LockThroughEntryDate);

        return true;
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}
