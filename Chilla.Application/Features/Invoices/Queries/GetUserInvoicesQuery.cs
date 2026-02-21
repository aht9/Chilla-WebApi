using Chilla.Application.Features.Invoices.DTOs;
using Chilla.Application.Services.Interface;
using Chilla.Domain.Common;
using MediatR;

namespace Chilla.Application.Features.Invoices.Queries;

public record GetUserInvoicesQuery : IRequest<IReadOnlyList<InvoiceDto>>;

public class GetUserInvoicesQueryHandler : IRequestHandler<GetUserInvoicesQuery, IReadOnlyList<InvoiceDto>>
{
    private readonly IDapperService _dapperService;
    private readonly ICurrentUserService _currentUserService;

    public GetUserInvoicesQueryHandler(IDapperService dapperService, ICurrentUserService currentUserService)
    {
        _dapperService = dapperService;
        _currentUserService = currentUserService;
    }

    public async Task<IReadOnlyList<InvoiceDto>> Handle(GetUserInvoicesQuery request,
        CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId!.Value;

        // اجرای 2 کوئری همزمان برای بالاترین پرفورمنس
        // کوئری اول: گرفتن فاکتورهای شخص
        // کوئری دوم: گرفتن چله‌های مربوط به این فاکتورها (با Join زدن اشتراک‌ها و پلن‌ها)
        var sql = @"
            -- Query 1: Invoices
            SELECT 
                Id, TotalAmount, DiscountAmount, Amount AS PayableAmount, 
                CAST(Status AS INT) AS Status, CreatedAt, PaidAt, CouponCode
            FROM Invoices
            WHERE UserId = @UserId AND IsDeleted = 0
            ORDER BY CreatedAt DESC;

            -- Query 2: Invoice Items (Plans via UserSubscriptions)
            SELECT 
                s.InvoiceId, s.PlanId, p.Title AS PlanTitle
            FROM UserSubscriptions s
            INNER JOIN Plans p ON s.PlanId = p.Id
            WHERE s.UserId = @UserId AND s.InvoiceId IS NOT NULL AND s.IsDeleted = 0;";

        using var grid =
            await _dapperService.QueryMultipleAsync(sql, new { UserId = userId }, cancellationToken: cancellationToken);

        // خواندن نتایج کوئری اول
        var invoices = (await grid.ReadAsync<InvoiceDto>()).ToList();

        // اگر فاکتوری نداشت، لیست خالی برگردان
        if (!invoices.Any()) return invoices;

        // خواندن نتایج کوئری دوم
        var allItems = (await grid.ReadAsync<InvoiceItemRawDto>()).ToList();

        // مپ کردن آیتم‌ها به فاکتور مربوطه در مموری (بسیار سریع‌تر از Join زدن در Dapper برای روابط یک‌به‌چند)
        foreach (var invoice in invoices)
        {
            invoice.Items = allItems
                .Where(item => item.InvoiceId == invoice.Id)
                .Select(item => new InvoiceItemDto
                {
                    PlanId = item.PlanId,
                    PlanTitle = item.PlanTitle
                })
                .ToList();
        }

        return invoices;
    }
}