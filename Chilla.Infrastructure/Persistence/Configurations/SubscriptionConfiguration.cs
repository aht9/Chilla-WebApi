using Chilla.Domain.Aggregates.SubscriptionAggregate;

namespace Chilla.Infrastructure.Persistence.Configurations;

public class SubscriptionConfiguration : IEntityTypeConfiguration<UserSubscription>
{
    public void Configure(EntityTypeBuilder<UserSubscription> builder)
    {
        builder.ToTable("UserSubscriptions");
        builder.HasKey(s => s.Id);

        builder.Property(s => s.Status)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.HasIndex(s => s.UserId);
        builder.HasIndex(s => s.PlanId);

        // [تغییر اساسی]: تبدیل OwnsMany به HasMany
        builder.HasMany(s => s.Progress)
            .WithOne()
            .HasForeignKey("UserSubscriptionId") // تعریف Shadow FK
            .OnDelete(DeleteBehavior.Cascade);   // حذف آبشاری (مشابه رفتار Owned)
            
        builder.Property(s => s.RowVersion).IsRowVersion();
    }
}