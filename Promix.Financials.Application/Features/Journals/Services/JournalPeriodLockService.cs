using Promix.Financials.Application.Abstractions;
using Promix.Financials.Domain.Exceptions;

namespace Promix.Financials.Application.Features.Journals.Services;

public sealed class JournalPeriodLockService
{
    private readonly ICompanyJournalLockRepository _companies;
    private readonly IUserContext _userContext;
    private readonly IDateTimeProvider _clock;

    public JournalPeriodLockService(
        ICompanyJournalLockRepository companies,
        IUserContext userContext,
        IDateTimeProvider clock)
    {
        _companies = companies;
        _userContext = userContext;
        _clock = clock;
    }

    public async Task EnsureEntryDateIsOpenAsync(Guid companyId, DateOnly entryDate, CancellationToken ct = default)
    {
        if (companyId == Guid.Empty)
            throw new BusinessRuleException("CompanyId is required.");

        var company = await _companies.GetByIdAsync(companyId, ct)
            ?? throw new BusinessRuleException("Company was not found.");

        company.EnsureJournalDateIsOpen(entryDate);
    }

    public async Task LockThroughAsync(Guid companyId, DateOnly lockThroughDate, CancellationToken ct = default)
    {
        if (!_userContext.IsAuthenticated || _userContext.UserId == Guid.Empty)
            throw new BusinessRuleException("User is not authenticated.");

        if (companyId == Guid.Empty)
            throw new BusinessRuleException("CompanyId is required.");

        var company = await _companies.GetByIdAsync(companyId, ct)
            ?? throw new BusinessRuleException("Company was not found.");

        company.LockJournalThrough(lockThroughDate, _userContext.UserId, _clock.UtcNow);
        await _companies.SaveChangesAsync(ct);
    }
}
