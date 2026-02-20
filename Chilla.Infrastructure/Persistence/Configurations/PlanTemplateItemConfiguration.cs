using Chilla.Domain.Aggregates.PlanAggregate;

namespace Chilla.Infrastructure.Persistence.Configurations;

public class PlanTemplateItemConfiguration : IEntityTypeConfiguration<PlanTemplateItem>
{
    public void Configure(EntityTypeBuilder<PlanTemplateItem> builder)
    {
        builder.ToTable("PlanTemplateItems");
        builder.HasKey(i => i.Id);

        builder.HasIndex("PlanId");
        
        builder.Property(i => i.StartDay).IsRequired();
        builder.Property(i => i.EndDay).IsRequired();
        
        // افزودن ستون نوتیفیکیشن (ذخیره به صورت int چون Flag است)
        builder.Property(i => i.RequiredNotifications)
            .HasConversion<int>(); // یا string اگر خوانایی دیتابیس مهم است

        builder.Property(i => i.TaskName).HasMaxLength(200).IsRequired();
        
        builder.Property(i => i.Type)
            .HasConversion<string>()
            .HasMaxLength(50);

        builder.Property(i => i.ConfigJson).HasColumnType("nvarchar(max)");
        
        builder.Property(x => x.RowVersion).IsRowVersion();
    }
}