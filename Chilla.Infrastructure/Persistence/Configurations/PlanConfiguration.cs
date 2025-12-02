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

        // --- Relationships ---
        // Template Items are part of the Aggregate. Cascade Delete is required.
        builder.OwnsMany(p => p.Items, itemBuilder =>
        {
            itemBuilder.ToTable("PlanTemplateItems");
            itemBuilder.HasKey(i => i.Id);
            itemBuilder.WithOwner().HasForeignKey("PlanId");

            itemBuilder.Property(i => i.TaskName).HasMaxLength(200).IsRequired();
            
            // ذخیره Enum به صورت String برای خوانایی دیتابیس و جلوگیری از مشکلات تغییر ترتیب Enum
            itemBuilder.Property(i => i.Type)
                .HasConversion<string>()
                .HasMaxLength(50);

            // JSON Config نگهداری می‌شود
            itemBuilder.Property(i => i.ConfigJson).HasColumnType("nvarchar(max)");
            
            itemBuilder.UsePropertyAccessMode(PropertyAccessMode.Field);
        });

        builder.Metadata.FindNavigation(nameof(Plan.Items))!
            .SetPropertyAccessMode(PropertyAccessMode.Field);
            
        builder.Property(p => p.RowVersion).IsRowVersion();
    }
}