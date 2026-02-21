using Chilla.Domain.Aggregates.UserAggregate;

namespace Chilla.Infrastructure.Persistence.Configurations;

public class UserRoleConfiguration : IEntityTypeConfiguration<UserRole>
{
    public void Configure(EntityTypeBuilder<UserRole> builder)
    {
        builder.ToTable("UserRoles");
        builder.HasKey(r => r.Id);

        builder.HasOne(ur => ur.Role)
            .WithMany() 
            .HasForeignKey(ur => ur.RoleId)
            .OnDelete(DeleteBehavior.Restrict); 
        
        builder.Property(t => t.RowVersion).IsRowVersion();
    }
}