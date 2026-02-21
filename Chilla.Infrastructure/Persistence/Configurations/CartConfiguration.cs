using Chilla.Domain.Aggregates.CartAggregate;

namespace Chilla.Infrastructure.Persistence.Configurations;

public class CartConfiguration : IEntityTypeConfiguration<Cart>
{
    public void Configure(EntityTypeBuilder<Cart> builder)
    {
        builder.ToTable("Carts");
        builder.HasKey(c => c.Id);

        // هر کاربر فقط می‌تواند یک سبد خرید فعال داشته باشد
        builder.HasIndex(c => c.UserId).IsUnique();

        builder.Property(c => c.CouponCode).HasMaxLength(50);

        builder.HasMany(c => c.Items)
            .WithOne()
            .HasForeignKey(ci => ci.CartId)
            .OnDelete(DeleteBehavior.Cascade); 
        
        builder.Property(c => c.RowVersion).IsRowVersion();
    }
}

public class CartItemConfiguration : IEntityTypeConfiguration<CartItem>
{
    public void Configure(EntityTypeBuilder<CartItem> builder)
    {
        builder.ToTable("CartItems");
        builder.HasKey(ci => ci.Id);
        builder.Property(ci => ci.Price).HasPrecision(18, 2);
        
        builder.Property(c => c.RowVersion).IsRowVersion();
    }
}