using Promix.Financials.Application.Abstractions;
using Promix.Financials.Domain.Exceptions;

namespace Promix.Financials.Application.Features.Companies;

public sealed class ResetApplicationDataService
{
    private readonly ICompanyAdminRepository _companyAdminRepository;
    private readonly ISessionStore _sessionStore;
    private readonly IUserContext _userContext;

    public ResetApplicationDataService(
        ICompanyAdminRepository companyAdminRepository,
        ISessionStore sessionStore,
        IUserContext userContext)
    {
        _companyAdminRepository = companyAdminRepository;
        _sessionStore = sessionStore;
        _userContext = userContext;
    }

    public async Task ResetAsync(CancellationToken ct = default)
    {
        if (!_userContext.IsAuthenticated)
            throw new BusinessRuleException("يجب تسجيل الدخول قبل تنفيذ إعادة التهيئة.");

        if (!_userContext.IsAdmin)
            throw new BusinessRuleException("إعادة التهيئة الشاملة متاحة للمستخدم الإداري فقط.");

        await _companyAdminRepository.ResetApplicationDataAsync(ct);

        var session = await _sessionStore.LoadAsync(ct);
        if (session is not null)
        {
            session.CompanyId = null;
            var activeUserId = await _sessionStore.LoadActiveUserIdAsync(ct);
            var persist = activeUserId.HasValue && activeUserId.Value == session.UserId;
            await _sessionStore.SaveAsync(session, persist, ct);
        }

        _userContext.SetCompany(null);
    }
}
