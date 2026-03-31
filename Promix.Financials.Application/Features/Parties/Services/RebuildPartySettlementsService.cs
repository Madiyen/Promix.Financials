using Promix.Financials.Application.Abstractions;
using Promix.Financials.Domain.Aggregates.Journals;
using Promix.Financials.Domain.Aggregates.Parties;
using Promix.Financials.Domain.Exceptions;

namespace Promix.Financials.Application.Features.Parties.Services;

public sealed class RebuildPartySettlementsService
{
    private readonly IPartySettlementRepository _settlements;
    private readonly IUserContext _userContext;
    private readonly IDateTimeProvider _clock;

    public RebuildPartySettlementsService(
        IPartySettlementRepository settlements,
        IUserContext userContext,
        IDateTimeProvider clock)
    {
        _settlements = settlements;
        _userContext = userContext;
        _clock = clock;
    }

    public static IReadOnlyList<PartyAccountScope> CollectScopes(IEnumerable<JournalLine> lines)
        => lines
            .Where(x => x.PartyId.HasValue)
            .Select(x => new PartyAccountScope(x.PartyId!.Value, x.AccountId))
            .Distinct()
            .ToList();

    public async Task RebuildAsync(Guid companyId, IEnumerable<PartyAccountScope> scopes, CancellationToken ct = default)
    {
        if (!_userContext.IsAuthenticated || _userContext.UserId == Guid.Empty)
            throw new BusinessRuleException("User is not authenticated.");

        var distinctScopes = scopes
            .Where(x => x.PartyId != Guid.Empty && x.AccountId != Guid.Empty)
            .Distinct()
            .ToList();

        foreach (var scope in distinctScopes)
        {
            var existing = await _settlements.GetByPairAsync(companyId, scope.PartyId, scope.AccountId, ct);
            if (existing.Count > 0)
                _settlements.RemoveRange(existing);

            var lines = await _settlements.GetPostedLedgerLinesAsync(companyId, scope.PartyId, scope.AccountId, ct);
            if (lines.Count == 0)
                continue;

            var debitQueue = new Queue<PartySettlementRemaining>(lines.Where(x => x.Debit > 0).Select(x => new PartySettlementRemaining(x, x.Debit)));
            var creditQueue = new Queue<PartySettlementRemaining>(lines.Where(x => x.Credit > 0).Select(x => new PartySettlementRemaining(x, x.Credit)));
            var newSettlements = new List<PartySettlement>();

            while (debitQueue.Count > 0 && creditQueue.Count > 0)
            {
                var debit = debitQueue.Peek();
                var credit = creditQueue.Peek();
                var amount = Math.Min(debit.RemainingAmount, credit.RemainingAmount);

                if (amount <= 0)
                {
                    if (debit.RemainingAmount <= 0)
                        debitQueue.Dequeue();

                    if (credit.RemainingAmount <= 0)
                        creditQueue.Dequeue();

                    continue;
                }

                newSettlements.Add(new PartySettlement(
                    companyId,
                    scope.PartyId,
                    scope.AccountId,
                    debit.Line.LineId,
                    credit.Line.LineId,
                    amount,
                    debit.Line.EntryDate >= credit.Line.EntryDate ? debit.Line.EntryDate : credit.Line.EntryDate,
                    _userContext.UserId,
                    _clock.UtcNow));

                debit.RemainingAmount -= amount;
                credit.RemainingAmount -= amount;

                if (debit.RemainingAmount <= 0)
                    debitQueue.Dequeue();

                if (credit.RemainingAmount <= 0)
                    creditQueue.Dequeue();
            }

            if (newSettlements.Count > 0)
                await _settlements.AddRangeAsync(newSettlements, ct);
        }
    }

    public sealed record PartyAccountScope(Guid PartyId, Guid AccountId);

    private sealed class PartySettlementRemaining
    {
        public PartySettlementRemaining(PartySettlementLedgerLine line, decimal remainingAmount)
        {
            Line = line;
            RemainingAmount = remainingAmount;
        }

        public PartySettlementLedgerLine Line { get; }
        public decimal RemainingAmount { get; set; }
    }
}
