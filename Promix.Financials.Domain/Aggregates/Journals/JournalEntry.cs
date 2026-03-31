using Promix.Financials.Domain.Common;
using Promix.Financials.Domain.Enums;
using Promix.Financials.Domain.Exceptions;

namespace Promix.Financials.Domain.Aggregates.Journals;

public sealed class JournalEntry : AggregateRoot<Guid>
{
    private readonly List<JournalLine> _lines = new();

    private JournalEntry() { }

    public JournalEntry(
        Guid companyId,
        string entryNumber,
        DateOnly entryDate,
        JournalEntryType type,
        string currencyCode,
        decimal exchangeRate,
        decimal currencyAmount,
        Guid createdByUserId,
        DateTimeOffset createdAtUtc,
        string? referenceNo,
        string? description,
        TransferSettlementMode? transferSettlementMode = null)
    {
        if (companyId == Guid.Empty)
            throw new BusinessRuleException("CompanyId is required.");

        if (createdByUserId == Guid.Empty)
            throw new BusinessRuleException("CreatedByUserId is required.");

        if (string.IsNullOrWhiteSpace(entryNumber))
            throw new BusinessRuleException("Entry number is required.");

        if (string.IsNullOrWhiteSpace(currencyCode))
            throw new BusinessRuleException("Currency code is required.");

        if (exchangeRate <= 0)
            throw new BusinessRuleException("Exchange rate must be greater than zero.");

        if (currencyAmount <= 0)
            throw new BusinessRuleException("Currency amount must be greater than zero.");

        Id = Guid.NewGuid();
        CompanyId = companyId;
        EntryNumber = entryNumber.Trim().ToUpperInvariant();
        EntryDate = entryDate;
        Type = type;
        CurrencyCode = currencyCode.Trim().ToUpperInvariant();
        ExchangeRate = decimal.Round(exchangeRate, 8, MidpointRounding.AwayFromZero);
        CurrencyAmount = decimal.Round(currencyAmount, 4, MidpointRounding.AwayFromZero);
        Status = JournalEntryStatus.Draft;
        CreatedByUserId = createdByUserId;
        CreatedAtUtc = createdAtUtc;
        ReferenceNo = Normalize(referenceNo, 50);
        Description = Normalize(description, 500);
        TransferSettlementMode = type == JournalEntryType.TransferVoucher
            ? transferSettlementMode ?? Enums.TransferSettlementMode.None
            : null;
    }

    public Guid CompanyId { get; private set; }
    public string EntryNumber { get; private set; } = default!;
    public DateOnly EntryDate { get; private set; }
    public JournalEntryType Type { get; private set; }
    public string CurrencyCode { get; private set; } = default!;
    public decimal ExchangeRate { get; private set; }
    public decimal CurrencyAmount { get; private set; }
    public JournalEntryStatus Status { get; private set; }
    public string? ReferenceNo { get; private set; }
    public string? Description { get; private set; }
    public TransferSettlementMode? TransferSettlementMode { get; private set; }
    public Guid CreatedByUserId { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; private set; }
    public Guid? PostedByUserId { get; private set; }
    public DateTimeOffset? PostedAtUtc { get; private set; }
    public Guid? ModifiedByUserId { get; private set; }
    public DateTimeOffset? ModifiedAtUtc { get; private set; }
    public bool IsDeleted { get; private set; }
    public Guid? DeletedByUserId { get; private set; }
    public DateTimeOffset? DeletedAtUtc { get; private set; }

    public IReadOnlyCollection<JournalLine> Lines => _lines.AsReadOnly();

    public decimal TotalDebit => _lines.Sum(x => x.Debit);
    public decimal TotalCredit => _lines.Sum(x => x.Credit);
    public bool IsBalanced => TotalDebit == TotalCredit && TotalDebit > 0;

    public void AddLine(Guid accountId, string? description, decimal debit, decimal credit)
        => AddLine(accountId, partyId: null, partyName: null, description, debit, credit);

    public void AddLine(Guid accountId, string? partyName, string? description, decimal debit, decimal credit)
        => AddLine(accountId, partyId: null, partyName, description, debit, credit);

    public void AddLine(Guid accountId, Guid? partyId, string? partyName, string? description, decimal debit, decimal credit)
    {
        EnsureDraft();

        var normalizedDebit = decimal.Round(debit, 2, MidpointRounding.AwayFromZero);
        var normalizedCredit = decimal.Round(credit, 2, MidpointRounding.AwayFromZero);

        if (accountId == Guid.Empty)
            throw new BusinessRuleException("Account is required.");

        if (normalizedDebit < 0 || normalizedCredit < 0)
            throw new BusinessRuleException("Debit and credit must be positive values.");

        var hasDebit = normalizedDebit > 0;
        var hasCredit = normalizedCredit > 0;

        if (hasDebit == hasCredit)
            throw new BusinessRuleException("Each line must contain either debit or credit.");

        _lines.Add(new JournalLine(
            journalEntryId: Id,
            lineNumber: _lines.Count + 1,
            accountId: accountId,
            partyId: partyId,
            partyName: partyName,
            description: description,
            debit: normalizedDebit,
            credit: normalizedCredit));
    }

