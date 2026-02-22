

using Chilla.Domain.Aggregates.NotificationAggregate;
using Chilla.Domain.Aggregates.PlanAggregate;

namespace Chilla.Application.Features.Plans.Dtos;

public record PlanItemInputDto(
    int StartDay,       // روز شروع (مثلا 1)
    int EndDay,         // روز پایان (مثلا 5)
    string TaskName,    // نام تسک (مثلا "ذکر صبحگاهی")
    TaskType Type,      // نوع تسک
    bool IsMandatory,   // اجباری بودن
    
    // تنظیمات نوتیفیکیشن (می‌تواند Flag Enum باشد)
    NotificationType NotificationType, 
    
    // جزئیات زمانبندی مذهبی (به صورت آبجکت می‌گیریم، در هندلر تبدیل به JSON می‌کنیم)
    TaskScheduleDto ScheduleConfig 
);