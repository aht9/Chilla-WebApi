using Chilla.Application.Features.Users.Dtos;
using Chilla.Application.Features.Users.Queries;
using Chilla.Application.Services.Interface;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Chilla.WebApi.Controllers;

[ApiController]
[Route("api/users")]
[Authorize] 
public class UsersController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ICurrentUserService _currentUserService;

    public UsersController(IMediator mediator, ICurrentUserService currentUserService)
    {
        _mediator = mediator;
        _currentUserService = currentUserService;
    }

    /// <summary>
    /// دریافت اطلاعات پروفایل کاربر جاری (getProfile)
    /// </summary>
    [HttpGet("me")]
    [ProducesResponseType(typeof(UserProfileDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMyProfile()
    {
        var userId = _currentUserService.UserId;
        if (!userId.HasValue) return Unauthorized();

        var query = new GetUserProfileQuery(userId.Value);
        var result = await _mediator.Send(query);

        return Ok(result);
    }
}