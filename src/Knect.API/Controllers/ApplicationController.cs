using Knect.Application.Applications.DTOs;
using Knect.Application.Applications.Interfaces;
using Knect.Application.Authentication.Exceptions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Knect.API.Controllers;

[ApiController]
[Route("api/v1")]
public sealed class ApplicationController : ControllerBase
{
    private readonly IApplicationService _applicationService;

    public ApplicationController(IApplicationService applicationService)
    {
        _applicationService = applicationService;
    }

    [Authorize(Roles = "JobSeeker")]
    [HttpPost("jobs/{jobId:guid}/applications")]
    public async Task<IActionResult> Apply(Guid jobId, [FromBody] ApplyToJobRequest request, CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        var application = await _applicationService.ApplyAsync(userId, jobId, request, cancellationToken);
        return Ok(application);
    }

    [Authorize(Roles = "JobSeeker")]
    [HttpGet("users/me/applications")]
    public async Task<IActionResult> GetMyApplications([FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken cancellationToken = default)
    {
        var userId = GetCurrentUserId();
        var result = await _applicationService.GetMyApplicationsAsync(userId, page, pageSize, cancellationToken);
        return Ok(result);
    }

    [Authorize(Roles = "JobSeeker")]
    [HttpGet("users/me/applications/{applicationId:guid}")]
    public async Task<IActionResult> GetMyApplication(Guid applicationId, CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        var result = await _applicationService.GetMyApplicationAsync(userId, applicationId, cancellationToken);
        return Ok(result);
    }

    [Authorize(Roles = "JobSeeker")]
    [HttpDelete("users/me/applications/{applicationId:guid}")]
    public async Task<IActionResult> Withdraw(Guid applicationId, CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        var result = await _applicationService.WithdrawAsync(userId, applicationId, cancellationToken);
        return Ok(result);
    }

    [Authorize(Roles = "Employer")]
    [HttpGet("employers/applications")]
    public async Task<IActionResult> GetEmployerApplications([FromQuery] EmployerApplicationFilter filter, CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        var result = await _applicationService.GetEmployerApplicationsAsync(userId, filter, cancellationToken);
        return Ok(result);
    }

    [Authorize(Roles = "Employer")]
    [HttpGet("employers/applications/{applicationId:guid}")]
    public async Task<IActionResult> GetEmployerApplication(Guid applicationId, CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        var result = await _applicationService.GetEmployerApplicationAsync(userId, applicationId, cancellationToken);
        return Ok(result);
    }

    [Authorize(Roles = "Employer")]
    [HttpPatch("employers/applications/{applicationId:guid}/status")]
    public async Task<IActionResult> UpdateStatus(Guid applicationId, [FromBody] UpdateApplicationStatusRequest request, CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        var result = await _applicationService.UpdateStatusAsync(userId, applicationId, request, cancellationToken);
        return Ok(result);
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
