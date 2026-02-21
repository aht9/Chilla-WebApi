namespace Chilla.Application.Features.Invoices.DTOs;

public class InvoiceDto
{
    public Guid Id { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal PayableAmount { get; set; }
    public int Status { get; set; } 
    
    // تبدیل وضعیت عددی به متن فارسی برای نمایش راحت در فرانت‌اند
    public string StatusName => Status switch
    {
        0 => "در انتظار پرداخت",
        1 => "پرداخت شده",
        2 => "ناموفق",
        3 => "لغو شده",
        _ => "نامشخص"
    };

    public DateTime CreatedAt { get; set; }
    public DateTime? PaidAt { get; set; }
    public string? CouponCode { get; set; }
    
    public List<InvoiceItemDto> Items { get; set; } = new();
}