using Promix.Financials.Application.Abstractions;
using Promix.Financials.Application.Features.Parties.Commands;
using Promix.Financials.Domain.Exceptions;

namespace Promix.Financials.Application.Features.Parties.Services;

public sealed class ActivatePartyService
{
    private readonly IPartyRepository _parties;
    private readonly IUserContext _userContext;

    public ActivatePartyService(IPartyRepository parties, IUserContext userContext)
    {
        _parties = parties;
        _userContext = userContext;
    }

    public async Task ActivateAsync(ActivatePartyCommand command, CancellationToken ct = default)
    {
        if (!_userContext.IsAuthenticated)
            throw new BusinessRuleException("User is not authenticated.");

        var party = await _parties.GetByIdAsync(command.CompanyId, command.PartyId, ct)
            ?? throw new BusinessRuleException("Party was not found.");

        party.Activate();
        await _parties.SaveChangesAsync(ct);
    }
}
