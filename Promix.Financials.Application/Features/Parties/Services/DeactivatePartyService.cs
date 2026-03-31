using Promix.Financials.Application.Abstractions;
using Promix.Financials.Application.Features.Parties.Commands;
using Promix.Financials.Application.Features.Parties.Queries;
using Promix.Financials.Domain.Exceptions;

namespace Promix.Financials.Application.Features.Parties.Services;

public sealed class DeactivatePartyService
{
    private readonly IPartyRepository _parties;
    private readonly IPartyQuery _partyQuery;
    private readonly ICompanyJournalLockRepository _companies;
    private readonly IUserContext _userContext;
    private readonly IDateTimeProvider _clock;

    public DeactivatePartyService(
        IPartyRepository parties,
        IPartyQuery partyQuery,
        ICompanyJournalLockRepository companies,
        IUserContext userContext,
        IDateTimeProvider clock)
    {
        _parties = parties;
        _partyQuery = partyQuery;
        _companies = companies;
        _userContext = userContext;
        _clock = clock;
    }

    public async Task DeactivateAsync(DeactivatePartyCommand command, CancellationToken ct = default)
    {
        if (!_userContext.IsAuthenticated)
            throw new BusinessRuleException("User is not authenticated.");

        var party = await _parties.GetByIdAsync(command.CompanyId, command.PartyId, ct)
            ?? throw new BusinessRuleException("Party was not found.");

        var company = await _companies.GetByIdAsync(command.CompanyId, ct)
            ?? throw new BusinessRuleException("Company was not found.");

        var statement = await _partyQuery.GetStatementAsync(
            command.CompanyId,
            command.PartyId,
            company.AccountingStartDate,
            DateOnly.FromDateTime(_clock.UtcNow.Date),
            ct);

        if (statement is not null)
        {
            var hasOpenItems = statement.OpenItems.Count > 0;
            var hasOutstandingBalance = statement.ClosingBalance != 0m;

            if (hasOpenItems || hasOutstandingBalance)
            {
                throw new BusinessRuleException(
                    "لا يمكن إيقاف التعامل مع هذا الطرف لأنه ما زال يحمل رصيداً أو بنوداً مفتوحة. قم بتسوية الذمة أولاً ثم أعد المحاولة.");
            }
        }

        party.Deactivate();
        await _parties.SaveChangesAsync(ct);
    }
}
