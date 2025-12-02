using Chilla.Domain.Aggregates.UserAggregate;

namespace Chilla.Infrastructure.Persistence.Configurations;

public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("Users");

        builder.HasKey(u => u.Id);

        // --- Properties ---
        builder.Property(u => u.FirstName).HasMaxLength(100).IsRequired();
        builder.Property(u => u.LastName).HasMaxLength(100).IsRequired();
        builder.Property(u => u.Username).HasMaxLength(50).IsRequired();
        builder.Property(u => u.PhoneNumber).HasMaxLength(15).IsRequired();
        builder.Property(u => u.Email).HasMaxLength(255).IsRequired(false);
        builder.Property(u => u.PasswordHash).IsRequired(false); // Nullable for OTP users

        // --- Indexes ---
        builder.HasIndex(u => u.Username).IsUnique();
        builder.HasIndex(u => u.PhoneNumber).IsUnique();
        // اگر ایمیل وارد شده باشد باید یکتا باشد
        builder.HasIndex(u => u.Email).IsUnique().HasFilter("[Email] IS NOT NULL");

        // --- Concurrency ---
        builder.Property(u => u.RowVersion).IsRowVersion();

        // --- Owned Types / Relationships ---
        
        // 1. Refresh Tokens (Owned Collection)
        // این جدول چرخه حیاتش کاملاً وابسته به User است
        builder.OwnsMany(u => u.RefreshTokens, tokenBuilder =>
        {
            tokenBuilder.ToTable("UserRefreshTokens");
            tokenBuilder.HasKey(t => t.Id);
            tokenBuilder.WithOwner().HasForeignKey("UserId");
            
            tokenBuilder.Property(t => t.Token).HasMaxLength(500).IsRequired();
            tokenBuilder.Property(t => t.CreatedByIp).HasMaxLength(50);
            tokenBuilder.Property(t => t.RevokedByIp).HasMaxLength(50);
            
            // دسترسی EF به فیلد خصوصی _refreshTokens
            tokenBuilder.UsePropertyAccessMode(PropertyAccessMode.Field);
        });

        // 2. Roles (Many-to-Many via Join Entity logic implemented as Owned/Collection)
        // در دامین شما UserRole به صورت یک Entity داخلی در لیست _roles تعریف شده است
        builder.OwnsMany(u => u.Roles, roleBuilder =>
        {
            roleBuilder.ToTable("UserRoles");
            roleBuilder.HasKey(r => r.Id);
            roleBuilder.WithOwner().HasForeignKey("UserId");
            
            roleBuilder.HasIndex(r => r.RoleId);
            
            roleBuilder.UsePropertyAccessMode(PropertyAccessMode.Field);
        });
        
        // دسترسی به فیلدهای readonly
        builder.Metadata.FindNavigation(nameof(User.RefreshTokens))!
            .SetPropertyAccessMode(PropertyAccessMode.Field);
            
        builder.Metadata.FindNavigation(nameof(User.Roles))!
            .SetPropertyAccessMode(PropertyAccessMode.Field);
    }
}