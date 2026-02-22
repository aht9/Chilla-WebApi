using Chilla.Application.Features.Plans.Dtos;
using FluentValidation;

namespace Chilla.Application.Features.Plans.Commands.CreatePlan;

public class CreatePlanCommandValidator : AbstractValidator<CreatePlanCommand>
{
    public CreatePlanCommandValidator()
    {
        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("عنوان پلن نمی‌تواند خالی باشد.")
            .MaximumLength(200);

        RuleFor(x => x.Description)
            .NotEmpty().WithMessage("توضیحات پلن الزامی است.");

        RuleFor(x => x.Price)
            .GreaterThanOrEqualTo(0).WithMessage("قیمت نمی‌تواند منفی باشد.");

        RuleFor(x => x.DurationInDays)
            .GreaterThan(0).WithMessage("مدت زمان پلن باید حداقل ۱ روز باشد.")
            .LessThanOrEqualTo(100).WithMessage("حداکثر مدت زمان مجاز ۱۰۰ روز است.");

        // اعتبارسنجی لیست آیتم‌ها
        RuleForEach(x => x.Items).SetValidator(new PlanItemInputDtoValidator());
    }
}

public class PlanItemInputDtoValidator : AbstractValidator<PlanItemInputDto>
{
    public PlanItemInputDtoValidator()
    {
        RuleFor(x => x.TaskName)
            .NotEmpty().WithMessage("نام فعالیت (Task) الزامی است.");

        RuleFor(x => x.StartDay)
            .GreaterThan(0).WithMessage("روز شروع باید بزرگتر از صفر باشد.");

        RuleFor(x => x.EndDay)
            .GreaterThanOrEqualTo(x => x.StartDay).WithMessage("روز پایان نمی‌تواند قبل از روز شروع باشد.");

        // اعتبارسنجی آبجکت ScheduleConfig (جایگزین ConfigJson قدیمی)
        RuleFor(x => x.ScheduleConfig)
            .NotNull().WithMessage("تنظیمات زمان‌بندی الزامی است.")
            .SetValidator(new TaskScheduleDtoValidator());
    }
}

public class TaskScheduleDtoValidator : AbstractValidator<TaskScheduleDto>
{
    public TaskScheduleDtoValidator()
    {
        RuleFor(x => x.TargetCount)
            .GreaterThan(0).WithMessage("تعداد انجام کار باید حداقل ۱ باشد.");

        RuleFor(x => x.DurationMinutes)
            .GreaterThan(0).WithMessage("مدت زمان انجام کار باید معتبر باشد.");
            
        // نیازی به اعتبارسنجی جیسون نیست چون الان آبجکت Strongly-Typed داریم
    }
}