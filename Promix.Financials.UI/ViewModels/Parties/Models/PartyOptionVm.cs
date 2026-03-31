using System;
using Promix.Financials.Domain.Enums;

namespace Promix.Financials.UI.ViewModels.Parties.Models;

public sealed class PartyOptionVm
{
    public PartyOptionVm(
        Guid id,
        string code,
        string nameAr,
        PartyTypeFlags typeFlags,
        PartyLedgerMode ledgerMode,
        Guid? receivableAccountId,
        Guid? payableAccountId,
        bool isActive)
    {
        Id = id;
        Code = code;
        NameAr = nameAr;
        TypeFlags = typeFlags;
        LedgerMode = ledgerMode;
        ReceivableAccountId = receivableAccountId;
        PayableAccountId = payableAccountId;
        IsActive = isActive;
    }

    public PartyOptionVm(
        Guid id,
        string code,
        string nameAr,
        PartyTypeFlags typeFlags,
        Guid? receivableAccountId,
        Guid? payableAccountId,
        bool isActive)
        : this(
            id,
            code,
            nameAr,
            typeFlags,
            receivableAccountId.HasValue || payableAccountId.HasValue
                ? PartyLedgerMode.LegacyLinkedAccounts
                : PartyLedgerMode.Subledger,
            receivableAccountId,
            payableAccountId,
            isActive)
    {
    }

    public Guid Id { get; }
    public string Code { get; }
    public string NameAr { get; }
    public PartyTypeFlags TypeFlags { get; }
    public PartyLedgerMode LedgerMode { get; }
    public Guid? ReceivableAccountId { get; }
    public Guid? PayableAccountId { get; }
    public bool IsActive { get; }
    public string DisplayText => $"{Code} - {NameAr}";

    public Guid? ResolveVoucherAccountId(JournalEntryType type, Guid? receivableControlAccountId, Guid? payableControlAccountId)
        => type switch
        {
            JournalEntryType.ReceiptVoucher => ResolveReceivableAccountId(receivableControlAccountId),
            JournalEntryType.PaymentVoucher => ResolvePayableAccountId(payableControlAccountId),
            _ => null
        };

    public bool SupportsSide(PartyLedgerSide side)
        => side switch
        {
            PartyLedgerSide.Customer => TypeFlags.HasFlag(PartyTypeFlags.Customer),
            PartyLedgerSide.Vendor => TypeFlags.HasFlag(PartyTypeFlags.Vendor),
            _ => false
        };

    public PartyLedgerSide? GetSingleAvailableSide()
    {
        var customer = TypeFlags.HasFlag(PartyTypeFlags.Customer);
        var vendor = TypeFlags.HasFlag(PartyTypeFlags.Vendor);

        return (customer, vendor) switch
        {
            (true, false) => PartyLedgerSide.Customer,
            (false, true) => PartyLedgerSide.Vendor,
            _ => null
        };
    }

    public Guid? ResolveTransferAccountId(PartyLedgerSide side, Guid? receivableControlAccountId, Guid? payableControlAccountId)
        => side switch
        {
            PartyLedgerSide.Customer => ResolveReceivableAccountId(receivableControlAccountId),
            PartyLedgerSide.Vendor => ResolvePayableAccountId(payableControlAccountId),
            _ => null
        };

    public PartyLedgerSide? ResolveSideForAccount(Guid? accountId, Guid? receivableControlAccountId, Guid? payableControlAccountId)
    {
        if (accountId is not Guid resolvedAccountId)
            return null;

        if (resolvedAccountId == ResolveReceivableAccountId(receivableControlAccountId))
            return PartyLedgerSide.Customer;

        if (resolvedAccountId == ResolvePayableAccountId(payableControlAccountId))
            return PartyLedgerSide.Vendor;

        return null;
    }

    public Guid? ResolveJournalAccountId(bool prefersDebit, Guid? receivableControlAccountId, Guid? payableControlAccountId)
        => prefersDebit
            ? ResolveReceivableAccountId(receivableControlAccountId) ?? ResolvePayableAccountId(payableControlAccountId)
            : ResolvePayableAccountId(payableControlAccountId) ?? ResolveReceivableAccountId(receivableControlAccountId);

    public bool SupportsVoucherType(JournalEntryType type)
        => type switch
        {
            JournalEntryType.ReceiptVoucher => TypeFlags.HasFlag(PartyTypeFlags.Customer),
            JournalEntryType.PaymentVoucher => TypeFlags.HasFlag(PartyTypeFlags.Vendor),
            _ => true
        };

    public bool MatchesAccount(Guid? accountId, Guid? receivableControlAccountId, Guid? payableControlAccountId)
        => accountId.HasValue && (accountId == ResolveReceivableAccountId(receivableControlAccountId) || accountId == ResolvePayableAccountId(payableControlAccountId));

    private Guid? ResolveReceivableAccountId(Guid? receivableControlAccountId)
        => LedgerMode == PartyLedgerMode.Subledger
            ? receivableControlAccountId
            : ReceivableAccountId;

    private Guid? ResolvePayableAccountId(Guid? payableControlAccountId)
        => LedgerMode == PartyLedgerMode.Subledger
            ? payableControlAccountId
            : PayableAccountId;
}
