using Promix.Financials.Application.Abstractions;

namespace Promix.Financials.Infrastructure.Security;

public sealed class SessionUserContext : IUserContextBootstrappable   // ✅ كان IUserContext
{
    private string[] _roleNames = Array.Empty<string>();

    public Guid UserId { get; private set; }
    public Guid? CompanyId { get; private set; }
    public string Username { get; private set; } = string.Empty;
    public IReadOnlyList<string> RoleNames => _roleNames;

    public bool IsAuthenticated => UserId != Guid.Empty;

    public void SetSession(AppSession session, string username)
    {
        UserId = session.UserId;
        CompanyId = session.CompanyId;
        Username = username ?? string.Empty;
        _roleNames = session.RoleNames
            .Where(role => !string.IsNullOrWhiteSpace(role))
            .Select(role => role.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    public bool IsInRole(string roleName)
        => !string.IsNullOrWhiteSpace(roleName)
            && _roleNames.Any(role => string.Equals(role, roleName, StringComparison.OrdinalIgnoreCase));

    public void SetCompany(Guid? companyId)
    {
        CompanyId = companyId;
    }

    public void Clear()
    {
        UserId = Guid.Empty;
        CompanyId = null;
        Username = string.Empty;
        _roleNames = Array.Empty<string>();
    }
}
