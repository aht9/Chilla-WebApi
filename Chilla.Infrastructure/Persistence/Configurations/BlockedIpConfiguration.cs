namespace Chilla.Infrastructure.Persistence.Configurations;


public class BlockedIpConfiguration : IEntityTypeConfiguration<BlockedIp>
{
    public void Configure(EntityTypeBuilder<BlockedIp> builder)
    {
        builder.ToTable("BlockedIps");
        builder.HasKey(b => b.Id);
        
        builder.Property(b => b.IpAddress).HasMaxLength(45).IsRequired(); // IPv6 support
        builder.HasIndex(b => b.IpAddress);
        
        builder.Property(x => x.RowVersion).IsRowVersion();
    }
}


public class RequestLogConfiguration : IEntityTypeConfiguration<RequestLog>
{
    public void Configure(EntityTypeBuilder<RequestLog> builder)
    {
        builder.ToTable("RequestLogs");
        builder.HasKey(r => r.Id);
        
        builder.Property(r => r.IpAddress).HasMaxLength(45);
        builder.Property(r => r.Endpoint).HasMaxLength(255);
        
        // TTL Indexing logic requires specific DB features, here generic index:
        builder.HasIndex(r => r.OccurredOn); 
        builder.Property(x => x.RowVersion).IsRowVersion();
    }
}