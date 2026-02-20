using Chilla.Domain.Aggregates.UserAggregate;

namespace Chilla.Infrastructure.Persistence.Configurations;

public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("Users");
        builder.HasKey(u => u.Id);

        // --- Properties ---
        builder.Property(u => u.FirstName).HasMaxLength(100).IsRequired(false);
        builder.Property(u => u.LastName).HasMaxLength(100).IsRequired(false);
        builder.Property(u => u.Username).HasMaxLength(50).IsRequired();
        builder.Property(u => u.PhoneNumber).HasMaxLength(15).IsRequired();
        builder.Property(u => u.Email).HasMaxLength(255).IsRequired(false);
        builder.Property(u => u.PasswordHash).IsRequired(false);

        // --- Indexes ---
        builder.HasIndex(u => u.Username).IsUnique();
        builder.HasIndex(u => u.PhoneNumber).IsUnique();
        builder.HasIndex(u => u.Email).IsUnique().HasFilter("[Email] IS NOT NULL");

        builder.Property(u => u.RowVersion).IsRowVersion();

        // --- Relationships (Refactored to HasMany) ---

        // 1. Refresh Tokens
        builder.HasMany(u => u.RefreshTokens)
            .WithOne()
            .HasForeignKey("UserId") // Shadow FK
            .OnDelete(DeleteBehavior.Cascade);
        
        // 2. Roles
        builder.HasMany(u => u.Roles)
            .WithOne()
            .HasForeignKey("UserId") // Shadow FK
            .OnDelete(DeleteBehavior.Cascade);
    }
}