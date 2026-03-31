using Promix.Financials.Domain.Common;
using Promix.Financials.Domain.Exceptions;

namespace Promix.Financials.Domain.Aggregates.Parties;

public sealed class PartySettlement : Entity<Guid>
{
    private PartySettlement() { }

    public PartySettlement(
        Guid companyId,
        Guid partyId,
        Guid accountId,
        Guid debitLineId,
        Guid creditLineId,
        decimal amount,
        DateOnly settledOn,
        Guid createdByUserId,
        DateTimeOffset createdAtUtc)
    {
        if (companyId == Guid.Empty)
            throw new BusinessRuleException("CompanyId is required.");

        if (partyId == Guid.Empty)
            throw new BusinessRuleException("PartyId is required.");

        if (accountId == Guid.Empty)
            throw new BusinessRuleException("AccountId is required.");

        if (debitLineId == Guid.Empty || creditLineId == Guid.Empty)
            throw new BusinessRuleException("Settlement lines are required.");

        if (debitLineId == creditLineId)
            throw new BusinessRuleException("Settlement lines must be different.");

        if (amount <= 0)
            throw new BusinessRuleException("Settlement amount must be greater than zero.");

        if (createdByUserId == Guid.Empty)
            throw new BusinessRuleException("CreatedByUserId is required.");

        Id = Guid.NewGuid();
        CompanyId = companyId;
        PartyId = partyId;
        AccountId = accountId;
        DebitLineId = debitLineId;
        CreditLineId = creditLineId;
        Amount = decimal.Round(amount, 2, MidpointRounding.AwayFromZero);
        SettledOn = settledOn;
        CreatedByUserId = createdByUserId;
        CreatedAtUtc = createdAtUtc;
    }

    public Guid CompanyId { get; private set; }
    public Guid PartyId { get; private set; }
    public Guid AccountId { get; private set; }
    public Guid DebitLineId { get; private set; }
    public Guid CreditLineId { get; private set; }
    public decimal Amount { get; private set; }
    public DateOnly SettledOn { get; private set; }
    public Guid CreatedByUserId { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; private set; }
}
