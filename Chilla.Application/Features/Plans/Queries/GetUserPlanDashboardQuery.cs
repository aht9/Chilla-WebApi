using System.Text.Json;
using Chilla.Application.Features.Plans.Dtos;
using Chilla.Application.Services.Interface;
using Chilla.Domain.Aggregates.PlanAggregate;
using Chilla.Domain.Common;
using Chilla.Domain.Exceptions;
using MediatR;

namespace Chilla.Application.Features.Plans.Queries;

public record GetUserPlanDashboardQuery(Guid SubscriptionId) : IRequest<PlanDashboardDto>;

public class GetUserPlanDashboardQueryHandler : IRequestHandler<GetUserPlanDashboardQuery, PlanDashboardDto>
{
    private readonly IDapperService _dapperService;
    private readonly ICurrentUserService _currentUserService;

    public GetUserPlanDashboardQueryHandler(IDapperService dapperService, ICurrentUserService currentUserService)
    {
        _dapperService = dapperService;
        _currentUserService = currentUserService;
    }

    public async Task<PlanDashboardDto> Handle(GetUserPlanDashboardQuery request, CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId;

        // کوئری چندگانه (Multiple Result Sets) برای واکشی تمام اطلاعات با یک کانکشن
        // نکته: نام جداول (مثل DailyProgresses) را با دیتابیس خود چک کنید
        const string sql = @"
            -- 1. دریافت اطلاعات اشتراک و پلن
            SELECT s.Id, s.PlanId, s.StartDate, s.Status, 
                   ISNULL(s.HasSignedCovenant, 0) AS HasSignedCovenant, 
                   p.DurationInDays
            FROM Subscriptions s
            JOIN Plans p ON s.PlanId = p.Id
            WHERE s.Id = @SubscriptionId AND s.UserId = @UserId;

            -- 2. دریافت تمام تسک‌های این چله (PlanTemplateItems)
            SELECT Id, StartDay, EndDay, TaskName, Type, ConfigJson, IsMandatory
            FROM PlanTemplateItems
            WHERE PlanId = (SELECT PlanId FROM Subscriptions WHERE Id = @SubscriptionId);

            -- 3. دریافت تاریخچه پیشرفت‌های کاربر برای این اشتراک (DailyProgress)
            SELECT TaskId, DayNumber, IsDone, CountCompleted
            FROM DailyProgress
            WHERE SubscriptionId = @SubscriptionId;
        ";

        using var multi = await _dapperService.QueryMultipleAsync(sql, new { request.SubscriptionId, UserId = userId },
            cancellationToken: cancellationToken);

        // --- پردازش نتایج ---
        var subscriptionInfo = await multi.ReadSingleOrDefaultAsync<SubscriptionInfoRaw>();
        if (subscriptionInfo == null)
            throw new NotFoundException("اشتراک یافت نشد یا متعلق به شما نیست.");

        var templateItems = (await multi.ReadAsync<TemplateItemRaw>()).ToList();
        var progresses = (await multi.ReadAsync<ProgressRaw>()).ToList();

        // محاسبه روز فعلی
        // اگر چله هنوز شروع نشده صفر، در غیر این صورت اختلاف روز + 1
        var today = DateTime.UtcNow.Date;
        var startDayDate = subscriptionInfo.StartDate.Date;
        int currentDay = today >= startDayDate
            ? (int)(today - startDayDate).TotalDays + 1
            : 0;

        // محدود کردن currentDay به سقف مجاز چله (مثلا از 40 روز بیشتر نشود)
        currentDay = Math.Min(currentDay, subscriptionInfo.DurationInDays);
        currentDay = Math.Max(currentDay, 1); // حداقل روز 1 است

        // استخراج تسک‌های امروز
        var todayTasks = templateItems
            .Where(t => t.StartDay <= currentDay && t.EndDay >= currentDay)
            .Select(t =>
            {
                // پیدا کردن پیشرفت کاربر برای این تسک در روز جاری
                var progress = progresses.FirstOrDefault(p => p.DayNumber == currentDay && p.TaskId == t.Id);

                return new TodayTaskDto(
                    TaskId: t.Id,
                    TaskName: t.TaskName,
                    Type: (TaskType)t.Type,
                    IsMandatory: t.IsMandatory,
                    ScheduleConfig: string.IsNullOrWhiteSpace(t.ConfigJson)
                        ? null
                        : JsonSerializer.Deserialize<TaskScheduleDto>(t.ConfigJson,
                            new JsonSerializerOptions { PropertyNameCaseInsensitive = true }),
                    IsCompleted: progress?.IsDone ?? false,
                    CountCompleted: progress?.CountCompleted ?? 0
                );
            }).ToList();

        // محاسبه وضعیت روزهای گذشته
        var pastDaysStatus = new List<PastDayStatusDto>();
        for (int d = 1; d < currentDay; d++)
        {
            // تسک‌های اجباری در روز d
            var mandatoryTasksForDay = templateItems
                .Where(t => t.IsMandatory && t.StartDay <= d && t.EndDay >= d)
                .Select(t => t.Id)
                .ToList();

            // تسک‌هایی که در روز d انجام شده‌اند
            var completedTasksForDay = progresses
                .Where(p => p.DayNumber == d && p.IsDone)
                .Select(p => p.TaskId)
                .ToList();

            // آیا تمام تسک‌های اجباری در این روز تیک خورده‌اند؟
            bool isFullyCompleted = mandatoryTasksForDay.All(taskId => completedTasksForDay.Contains(taskId));

            pastDaysStatus.Add(new PastDayStatusDto(d, isFullyCompleted));
        }

        return new PlanDashboardDto(
            SubscriptionId: subscriptionInfo.Id,
            CurrentDay: currentDay,
            DurationInDays: subscriptionInfo.DurationInDays,
            HasSignedCovenant: subscriptionInfo.HasSignedCovenant,
            TodayTasks: todayTasks,
            PastDaysStatus: pastDaysStatus
        );
    }

    // کلاس‌های کمکی برای Dapper Mappings (جهت خواندن سریع از دیتابیس)
    private class SubscriptionInfoRaw
    {
        public Guid Id { get; set; }
        public Guid PlanId { get; set; }
        public DateTime StartDate { get; set; }
        public int Status { get; set; }
        public bool HasSignedCovenant { get; set; }
        public int DurationInDays { get; set; }
    }

    private class TemplateItemRaw
    {
        public Guid Id { get; set; }
        public int StartDay { get; set; }
        public int EndDay { get; set; }
        public string TaskName { get; set; } = null!;
        public int Type { get; set; }
        public string ConfigJson { get; set; } = null!;
        public bool IsMandatory { get; set; }
    }

    private class ProgressRaw
    {
        public Guid TaskId { get; set; }
        public int DayNumber { get; set; }
        public bool IsDone { get; set; }
        public int CountCompleted { get; set; }
    }
}