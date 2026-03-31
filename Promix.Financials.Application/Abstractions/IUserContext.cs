using System;

namespace Promix.Financials.Application.Abstractions;

public interface IUserContext
{
    Guid UserId { get; }
    Guid? CompanyId { get; }     // ✅ nullable
    string Username { get; }
    IReadOnlyList<string> RoleNames { get; }
    bool IsAuthenticated { get; }
    bool IsAdmin => IsInRole("Admin");

    // ✅ Guard
    bool HasCompanySelected => CompanyId is not null;
    bool IsInRole(string roleName);

    // ✅ مهم: تحديث الشركة المختارة في الذاكرة بدون اعتماد على Infrastructure
    void SetCompany(Guid? companyId);
}
