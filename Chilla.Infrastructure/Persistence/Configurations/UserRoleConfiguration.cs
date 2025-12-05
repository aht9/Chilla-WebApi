using Chilla.Domain.Aggregates.UserAggregate;

namespace Chilla.Infrastructure.Persistence.Configurations;

public class UserRoleConfiguration : IEntityTypeConfiguration<UserRole>
{
    public void Configure(EntityTypeBuilder<UserRole> builder)
    {
        builder.ToTable("UserRoles");
        builder.HasKey(r => r.Id);

        // Shadow FK Index
        builder.HasIndex("UserId");
        
        builder.HasIndex(r => r.RoleId);
    }
}