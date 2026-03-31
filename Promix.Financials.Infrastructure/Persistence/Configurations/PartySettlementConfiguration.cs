using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Promix.Financials.Domain.Aggregates.Accounts;
using Promix.Financials.Domain.Aggregates.Journals;
using Promix.Financials.Domain.Aggregates.Parties;

namespace Promix.Financials.Infrastructure.Persistence.Configurations;

public sealed class PartySettlementConfiguration : IEntityTypeConfiguration<PartySettlement>
{
    public void Configure(EntityTypeBuilder<PartySettlement> builder)
    {
        builder.ToTable("PartySettlements");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();
        builder.Property(x => x.RowVersion).IsRowVersion();

        builder.Property(x => x.CompanyId).IsRequired();
        builder.Property(x => x.PartyId).IsRequired();
        builder.Property(x => x.AccountId).IsRequired();
        builder.Property(x => x.DebitLineId).IsRequired();
        builder.Property(x => x.CreditLineId).IsRequired();
        builder.Property(x => x.Amount).HasPrecision(18, 2).IsRequired();
        builder.Property(x => x.SettledOn).IsRequired();
        builder.Property(x => x.CreatedByUserId).IsRequired();
        builder.Property(x => x.CreatedAtUtc).IsRequired();

        builder.HasIndex(x => new { x.CompanyId, x.PartyId, x.AccountId });

        builder.HasOne<Party>()
            .WithMany()
            .HasForeignKey(x => x.PartyId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<Account>()
            .WithMany()
            .HasForeignKey(x => x.AccountId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<JournalLine>()
            .WithMany()
            .HasForeignKey(x => x.DebitLineId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<JournalLine>()
            .WithMany()
            .HasForeignKey(x => x.CreditLineId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
