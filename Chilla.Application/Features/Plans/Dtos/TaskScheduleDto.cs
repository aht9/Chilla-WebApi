namespace Chilla.Application.Features.Plans.Dtos;

public record TaskScheduleDto(
    int TargetCount, 
    string TimeReference, // "RelativeToFajr", "FixedTime", ...
    int StartOffsetMinutes, 
    int DurationMinutes,
    string Frequency,         // "Daily", "Weekly", "Once", ...
    int? FrequencyValue,      // 2 (e.g. 2 times a week)
    string? Description,
    List<string>? Instructions,
    List<string>? Warnings,
    bool RequiresUnbrokenChain,
    string? PostPlanNotes
);