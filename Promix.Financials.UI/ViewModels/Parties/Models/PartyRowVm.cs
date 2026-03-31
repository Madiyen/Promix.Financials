using System;
using System.Linq;
using Microsoft.UI;
using Microsoft.UI.Xaml.Media;
using Promix.Financials.Domain.Enums;

namespace Promix.Financials.UI.ViewModels.Parties.Models;

public sealed class PartyRowVm
{
    public PartyRowVm(
        Guid id,
        string code,
        string nameAr,
        string? nameEn,
        PartyTypeFlags typeFlags,
        string? phone,
        string? mobile,
        string? email,
        string? taxNo,
        string? address,
        string? notes,
        bool isActive,
        PartyLedgerMode ledgerMode,
        Guid? receivableAccountId,
        Guid? payableAccountId,
        decimal receivableOpenBalance,
        decimal payableOpenBalance)
    {
        Id = id;
        Code = code;
        NameAr = nameAr;
        NameEn = nameEn;
        TypeFlags = typeFlags;
        Phone = phone;
        Mobile = mobile;
        Email = email;
        TaxNo = taxNo;
        Address = address;
        Notes = notes;
        IsActive = isActive;
        LedgerMode = ledgerMode;
        ReceivableAccountId = receivableAccountId;
        PayableAccountId = payableAccountId;
        ReceivableOpenBalance = receivableOpenBalance;
        PayableOpenBalance = payableOpenBalance;
    }

    public Guid Id { get; }
    public string Code { get; }
    public string NameAr { get; }
    public string? NameEn { get; }
    public PartyTypeFlags TypeFlags { get; }
    public string? Phone { get; }
    public string? Mobile { get; }
    public string? Email { get; }
    public string? TaxNo { get; }
    public string? Address { get; }
    public string? Notes { get; }
    public bool IsActive { get; }
    public PartyLedgerMode LedgerMode { get; }
    public Guid? ReceivableAccountId { get; }
    public Guid? PayableAccountId { get; }
    public decimal ReceivableOpenBalance { get; }
    public decimal PayableOpenBalance { get; }

    public string TypeText => TypeFlags switch
    {
        PartyTypeFlags.Customer => "عميل",
        PartyTypeFlags.Vendor => "مورد",
        PartyTypeFlags.Both => "عميل ومورد",
        _ => "غير محدد"
    };

    public string StatusText => IsActive ? "نشط" : "موقوف التعامل";
    public string LedgerModeText => LedgerMode == PartyLedgerMode.Subledger ? "دفتر فرعي" : "حسابات مرتبطة";
    public string ReceivableOpenText => ReceivableOpenBalance.ToString("N2");
    public string PayableOpenText => PayableOpenBalance.ToString("N2");
    public string ContactText => string.Join(" • ", new[] { Phone, Mobile, Email }.Where(x => !string.IsNullOrWhiteSpace(x)));
    public string ContactSummaryText => string.IsNullOrWhiteSpace(ContactText) ? "لا توجد بيانات تواصل مسجلة." : ContactText;
    public Brush TypeSurfaceBrush => TypeFlags switch
    {
        PartyTypeFlags.Customer => CreateBrush("#E0F2FE"),
        PartyTypeFlags.Vendor => CreateBrush("#FEF3C7"),
        PartyTypeFlags.Both => CreateBrush("#E6FFFA"),
        _ => CreateBrush("#E2E8F0")
    };
    public Brush TypeForegroundBrush => TypeFlags switch
    {
        PartyTypeFlags.Customer => CreateBrush("#075985"),
        PartyTypeFlags.Vendor => CreateBrush("#92400E"),
        PartyTypeFlags.Both => CreateBrush("#0F766E"),
        _ => CreateBrush("#334155")
    };
    public Brush StatusSurfaceBrush => IsActive ? CreateBrush("#DCFCE7") : CreateBrush("#E2E8F0");
    public Brush StatusForegroundBrush => IsActive ? CreateBrush("#166534") : CreateBrush("#475569");

    private static SolidColorBrush CreateBrush(string hex)
    {
        var value = hex.TrimStart('#');
        var color = value.Length switch
        {
            6 => ColorHelper.FromArgb(255,
                Convert.ToByte(value[..2], 16),
                Convert.ToByte(value.Substring(2, 2), 16),
                Convert.ToByte(value.Substring(4, 2), 16)),
            8 => ColorHelper.FromArgb(
                Convert.ToByte(value[..2], 16),
                Convert.ToByte(value.Substring(2, 2), 16),
                Convert.ToByte(value.Substring(4, 2), 16),
                Convert.ToByte(value.Substring(6, 2), 16)),
            _ => Colors.Transparent
        };

        return new SolidColorBrush(color);
    }
}
