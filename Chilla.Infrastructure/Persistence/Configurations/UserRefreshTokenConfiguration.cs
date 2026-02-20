using Chilla.Domain.Aggregates.UserAggregate;

namespace Chilla.Infrastructure.Persistence.Configurations;

public class UserRefreshTokenConfiguration : IEntityTypeConfiguration<UserRefreshToken>
{
    public void Configure(EntityTypeBuilder<UserRefreshToken> builder)
    {
        builder.ToTable("UserRefreshTokens");
        builder.HasKey(t => t.Id);
        
        // Shadow FK Index
        builder.HasIndex("UserId");

        builder.Property(t => t.Token).HasMaxLength(500).IsRequired();
        builder.Property(t => t.CreatedByIp).HasMaxLength(50);
        builder.Property(t => t.RevokedByIp).HasMaxLength(50);
        builder.Property(t => t.RowVersion).IsRowVersion();
        // Soft Delete به صورت خودکار توسط AppDbContext اعمال می‌شود
    }
}