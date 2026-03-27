using Promix.Financials.Application.Abstractions;
using Promix.Financials.Application.Features.Journals.Commands;
using Promix.Financials.Domain.Enums;
using Promix.Financials.Domain.Exceptions;

namespace Promix.Financials.Application.Features.Journals.Services;

public sealed class CreateDailyCashClosingService
{
    private readonly IJournalEntryRepository _entries;
    private readonly IAccountRepository _accounts;
    private readonly CreateJournalEntryService _createService;
    private readonly JournalPeriodLockService _periodLockService;

    public CreateDailyCashClosingService(
        IJournalEntryRepository entries,
        IAccountRepository accounts,
        CreateJournalEntryService createService,
        JournalPeriodLockService periodLockService)
    {
        _entries = entries;
        _accounts = accounts;
        _createService = createService;
        _periodLockService = periodLockService;
    }

    public async Task<Guid> CreateAsync(CreateDailyCashClosingCommand command, CancellationToken ct = default)
    {
        if (command.CompanyId == Guid.Empty)
            throw new BusinessRuleException("CompanyId is required.");

        if (command.SourceAccountId == Guid.Empty || command.TargetAccountId == Guid.Empty)
            throw new BusinessRuleException("يجب تحديد حساب المصدر والحساب المقابل.");

        if (command.SourceAccountId == command.TargetAccountId)
            throw new BusinessRuleException("يجب أن يختلف حساب المصدر عن الحساب المقابل.");

        var sourceAccount = await _accounts.GetByIdAsync(command.SourceAccountId, command.CompanyId);
        var targetAccount = await _accounts.GetByIdAsync(command.TargetAccountId, command.CompanyId);

        if (sourceAccount is null || targetAccount is null)
            throw new BusinessRuleException("تعذر العثور على أحد الحسابين المحددين.");

        if (!sourceAccount.IsPosting || !targetAccount.IsPosting)
            throw new BusinessRuleException("يجب أن تكون الحسابات المختارة حسابات نهائية.");

        if (!sourceAccount.IsActive || !targetAccount.IsActive)
            throw new BusinessRuleException("يجب أن تكون الحسابات المختارة فعالة.");

        if (await _entries.HasDailyCashClosingAsync(command.CompanyId, command.SourceAccountId, command.EntryDate, ct))
            throw new BusinessRuleException("تم إنشاء إقفال صندوق يومي لهذا الحساب في هذا التاريخ مسبقاً.");

        var movement = await _entries.GetDailyMovementSummaryAsync(command.CompanyId, command.SourceAccountId, command.EntryDate, ct);
        if (movement.NetMovement == 0)
            throw new BusinessRuleException("لا توجد حركة مرحّلة صافية لهذا الحساب في التاريخ المحدد.");

        var amount = decimal.Abs(movement.NetMovement);
        var description = string.IsNullOrWhiteSpace(command.Description)
            ? $"إقفال صندوق يومي من {sourceAccount.NameAr} إلى {targetAccount.NameAr} بتاريخ {command.EntryDate:yyyy-MM-dd}"
            : command.Description.Trim();

        CreateJournalEntryLineCommand sourceLine;
        CreateJournalEntryLineCommand targetLine;

        if (movement.NetMovement > 0)
        {
            targetLine = new CreateJournalEntryLineCommand(
                targetAccount.Id,
                amount,
                0m,
                $"تحويل رصيد الإقفال إلى {targetAccount.NameAr}");

            sourceLine = new CreateJournalEntryLineCommand(
                sourceAccount.Id,
                0m,
                amount,
                $"تصفير رصيد {sourceAccount.NameAr} لليوم");
        }
        else
        {
            sourceLine = new CreateJournalEntryLineCommand(
                sourceAccount.Id,
                amount,
                0m,
                $"إعادة {sourceAccount.NameAr} إلى رصيد صفري");

            targetLine = new CreateJournalEntryLineCommand(
                targetAccount.Id,
                0m,
                amount,
                $"مقابل إقفال الرصيد الدائن في {sourceAccount.NameAr}");
        }

        var createCommand = new CreateJournalEntryCommand(
            command.CompanyId,
            command.EntryDate,
            JournalEntryType.DailyCashClosing,
            command.ReferenceNo,
            description,
            CurrencyCode: null,
            ExchangeRate: null,
            CurrencyAmount: null,
            PostNow: true,
            Lines: new[] { targetLine, sourceLine });

        var entryId = await _createService.CreateAsync(createCommand, ct);

        if (command.LockThroughEntryDate)
            await _periodLockService.LockThroughAsync(command.CompanyId, command.EntryDate, ct);

        return entryId;
    }
}
