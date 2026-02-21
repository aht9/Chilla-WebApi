using Chilla.Application.Features.Plans.Commands.CreatePlan;
using Chilla.Application.Features.Plans.Commands.DeletePlan;
using Chilla.Application.Features.Plans.Commands.UpdatePlan;
using Chilla.Application.Features.Plans.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Chilla.WebApi.Controllers.Admin;

[Authorize(Roles = "SuperAdmin")] 
[Route("api/admin/plans")]
[ApiController]
public class AdminPlansController : ControllerBase
{
    private readonly IMediator _mediator;

    public AdminPlansController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpPost]
    public async Task<IActionResult> CreatePlan([FromBody] CreatePlanCommand command)
    {
        var id = await _mediator.Send(command);
        return CreatedAtAction(nameof(GetPlanById), new { id }, new { Id = id });
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdatePlan(Guid id, [FromBody] UpdatePlanCommand command)
    {
        if (id != command.Id) return BadRequest("ID mismatch");
        await _mediator.Send(command);
        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeletePlan(Guid id)
    {
        await _mediator.Send(new DeletePlanCommand(id));
        return NoContent();
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetPlanById(Guid id)
    {
        var query = new GetAdminPlanByIdQuery(id);
        var result = await _mediator.Send(query);
        return Ok(result);
    }
}