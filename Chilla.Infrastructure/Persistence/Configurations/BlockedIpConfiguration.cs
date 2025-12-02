namespace Chilla.Infrastructure.Persistence.Configurations;

public class BlockedIpConfiguration : IEntityTypeConfiguration<BlockedIp>
{
    public void Configure(EntityTypeBuilder<BlockedIp> builder)
    {
        builder.ToTable("BlockedIps");
        builder.HasKey(b => b.Id);
        builder.Property(b => b.IpAddress).HasMaxLength(50).IsRequired();
        builder.HasIndex(b => b.IpAddress);
    }
}

public class RequestLogConfiguration : IEntityTypeConfiguration<RequestLog>
{
    public void Configure(EntityTypeBuilder<RequestLog> builder)
    {
        builder.ToTable("RequestLogs");
        builder.HasKey(r => r.Id);
        builder.Property(r => r.IpAddress).HasMaxLength(50);
        builder.Property(r => r.Endpoint).HasMaxLength(200);
        
        // این لاگ‌ها زیاد می‌شوند، ایندکس روی تاریخ برای پاکسازی دوره‌ای مفید است
        builder.HasIndex(r => r.OccurredOn);
    }
}