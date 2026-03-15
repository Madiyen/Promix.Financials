using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Promix.Financials.Infrastructure.Persistence;

public sealed class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<PromixDbContext>
{
    public PromixDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<PromixDbContext>();

        optionsBuilder.UseSqlServer(
            "Server=.\\MSSQLSERVER2025;Database=PromixFinancials;Trusted_Connection=True;TrustServerCertificate=True;");

        return new PromixDbContext(optionsBuilder.Options);
    }
}