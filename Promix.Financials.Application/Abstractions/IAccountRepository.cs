using Promix.Financials.Domain.Aggregates.Accounts;

namespace Promix.Financials.Application.Abstractions;

public interface IAccountRepository
{
    Task<bool> CodeExistsAsync(Guid companyId, string code, CancellationToken ct = default);
    Task<bool> SystemRoleExistsAsync(Guid companyId, string systemRole, CancellationToken ct = default);
    Task<Account?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task AddAsync(Account account, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);

    Task<Account?> GetByIdAsync(Guid id, Guid companyId, CancellationToken ct = default);
    Task<Account?> GetByCodeAsync(Guid companyId, string code, CancellationToken ct = default);
    Task<Account?> GetBySystemRoleAsync(Guid companyId, string systemRole, CancellationToken ct = default);
    Task<IReadOnlyList<Account>> GetAllAsync(Guid companyId, CancellationToken ct = default);
    Task<IReadOnlyList<Account>> GetChildrenAsync(Guid companyId, Guid parentId, CancellationToken ct = default);
    Task<IReadOnlyList<Account>> GetPostingAccountsAsync(Guid companyId, CancellationToken ct = default);
    Task<bool> HasChildrenAsync(Guid accountId, Guid companyId, CancellationToken ct = default);
    Task<bool> HasMovementsAsync(Guid accountId, Guid companyId, CancellationToken ct = default);
    void Remove(Account account);
}
