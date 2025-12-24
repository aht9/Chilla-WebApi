using FluentValidation;

namespace Chilla.Application.Features.Plans.Commands.CreatePlan;

public class CreatePlanCommandValidator : AbstractValidator<CreatePlanCommand>
{
    public CreatePlanCommandValidator()
    {
        RuleFor(x => x.Title).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Description).NotEmpty();
        RuleFor(x => x.Price).GreaterThanOrEqualTo(0);
        RuleFor(x => x.DurationInDays).GreaterThan(0).LessThanOrEqualTo(100); // مثلاً چله ۴۰ روز است
        
        RuleForEach(x => x.Items).ChildRules(item =>
        {
            item.RuleFor(x => x.StartDay).GreaterThan(0);
            item.RuleFor(x => x.EndDay).GreaterThanOrEqualTo(x => x.StartDay);
            item.RuleFor(x => x.TaskName).NotEmpty();
            item.RuleFor(x => x.ConfigJson).Must(BeValidJson).WithMessage("Invalid JSON format.");
        });
    }

    private bool BeValidJson(string json)
    {
        if (string.IsNullOrWhiteSpace(json)) return true; // Null/Empty is handled nicely or allowed
        try
        {
            System.Text.Json.JsonDocument.Parse(json);
            return true;
        }
        catch
        {
            return false;
        }
    }
}