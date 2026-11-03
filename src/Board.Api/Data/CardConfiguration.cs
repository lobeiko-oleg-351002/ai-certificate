using Board.Api.Cards;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Board.Api.Data;

public sealed class CardConfiguration : IEntityTypeConfiguration<Card>
{
    public void Configure(EntityTypeBuilder<Card> builder)
    {
        builder.HasKey(c => c.Key);
        builder.Property(c => c.Key).HasMaxLength(16);
        builder.HasIndex(c => c.Number).IsUnique();
        builder.Property(c => c.Title).HasMaxLength(CardLimits.Title).IsRequired();
        builder.Property(c => c.Description).HasMaxLength(CardLimits.Description).IsRequired();
        builder.Property(c => c.Status).HasConversion<string>().HasMaxLength(16);
        builder.Property(c => c.Phase).HasMaxLength(CardLimits.Label);
        builder.Property(c => c.PendingGate).HasMaxLength(CardLimits.Label);
    }
}
