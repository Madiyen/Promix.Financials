namespace Promix.Financials.Domain.Enums;

public enum SourceDocumentType
{
    ManualJournal = 1,
    ReceiptVoucher = 2,
    PaymentVoucher = 3,
    TransferVoucher = 4,
    OpeningEntry = 5,
    Adjustment = 6,
    DailyCashClosing = 7,
    YearEndClosing = 8,
    SalesInvoice = 9,
    PurchaseInvoice = 10,
    InventoryAdjustment = 11,
    StockIssue = 12,
    StockReceipt = 13
}
