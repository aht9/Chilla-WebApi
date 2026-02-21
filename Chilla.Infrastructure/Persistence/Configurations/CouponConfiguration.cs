using Chilla.Domain.Aggregates.CouponAggregate;

namespace Chilla.Infrastructure.Persistence.Configurations;

public class CouponConfiguration : IEntityTypeConfiguration<Coupon>
{
    public void Configure(EntityTypeBuilder<Coupon> builder)
    {
        builder.ToTable("Coupons");
        builder.HasKey(c => c.Id);
        
        // جلوگیری از ثبت کد تخفیف تکراری
        builder.HasIndex(c => c.Code).IsUnique();
        builder.Property(c => c.Code).IsRequired().HasMaxLength(50);
        
        // تنظیم دقت برای فیلدهای پولی (مبلغی)
        builder.Property(c => c.DiscountValue).HasPrecision(18, 2);
        builder.Property(c => c.MaxDiscountAmount).HasPrecision(18, 2);
        builder.Property(c => c.MinPurchaseAmount).HasPrecision(18, 2);
    }
}