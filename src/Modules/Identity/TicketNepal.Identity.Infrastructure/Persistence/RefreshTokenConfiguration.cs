using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TicketNepal.Identity.Domain;

namespace TicketNepal.Identity.Infrastructure.Persistence;

internal sealed class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
{
    public void Configure(EntityTypeBuilder<RefreshToken> builder)
    {
        builder.ToTable("refresh_tokens");
        builder.HasKey(t => t.Id);
        builder.Property(t => t.Id).ValueGeneratedNever();

        builder.Property(t => t.TokenHash).HasMaxLength(64).IsRequired(); // hex SHA-256
        builder.Property(t => t.ReplacedByTokenHash).HasMaxLength(64);
        builder.Property(t => t.CreatedByIp).HasMaxLength(45);            // fits IPv6

        builder.HasIndex(t => t.TokenHash).IsUnique();
        builder.HasIndex(t => t.FamilyId);
        builder.HasIndex(t => t.UserId);

        builder.HasOne<ApplicationUser>().WithMany()
            .HasForeignKey(t => t.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}