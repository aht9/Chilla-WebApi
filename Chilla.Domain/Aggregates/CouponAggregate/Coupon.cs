using Chilla.Domain.Exceptions;

namespace Chilla.Domain.Aggregates.CouponAggregate;

public class Coupon : BaseEntity , IAggregateRoot
{
    public string Code { get; private set; }
    public DiscountType DiscountType { get; private set; }
    public decimal DiscountValue { get; private set; } // درصد یا مبلغ ریالی
    public decimal? MaxDiscountAmount { get; private set; } // سقف تخفیف (برای درصدی)
    public decimal? MinPurchaseAmount { get; private set; } // حداقل مبلغ سبد خرید برای اعمال کوپن
    
    public int? MaxUsageCount { get; private set; } // حداکثر ظرفیت استفاده کل
    public int CurrentUsageCount { get; private set; } // تعداد استفاده شده تا الان
    
    public DateTime? StartDate { get; private set; }
    public DateTime? EndDate { get; private set; }
    public bool IsActive { get; private set; }
    
    public Guid? SpecificUserId { get; private set; } // اگر نال باشد یعنی برای همه است

    private Coupon() { } // For EF Core

    public Coupon(string code, DiscountType type, decimal value, decimal? maxDiscount, 
        decimal? minPurchase, int? maxUsage, DateTime? startDate, DateTime? endDate, Guid? specificUserId)
    {
        Code = code.ToUpper();
        DiscountType = type;
        DiscountValue = value;
        MaxDiscountAmount = maxDiscount;
        MinPurchaseAmount = minPurchase;
        MaxUsageCount = maxUsage;
        StartDate = startDate;
        EndDate = endDate;
        SpecificUserId = specificUserId;
        IsActive = true;
        CurrentUsageCount = 0;
    }

    // متد اعتبارسنجی کوپن در لایه دامین
    public void ValidateForUse(Guid userId, decimal cartTotalAmount)
    {
        if (!IsActive) throw new DomainException("این کد تخفیف غیرفعال شده است.");
        
        if (SpecificUserId.HasValue && SpecificUserId.Value != userId)
            throw new DomainException("این کد تخفیف مختص شما نیست.");

        if (StartDate.HasValue && DateTime.UtcNow < StartDate.Value)
            throw new DomainException("زمان شروع استفاده از این کد تخفیف نرسیده است.");

        if (EndDate.HasValue && DateTime.UtcNow > EndDate.Value)
            throw new DomainException("این کد تخفیف منقضی شده است.");

        if (MaxUsageCount.HasValue && CurrentUsageCount >= MaxUsageCount.Value)
            throw new DomainException("ظرفیت استفاده از این کد تخفیف به پایان رسیده است.");

        if (MinPurchaseAmount.HasValue && cartTotalAmount < MinPurchaseAmount.Value)
            throw new DomainException($"حداقل مبلغ خرید برای این کد تخفیف {MinPurchaseAmount} ریال می‌باشد.");
    }

    // متد محاسبه مبلغ نهایی تخفیف
    public decimal CalculateDiscountAmount(decimal cartTotalAmount)
    {
        if (DiscountType == DiscountType.FixedAmount)
        {
            return DiscountValue > cartTotalAmount ? cartTotalAmount : DiscountValue;
        }

        // Percentage logic
        var discount = cartTotalAmount * (DiscountValue / 100);
        if (MaxDiscountAmount.HasValue && discount > MaxDiscountAmount.Value)
        {
            return MaxDiscountAmount.Value;
        }
        
        return discount;
    }
    
    public void Update(string code, DiscountType type, decimal value, decimal? maxDiscount, 
        decimal? minPurchase, int? maxUsage, DateTime? startDate, DateTime? endDate, Guid? specificUserId, bool isActive)
    {
        Code = code.ToUpper();
        DiscountType = type;
        DiscountValue = value;
        MaxDiscountAmount = maxDiscount;
        MinPurchaseAmount = minPurchase;
        MaxUsageCount = maxUsage;
        StartDate = startDate;
        EndDate = endDate;
        SpecificUserId = specificUserId;
        IsActive = isActive;
    }

    // متد ثبت استفاده از کوپن (در زمان نهایی شدن خرید صدا زده می‌شود)
    public void IncrementUsage()
    {
        CurrentUsageCount++;
        if (MaxUsageCount.HasValue && CurrentUsageCount >= MaxUsageCount.Value)
        {
            IsActive = false; // غیرفعال شدن خودکار پس از اتمام ظرفیت
        }
    }
}