using Chilla.Domain.Aggregates.InvoiceAggregate;

namespace Chilla.Infrastructure.Persistence.Configurations;

public class InvoiceConfiguration : IEntityTypeConfiguration<Invoice>
{
    public void Configure(EntityTypeBuilder<Invoice> builder)
    {
        builder.ToTable("Invoices");

        builder.HasKey(i => i.Id);

        builder.Property(i => i.Amount).HasColumnType("decimal(18,2)").IsRequired();
        builder.Property(i => i.Status).HasConversion<string>().HasMaxLength(20);
        
        builder.Property(i => i.Authority).HasMaxLength(100);
        builder.Property(i => i.RefId).HasMaxLength(100);
        builder.Property(i => i.GatewayName).HasMaxLength(50);
        builder.Property(i => i.Description).HasMaxLength(500);

        // ایندکس روی Authority برای جستجوی سریع در زمان کال‌بک بانک
        builder.HasIndex(i => i.Authority);
        
        // ایندکس روی UserId برای تاریخچه خریدها
        builder.HasIndex(i => i.UserId);

        builder.Property(i => i.RowVersion).IsRowVersion();
    }
}