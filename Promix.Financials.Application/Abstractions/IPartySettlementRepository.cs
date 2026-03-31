using Promix.Financials.Domain.Aggregates.Parties;

namespace Promix.Financials.Application.Abstractions;

public sealed record PartySettlementLedgerLine(
    Guid LineId,
    Guid PartyId,
    Guid AccountId,
    DateOnly EntryDate,
    string EntryNumber,
    int LineNumber,
    decimal Debit,
    decimal Credit
);

public interface IPartySettlementRepository
{
    Task<IReadOnlyList<PartySettlement>> GetByPairAsync(Guid companyId, Guid partyId, Guid accountId, CancellationToken ct = default);
    Task<IReadOnlyList<PartySettlementLedgerLine>> GetPostedLedgerLinesAsync(Guid companyId, Guid partyId, Guid accountId, CancellationToken ct = default);
    Task AddRangeAsync(IReadOnlyList<PartySettlement> settlements, CancellationToken ct = default);
    void RemoveRange(IEnumerable<PartySettlement> settlements);
    Task SaveChangesAsync(CancellationToken ct = default);
}
