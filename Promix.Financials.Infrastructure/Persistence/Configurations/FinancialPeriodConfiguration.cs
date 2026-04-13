using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Promix.Financials.Domain.Accounting;
using Promix.Financials.Domain.Aggregates.Journals;

namespace Promix.Financials.Infrastructure.Persistence.Configurations;

public sealed class FinancialPeriodConfiguration : IEntityTypeConfiguration<FinancialPeriod>
{
    public void Configure(EntityTypeBuilder<FinancialPeriod> builder)
    {
        builder.ToTable("FinancialPeriods");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();
        builder.Property(x => x.RowVersion).IsRowVersion();

        builder.Property(x => x.CompanyId).IsRequired();
        builder.Property(x => x.FinancialYearId).IsRequired();
        builder.Property(x => x.Code).HasMaxLength(50).IsRequired();
        builder.Property(x => x.Name).HasMaxLength(200).IsRequired();
        builder.Property(x => x.StartDate).IsRequired();
        builder.Property(x => x.EndDate).IsRequired();
        builder.Property(x => x.Status).HasConversion<int>().IsRequired();
        builder.Property(x => x.IsAdjustmentPeriod).IsRequired();

        builder.HasIndex(x => new { x.CompanyId, x.FinancialYearId, x.Code }).IsUnique();
        builder.HasIndex(x => new { x.CompanyId, x.FinancialYearId, x.StartDate, x.EndDate }).IsUnique();

        builder.HasOne<FinancialYear>()
            .WithMany()
            .HasForeignKey(x => x.FinancialYearId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany<JournalEntry>()
            .WithOne()
            .HasForeignKey(x => x.FinancialPeriodId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
