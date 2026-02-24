using System.Diagnostics;
using Chilla.Application.Features.Plans.Dtos;
using Chilla.Application.Services.Interface;
using Chilla.Domain.Common;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Chilla.Application.Features.Plans.Queries;

public record GetUserSubscriptionsQuery() : IRequest<List<UserPlanListItemDto>>;

public class GetUserSubscriptionsQueryHandler : IRequestHandler<GetUserSubscriptionsQuery, List<UserPlanListItemDto>>
{
    private readonly IDapperService _dapperService;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<GetUserSubscriptionsQueryHandler> _logger;

    public GetUserSubscriptionsQueryHandler(
        IDapperService dapperService,
        ICurrentUserService currentUserService,
        ILogger<GetUserSubscriptionsQueryHandler> logger)
    {
        _dapperService = dapperService ?? throw new ArgumentNullException(nameof(dapperService));
        _currentUserService = currentUserService ?? throw new ArgumentNullException(nameof(currentUserService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<List<UserPlanListItemDto>> Handle(GetUserSubscriptionsQuery request, CancellationToken cancellationToken)
    {
        // ۱. دریافت شناسه کاربری به صورت امن از توکن JWT
        var userId = _currentUserService.UserId;
        if (userId == Guid.Empty)
            throw new UnauthorizedAccessException("کاربر احراز هویت نشده است.");

        _logger.LogInformation("شروع دریافت لیست پلن‌های کاربر با شناسه: {UserId}", userId);
        var stopwatch = Stopwatch.StartNew();

        try
        {
            // ۲. کوئری SQL برای دریافت اطلاعات اشتراک کاربر و پلن‌ها
            // نکته: اگر نام جدول شما UserSubscriptions است، آن را جایگزین Subscriptions کنید.
            // وضعیت (Status) ممکن است در دیتابیس شما Enum (عدد) باشد. 
            const string sql = @"
                SELECT 
                    s.Id AS SubscriptionId,
                    p.Id AS PlanId,
                    p.Title,
                    s.StartDate,
                    s.EndDate,
                    -- اگر Status به صورت عدد (Enum) ذخیره می‌شود، در DTO هم می‌توانید عدد بگیرید یا در SQL کست (Cast) کنید
                    CAST(s.Status AS VARCHAR(50)) AS Status, 
                    p.DurationInDays,
                    
                    -- محاسبه تعداد روزهای سپری شده در پایگاه داده
                    CASE 
                        WHEN s.Status = 2 /* مثلاً 2 یعنی Completed */ THEN p.DurationInDays
                        WHEN s.StartDate > GETUTCDATE() THEN 0 /* هنوز شروع نشده */
                        ELSE DATEDIFF(DAY, s.StartDate, GETUTCDATE()) + 1 /* با احتساب روز اول */
                    END AS DaysPassed
                FROM Subscriptions s
                INNER JOIN Plans p ON s.PlanId = p.Id
                WHERE s.UserId = @UserId
                ORDER BY s.StartDate DESC";

            // ۳. اجرای کوئری با دپر
            var rawResults = await _dapperService.QueryAsync<UserPlanListItemDto>(
                sql, 
                new { UserId = userId }, 
                cancellationToken: cancellationToken);

            // ۴. محاسبه درصد پیشرفت (Progress Percentage) به صورت ایمن (در مموری)
            var finalList = rawResults.Select(item => 
            {
                // جلوگیری از عبور روزهای گذشته از کل طول دوره (مثلاً روز 45 از چله 40 روزه)
                int actualDaysPassed = Math.Min(item.DaysPassed, item.DurationInDays);
                actualDaysPassed = Math.Max(actualDaysPassed, 0); // جلوگیری از عدد منفی
                
                // محاسبه درصد
                int percentage = item.DurationInDays > 0 
                    ? (int)Math.Round((double)actualDaysPassed / item.DurationInDays * 100) 
                    : 0;

                return item with { 
                    DaysPassed = actualDaysPassed,
                    ProgressPercentage = percentage
                };
            }).ToList();

            stopwatch.Stop();
            _logger.LogInformation("واکشی لیست پلن‌های کاربر با موفقیت پایان یافت. تعداد: {Count}, زمان: {Elapsed}ms", 
                finalList.Count, stopwatch.ElapsedMilliseconds);

            return finalList;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex, "خطا در واکشی پلن‌های کاربر. زمان صرف شده: {Elapsed}ms", stopwatch.ElapsedMilliseconds);
            throw;
        }
    }
}