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

        builder.OwnsMany(r => r.Permissions, permBuilder =>
        {
            permBuilder.ToTable("RolePermissions");
            permBuilder.HasKey(p => p.Id);
            permBuilder.WithOwner().HasForeignKey("RoleId");

            permBuilder.Property(p => p.Permission)
                .HasConversion<string>()
                .HasMaxLength(100);
                
            permBuilder.UsePropertyAccessMode(PropertyAccessMode.Field);
        });

        builder.Metadata.FindNavigation(nameof(Role.Permissions))!
            .SetPropertyAccessMode(PropertyAccessMode.Field);
    }
}