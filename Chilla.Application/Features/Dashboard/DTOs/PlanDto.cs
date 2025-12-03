namespace Chilla.Application.Features.Dashboard.DTOs;

public record PlanDto(
    Guid Id, 
    string Title, 
    decimal Price, 
    int DurationDays,
    string? Description
);