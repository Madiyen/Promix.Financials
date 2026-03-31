using Promix.Financials.Domain.Enums;

namespace Promix.Financials.Application.Features.Parties.Commands;

public sealed record CreatePartyCommand(
    Guid CompanyId,
    string Code,
    string NameAr,
    string? NameEn,
    PartyTypeFlags TypeFlags,
    PartyLedgerMode LedgerMode,
    string? Phone,
    string? Mobile,
    string? Email,
    string? TaxNo,
    string? Address,
    string? Notes,
    Guid? ReceivableAccountId,
    Guid? PayableAccountId
)
{
    public CreatePartyCommand(
        Guid CompanyId,
        string Code,
        string NameAr,
        string? NameEn,
        PartyTypeFlags TypeFlags,
        string? Phone,
        string? Mobile,
        string? Email,
        string? TaxNo,
        string? Address,
        string? Notes,
        Guid? ReceivableAccountId,
        Guid? PayableAccountId)
        : this(
            CompanyId,
            Code,
            NameAr,
            NameEn,
            TypeFlags,
            ReceivableAccountId.HasValue || PayableAccountId.HasValue
                ? PartyLedgerMode.LegacyLinkedAccounts
                : PartyLedgerMode.Subledger,
            Phone,
            Mobile,
            Email,
            TaxNo,
            Address,
            Notes,
            ReceivableAccountId,
            PayableAccountId)
    {
    }
}

public sealed record EditPartyCommand(
    Guid CompanyId,
    Guid PartyId,
    string Code,
    string NameAr,
    string? NameEn,
    PartyTypeFlags TypeFlags,
    PartyLedgerMode LedgerMode,
    string? Phone,
    string? Mobile,
    string? Email,
    string? TaxNo,
    string? Address,
    string? Notes,
    bool IsActive,
    Guid? ReceivableAccountId,
    Guid? PayableAccountId
)
{
    public EditPartyCommand(
        Guid CompanyId,
        Guid PartyId,
        string Code,
        string NameAr,
        string? NameEn,
        PartyTypeFlags TypeFlags,
        string? Phone,
        string? Mobile,
        string? Email,
        string? TaxNo,
        string? Address,
        string? Notes,
        bool IsActive,
        Guid? ReceivableAccountId,
        Guid? PayableAccountId)
        : this(
            CompanyId,
            PartyId,
            Code,
            NameAr,
            NameEn,
            TypeFlags,
            ReceivableAccountId.HasValue || PayableAccountId.HasValue
                ? PartyLedgerMode.LegacyLinkedAccounts
                : PartyLedgerMode.Subledger,
            Phone,
            Mobile,
            Email,
            TaxNo,
            Address,
            Notes,
            IsActive,
            ReceivableAccountId,
            PayableAccountId)
    {
    }
}

public sealed record DeactivatePartyCommand(
    Guid CompanyId,
    Guid PartyId
);

public sealed record ActivatePartyCommand(
    Guid CompanyId,
    Guid PartyId
);
