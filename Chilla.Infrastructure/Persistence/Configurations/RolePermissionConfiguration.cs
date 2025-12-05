using Chilla.Domain.Aggregates.RoleAggregate;

namespace Chilla.Infrastructure.Persistence.Configurations;

public class RolePermissionConfiguration : IEntityTypeConfiguration<RolePermission>
{
    public void Configure(EntityTypeBuilder<RolePermission> builder)
    {
        builder.ToTable("RolePermissions");
        builder.HasKey(p => p.Id);

        // Shadow FK Index
        builder.HasIndex("RoleId");

        builder.Property(p => p.Permission)
            .HasConversion<string>()
            .HasMaxLength(100);
    }
}