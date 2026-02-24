using System.Diagnostics;
using Chilla.Application.Features.Plans.Dtos;
using Chilla.Domain.Aggregates.PlanAggregate;
using Chilla.Domain.Common;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Chilla.Application.Features.Plans.Queries;

public record GetAdminPlansQuery() : IRequest<List<AdminPlanListItemDto>>;

public class GetAdminPlansQueryHandler : IRequestHandler<GetAdminPlansQuery, List<AdminPlanListItemDto>>
{
    private readonly IDapperService _dapperService;
    private readonly ILogger<GetAdminPlansQueryHandler> _logger;

    // تزریق DapperService و Logger
    public GetAdminPlansQueryHandler(
        IDapperService dapperService, 
        ILogger<GetAdminPlansQueryHandler> logger)
    {
        _dapperService = dapperService ?? throw new ArgumentNullException(nameof(dapperService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<List<AdminPlanListItemDto>> Handle(GetAdminPlansQuery request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("شروع عملیات واکشی لیست پلن‌ها برای پنل مدیریت...");
        var stopwatch = Stopwatch.StartNew();

        try
        {
            // در پروژه‌های بزرگ بهتر است کوئری‌های SQL به جای Hard-Code شدن در هندلر، 
            // در فایل‌های resource یا کلاس‌های ثابت (Constants) نگهداری شوند.
            const string sql = @"
                SELECT 
                    p.Id, 
                    p.Title, 
                    p.Price, 
                    p.DurationInDays, 
                    p.IsActive, 
                    (SELECT COUNT(1) 
                     FROM PlanTemplateItems pti 
                     WHERE pti.PlanId = p.Id) AS TotalTasksCount
                FROM Plans p
                ORDER BY p.CreatedDate DESC"; 

            // فراخوانی دپر همراه با CancellationToken جهت لغو کوئری در صورت قطعی اتصال کاربر
            var plans = await _dapperService.QueryAsync<AdminPlanListItemDto>(
                sql, 
                param: null, 
                cancellationToken: cancellationToken);

            var result = plans.ToList();
            
            stopwatch.Stop();
            
            // لاگ موفقیت‌آمیز بودن به همراه متریک‌های پرفورمنس
            _logger.LogInformation(
                "واکشی لیست پلن‌ها با موفقیت پایان یافت. تعداد رکوردها: {Count}. زمان اجرا: {ElapsedMilliseconds} میلی‌ثانیه.", 
                result.Count, 
                stopwatch.ElapsedMilliseconds);

            return result;
        }
        catch (TaskCanceledException ex)
        {
            // مدیریت خطای لغو شدن درخواست توسط کلاینت (مثلاً کاربر صفحه را بسته است)
            _logger.LogWarning(ex, "درخواست دریافت لیست پلن‌ها توسط کاربر لغو شد.");
            throw; 
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            
            // لاگ خطای دیتابیس با جزئیات زمان اجرا برای پیدا کردن Bottleneck ها
            _logger.LogError(ex, 
                "خطای پیش‌بینی نشده هنگام خواندن لیست پلن‌ها از دیتابیس. زمان سپری شده تا لحظه خطا: {ElapsedMilliseconds} میلی‌ثانیه.", 
                stopwatch.ElapsedMilliseconds);
            
            // خطا را به سمت بالا پرتاب می‌کنیم تا GlobalExceptionHandler سیستم آن را تبدیل به خطای 500 استاندارد کند
            throw; 
        }
    }
}