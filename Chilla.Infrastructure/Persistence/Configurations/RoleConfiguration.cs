using Chilla.Domain.Aggregates.RoleAggregate;

namespace Chilla.Infrastructure.Persistence.Configurations;

public class RoleConfiguration : IEntityTypeConfiguration<Role>
{
    public void Configure(EntityTypeBuilder<Role> builder)
    {
        builder.ToTable("Roles");
        builder.HasKey(r => r.Id);

        builder.Property(r => r.Name).HasMaxLength(50).IsRequired();
        builder.HasIndex(r => r.Name).IsUnique();
        
        // --- Relationships (Refactored to HasMany) ---
        
        builder.HasMany(r => r.Permissions)
            .WithOne()
            .HasForeignKey("RoleId") // Shadow FK
            .OnDelete(DeleteBehavior.Cascade);
    }
}