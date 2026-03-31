using Promix.Financials.Domain.Common;
using Promix.Financials.Domain.Enums;
using Promix.Financials.Domain.Exceptions;

namespace Promix.Financials.Domain.Aggregates.Parties;

public sealed class Party : AggregateRoot<Guid>
{
    private Party() { }

    public Party(
        Guid companyId,
        string code,
        string nameAr,
        string? nameEn,
        PartyTypeFlags typeFlags,
        PartyLedgerMode ledgerMode,
        Guid? receivableAccountId,
        Guid? payableAccountId,
        string? phone,
        string? mobile,
        string? email,
        string? taxNo,
        string? address,
        string? notes,
        bool isActive = true)
    {
        if (companyId == Guid.Empty)
            throw new BusinessRuleException("CompanyId is required.");

        if (string.IsNullOrWhiteSpace(code))
            throw new BusinessRuleException("Party code is required.");

        if (string.IsNullOrWhiteSpace(nameAr))
            throw new BusinessRuleException("Arabic party name is required.");

        EnsureAccountShape(typeFlags, ledgerMode, receivableAccountId, payableAccountId);

        Id = Guid.NewGuid();
        CompanyId = companyId;
        Code = NormalizeCode(code);
        NameAr = Normalize(nameAr, 150)!;
        NameEn = Normalize(nameEn, 150);
        TypeFlags = typeFlags;
        LedgerMode = ledgerMode;
        ReceivableAccountId = receivableAccountId;
        PayableAccountId = payableAccountId;
        Phone = Normalize(phone, 40);
        Mobile = Normalize(mobile, 40);
        Email = Normalize(email, 120);
        TaxNo = Normalize(taxNo, 50);
        Address = Normalize(address, 300);
        Notes = Normalize(notes, 500);
        IsActive = isActive;
    }

    public Party(
        Guid companyId,
        string code,
        string nameAr,
        string? nameEn,
        PartyTypeFlags typeFlags,
        Guid? receivableAccountId,
        Guid? payableAccountId,
        string? phone,
        string? mobile,
        string? email,
        string? taxNo,
        string? address,
        string? notes,
        bool isActive = true)
        : this(
            companyId,
            code,
            nameAr,
            nameEn,
            typeFlags,
            PartyLedgerMode.LegacyLinkedAccounts,
            receivableAccountId,
            payableAccountId,
            phone,
            mobile,
            email,
            taxNo,
            address,
            notes,
            isActive)
    {
    }

    public Guid CompanyId { get; private set; }
    public string Code { get; private set; } = default!;
    public string NameAr { get; private set; } = default!;
    public string? NameEn { get; private set; }
    public PartyTypeFlags TypeFlags { get; private set; }
    public PartyLedgerMode LedgerMode { get; private set; }
    public Guid? ReceivableAccountId { get; private set; }
    public Guid? PayableAccountId { get; private set; }
    public string? Phone { get; private set; }
    public string? Mobile { get; private set; }
    public string? Email { get; private set; }
    public string? TaxNo { get; private set; }
    public string? Address { get; private set; }
    public string? Notes { get; private set; }
    public bool IsActive { get; private set; }

    public bool IsCustomer => TypeFlags.HasFlag(PartyTypeFlags.Customer);
    public bool IsVendor => TypeFlags.HasFlag(PartyTypeFlags.Vendor);

    public void Update(
        string code,
        string nameAr,
        string? nameEn,
        PartyTypeFlags typeFlags,
        PartyLedgerMode ledgerMode,
        Guid? receivableAccountId,
        Guid? payableAccountId,
        string? phone,
        string? mobile,
        string? email,
        string? taxNo,
        string? address,
        string? notes,
        bool isActive)
    {
        if (string.IsNullOrWhiteSpace(code))
            throw new BusinessRuleException("Party code is required.");

        if (string.IsNullOrWhiteSpace(nameAr))
            throw new BusinessRuleException("Arabic party name is required.");

        EnsureAccountShape(typeFlags, ledgerMode, receivableAccountId, payableAccountId);

        Code = NormalizeCode(code);
        NameAr = Normalize(nameAr, 150)!;
        NameEn = Normalize(nameEn, 150);
        TypeFlags = typeFlags;
        LedgerMode = ledgerMode;
        ReceivableAccountId = receivableAccountId;
        PayableAccountId = payableAccountId;
        Phone = Normalize(phone, 40);
        Mobile = Normalize(mobile, 40);
        Email = Normalize(email, 120);
        TaxNo = Normalize(taxNo, 50);
        Address = Normalize(address, 300);
        Notes = Normalize(notes, 500);
        IsActive = isActive;
    }

    public void Deactivate() => IsActive = false;

    public void Activate() => IsActive = true;

    private static void EnsureAccountShape(
        PartyTypeFlags typeFlags,
        PartyLedgerMode ledgerMode,
        Guid? receivableAccountId,
        Guid? payableAccountId)
    {
        if (typeFlags == PartyTypeFlags.None)
            throw new BusinessRuleException("Party type is required.");

        if (!Enum.IsDefined(typeof(PartyLedgerMode), ledgerMode))
            throw new BusinessRuleException("Party ledger mode is required.");

        if (ledgerMode == PartyLedgerMode.Subledger)
        {
            if (receivableAccountId.HasValue || payableAccountId.HasValue)
                throw new BusinessRuleException("Subledger parties must not store legacy linked accounts.");

            return;
        }

        var expectsReceivable = typeFlags.HasFlag(PartyTypeFlags.Customer);
        var expectsPayable = typeFlags.HasFlag(PartyTypeFlags.Vendor);

        if (expectsReceivable && (!receivableAccountId.HasValue || receivableAccountId.Value == Guid.Empty))
            throw new BusinessRuleException("Receivable account is required for customer parties.");

        if (!expectsReceivable && receivableAccountId is not null)
            throw new BusinessRuleException("Receivable account is not valid for this party type.");

        if (expectsPayable && (!payableAccountId.HasValue || payableAccountId.Value == Guid.Empty))
            throw new BusinessRuleException("Payable account is required for vendor parties.");

        if (!expectsPayable && payableAccountId is not null)
            throw new BusinessRuleException("Payable account is not valid for this party type.");
    }

    private static string NormalizeCode(string value)
        => value.Trim().ToUpperInvariant();

    private static string? Normalize(string? value, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        var trimmed = value.Trim();
        return trimmed.Length <= maxLength ? trimmed : trimmed[..maxLength];
    }
}
