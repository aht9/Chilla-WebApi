using Chilla.Domain.Aggregates.SubscriptionAggregate;

namespace Chilla.Infrastructure.Persistence.Configurations;

public class DailyProgressConfiguration : IEntityTypeConfiguration<DailyProgress>
{
    public void Configure(EntityTypeBuilder<DailyProgress> builder)
    {
        builder.ToTable("DailyProgresses");
        builder.HasKey(dp => dp.Id);

        // ایندکس روی کلید خارجی (چون Shadow Property است با رشته تعریف می‌شود)
        builder.HasIndex("UserSubscriptionId");

        builder.Property(dp => dp.ScheduledDate).HasColumnType("date"); 
        builder.HasIndex(dp => dp.ScheduledDate);
        
        builder.Property(dp => dp.CompletedAt).IsRequired(false);
        builder.Property(dp => dp.IsLateEntry).HasDefaultValue(false);
        builder.Property(dp => dp.LateReason).HasMaxLength(500).IsRequired(false);
        
        builder.Property(x => x.RowVersion).IsRowVersion();
    }
}