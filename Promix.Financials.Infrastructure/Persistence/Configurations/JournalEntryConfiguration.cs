using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Promix.Financials.Domain.Aggregates.Journals;

namespace Promix.Financials.Infrastructure.Persistence.Configurations;

public sealed class JournalEntryConfiguration : IEntityTypeConfiguration<JournalEntry>
{
    public void Configure(EntityTypeBuilder<JournalEntry> builder)
    {
        builder.ToTable("JournalEntries");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();
        builder.Property(x => x.RowVersion).IsRowVersion();

        builder.Property(x => x.CompanyId).IsRequired();
        builder.Property(x => x.EntryNumber).HasMaxLength(30).IsRequired();
        builder.Property(x => x.EntryDate).IsRequired();
        builder.Property(x => x.FinancialYearId).IsRequired();
        builder.Property(x => x.FinancialPeriodId).IsRequired();
        builder.Property(x => x.Type).HasConversion<int>().IsRequired();
        builder.Property(x => x.CurrencyCode).HasMaxLength(10).IsRequired();
        builder.Property(x => x.ExchangeRate).HasPrecision(18, 8).IsRequired();
        builder.Property(x => x.CurrencyAmount).HasPrecision(18, 4).IsRequired();
        builder.Property(x => x.Status).HasConversion<int>().IsRequired();
        builder.Property(x => x.SourceDocumentType).HasConversion<int>().IsRequired();
        builder.Property(x => x.SourceDocumentId);
        builder.Property(x => x.SourceDocumentNumber).HasMaxLength(50);
        builder.Property(x => x.SourceLineId);
        builder.Property(x => x.ReferenceNo).HasMaxLength(50);
        builder.Property(x => x.Description).HasMaxLength(500);
        builder.Property(x => x.TransferSettlementMode).HasConversion<int?>();
        builder.Property(x => x.CreatedByUserId).IsRequired();
        builder.Property(x => x.CreatedAtUtc).IsRequired();
        builder.Property(x => x.ModifiedByUserId);
        builder.Property(x => x.ModifiedAtUtc);
        builder.Property(x => x.IsDeleted).HasDefaultValue(false);
        builder.Property(x => x.DeletedByUserId);
        builder.Property(x => x.DeletedAtUtc);

        builder.HasIndex(x => new { x.CompanyId, x.EntryNumber }).IsUnique();
        builder.HasIndex(x => new { x.CompanyId, x.EntryDate });
        builder.HasIndex(x => new { x.CompanyId, x.IsDeleted, x.EntryDate });
        builder.HasIndex(x => new { x.CompanyId, x.FinancialYearId, x.Type, x.EntryNumber });

        builder.HasMany(x => x.Lines)
            .WithOne(x => x.JournalEntry)
            .HasForeignKey(x => x.JournalEntryId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<Promix.Financials.Domain.Accounting.FinancialYear>()
            .WithMany()
            .HasForeignKey(x => x.FinancialYearId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Promix.Financials.Domain.Accounting.FinancialPeriod>()
            .WithMany()
            .HasForeignKey(x => x.FinancialPeriodId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