    public void Update(
        DateOnly entryDate,
        string currencyCode,
        decimal exchangeRate,
        decimal currencyAmount,
        string? referenceNo,
        string? description,
        IReadOnlyList<JournalEntryEditableLine> lines,
        Guid modifiedByUserId,
        DateTimeOffset modifiedAtUtc,
        TransferSettlementMode? transferSettlementMode = null)
    {
        EnsureNotDeleted();

        if (modifiedByUserId == Guid.Empty)
            throw new BusinessRuleException("ModifiedByUserId is required.");

        if (string.IsNullOrWhiteSpace(currencyCode))
            throw new BusinessRuleException("Currency code is required.");

        if (exchangeRate <= 0)
            throw new BusinessRuleException("Exchange rate must be greater than zero.");

        if (currencyAmount <= 0)
            throw new BusinessRuleException("Currency amount must be greater than zero.");

        if (lines is null || lines.Count < 2)
            throw new BusinessRuleException("The journal entry must contain at least two lines.");

        _lines.Clear();
        foreach (var line in lines)
            AddEditableLine(line);

        if (!IsBalanced)
            throw new BusinessRuleException("The journal entry is not balanced.");

        EntryDate = entryDate;
        CurrencyCode = currencyCode.Trim().ToUpperInvariant();
        ExchangeRate = decimal.Round(exchangeRate, 8, MidpointRounding.AwayFromZero);
        CurrencyAmount = decimal.Round(currencyAmount, 4, MidpointRounding.AwayFromZero);
        ReferenceNo = Normalize(referenceNo, 50);
        Description = Normalize(description, 500);
        TransferSettlementMode = Type == JournalEntryType.TransferVoucher
            ? transferSettlementMode ?? Enums.TransferSettlementMode.None
            : null;
        ModifiedByUserId = modifiedByUserId;
        ModifiedAtUtc = modifiedAtUtc;
    }

    public void Delete(Guid deletedByUserId, DateTimeOffset deletedAtUtc)
    {
        EnsureNotDeleted();

        if (deletedByUserId == Guid.Empty)
            throw new BusinessRuleException("DeletedByUserId is required.");

        IsDeleted = true;
        DeletedByUserId = deletedByUserId;
        DeletedAtUtc = deletedAtUtc;
    }

    public void Post(Guid postedByUserId, DateTimeOffset postedAtUtc)
    {
        EnsureDraft();

        if (postedByUserId == Guid.Empty)
            throw new BusinessRuleException("PostedByUserId is required.");

        if (_lines.Count < 2)
            throw new BusinessRuleException("The journal entry must contain at least two lines.");

        if (!IsBalanced)
            throw new BusinessRuleException("The journal entry is not balanced.");

        Status = JournalEntryStatus.Posted;
        PostedByUserId = postedByUserId;
        PostedAtUtc = postedAtUtc;
    }

    private void EnsureDraft()
    {
        EnsureNotDeleted();

        if (Status == JournalEntryStatus.Posted)
            throw new BusinessRuleException("Posted journal entries cannot be modified.");
    }

    private void EnsureNotDeleted()
    {
        if (IsDeleted)
            throw new BusinessRuleException("Deleted journal entries cannot be modified.");
    }

    private void AddEditableLine(JournalEntryEditableLine line)
    {
        var normalizedDebit = decimal.Round(line.Debit, 2, MidpointRounding.AwayFromZero);
        var normalizedCredit = decimal.Round(line.Credit, 2, MidpointRounding.AwayFromZero);

        if (line.AccountId == Guid.Empty)
            throw new BusinessRuleException("Account is required.");

        if (normalizedDebit < 0 || normalizedCredit < 0)
            throw new BusinessRuleException("Debit and credit must be positive values.");

        var hasDebit = normalizedDebit > 0;
        var hasCredit = normalizedCredit > 0;

        if (hasDebit == hasCredit)
            throw new BusinessRuleException("Each line must contain either debit or credit.");

        _lines.Add(new JournalLine(
            journalEntryId: Id,
            lineNumber: _lines.Count + 1,
            accountId: line.AccountId,
            partyId: line.PartyId,
            partyName: line.PartyName,
            description: line.Description,
            debit: normalizedDebit,
            credit: normalizedCredit));
    }

    private static string? Normalize(string? value, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        var trimmed = value.Trim();
        return trimmed.Length <= maxLength ? trimmed : trimmed[..maxLength];
    }
}

public sealed record JournalEntryEditableLine(
    Guid AccountId,
    Guid? PartyId,
    string? PartyName,
    string? Description,
    decimal Debit,
    decimal Credit
);
