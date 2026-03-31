using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Promix.Financials.Domain.Aggregates.Accounts;
using Promix.Financials.Domain.Aggregates.Parties;
using Promix.Financials.Domain.Security;

namespace Promix.Financials.Infrastructure.Persistence.Configurations;

public sealed class PartyConfiguration : IEntityTypeConfiguration<Party>
{
    public void Configure(EntityTypeBuilder<Party> builder)
    {
        builder.ToTable("Parties");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();
        builder.Property(x => x.RowVersion).IsRowVersion();

        builder.Property(x => x.CompanyId).IsRequired();
        builder.Property(x => x.Code).IsRequired().HasMaxLength(30);
        builder.Property(x => x.NameAr).IsRequired().HasMaxLength(150);
        builder.Property(x => x.NameEn).HasMaxLength(150);
        builder.Property(x => x.TypeFlags).IsRequired();
        builder.Property(x => x.LedgerMode).HasConversion<int>().IsRequired();
        builder.Property(x => x.Phone).HasMaxLength(40);
        builder.Property(x => x.Mobile).HasMaxLength(40);
        builder.Property(x => x.Email).HasMaxLength(120);
        builder.Property(x => x.TaxNo).HasMaxLength(50);
        builder.Property(x => x.Address).HasMaxLength(300);
        builder.Property(x => x.Notes).HasMaxLength(500);

        builder.HasIndex(x => new { x.CompanyId, x.Code }).IsUnique();

        builder.HasOne<Company>()
            .WithMany()
            .HasForeignKey(x => x.CompanyId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<Account>()
            .WithMany()
            .HasForeignKey(x => x.ReceivableAccountId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Account>()
            .WithMany()
            .HasForeignKey(x => x.PayableAccountId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
