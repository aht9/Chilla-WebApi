using Chilla.Domain.Aggregates.PlanAggregate;

namespace Chilla.Infrastructure.Persistence.Configurations;

public class PlanConfiguration : IEntityTypeConfiguration<Plan>
{
    public void Configure(EntityTypeBuilder<Plan> builder)
    {
        builder.ToTable("Plans");
        builder.HasKey(p => p.Id);

        builder.Property(p => p.Title).HasMaxLength(200).IsRequired();
        builder.Property(p => p.Description).HasMaxLength(2000);
        builder.Property(p => p.Price).HasColumnType("decimal(18,2)");

        // [تغییر اساسی]: تبدیل OwnsMany به HasMany
        builder.HasMany(p => p.Items)
            .WithOne()
            .HasForeignKey("PlanId")
            .OnDelete(DeleteBehavior.Cascade);
            
        builder.Property(p => p.RowVersion).IsRowVersion();
    }
}