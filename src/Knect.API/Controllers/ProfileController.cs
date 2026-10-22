using Knect.Application.Authentication.Exceptions;
using Knect.Application.Profiles.DTOs;
using Knect.Application.Profiles.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Knect.API.Controllers;

[ApiController]
[Route("api/v1")]
[Authorize]
public sealed class ProfileController : ControllerBase
{
    private readonly IUserProfileService _userProfileService;
    private readonly IEmployerProfileService _employerProfileService;

    public ProfileController(IUserProfileService userProfileService, IEmployerProfileService employerProfileService)
    {
        _userProfileService = userProfileService;
        _employerProfileService = employerProfileService;
    }

    [HttpGet("users/me")]
    public async Task<IActionResult> GetCurrentUser(CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        var profile = await _userProfileService.GetCurrentAsync(userId, cancellationToken);
        return Ok(profile);
    }

    [HttpPut("users/me")]
    public async Task<IActionResult> UpsertCurrentUser([FromBody] UpdateUserProfileRequest request, CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        var profile = await _userProfileService.UpsertAsync(userId, request, cancellationToken);
        return Ok(profile);
    }

    [HttpGet("users/{userId:guid}")]
    public async Task<IActionResult> GetUserById(Guid userId, CancellationToken cancellationToken)
    {
        var currentUserId = GetCurrentUserId();
        var profile = await _userProfileService.GetPublicAsync(userId, currentUserId, cancellationToken);
        return Ok(profile);
    }

    [HttpGet("employers/me")]
    public async Task<IActionResult> GetCurrentEmployer(CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        var profile = await _employerProfileService.GetCurrentAsync(userId, cancellationToken);
        return Ok(profile);
    }

    [HttpPut("employers/me")]
    public async Task<IActionResult> UpsertCurrentEmployer([FromBody] UpdateEmployerProfileRequest request, CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        var profile = await _employerProfileService.UpsertAsync(userId, request, cancellationToken);
        return Ok(profile);
    }

    [HttpGet("employers/{userId:guid}")]
    public async Task<IActionResult> GetEmployerById(Guid userId, CancellationToken cancellationToken)
    {
        var currentUserId = GetCurrentUserId();
        var profile = await _employerProfileService.GetPublicAsync(userId, currentUserId, cancellationToken);
        return Ok(profile);
    }

    private Guid GetCurrentUserId()
    {
        var claim = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        if (string.IsNullOrWhiteSpace(claim) || !Guid.TryParse(claim, out var userId))
        {
            throw new AuthException("User identity is unavailable.", 401);
        }

        return userId;
    }
}
