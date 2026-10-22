using Knect.Application.Authentication.Exceptions;
using Knect.Application.Jobs.DTOs;
using Knect.Application.Jobs.Interfaces;
using Knect.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Knect.API.Controllers;

[ApiController]
[Route("api/v1")]
public sealed class JobController : ControllerBase
{
    private readonly IJobCategoryService _jobCategoryService;
    private readonly IJobExperienceService _jobExperienceService;
    private readonly IJobService _jobService;

    public JobController(IJobCategoryService jobCategoryService, IJobExperienceService jobExperienceService, IJobService jobService)
    {
        _jobCategoryService = jobCategoryService;
        _jobExperienceService = jobExperienceService;
        _jobService = jobService;
    }

    [Authorize(Policy = "AdminOnly")]
    [HttpGet("admin/job-categories")]
    public async Task<IActionResult> GetCategories(CancellationToken cancellationToken)
    {
        var result = await _jobCategoryService.GetCategoriesAsync(cancellationToken);
        return Ok(result);
    }

    [Authorize(Policy = "AdminOnly")]
    [HttpPost("admin/job-categories")]
    public async Task<IActionResult> CreateCategory([FromBody] CreateJobCategoryRequest request, CancellationToken cancellationToken)
    {
        var result = await _jobCategoryService.CreateAsync(request.Name, request.Description, cancellationToken);
        return Ok(result);
    }

    [Authorize(Policy = "AdminOnly")]
    [HttpPut("admin/job-categories/{categoryId:guid}")]
    public async Task<IActionResult> UpdateCategory(Guid categoryId, [FromBody] UpdateJobCategoryRequest request, CancellationToken cancellationToken)
    {
        var result = await _jobCategoryService.UpdateAsync(categoryId, request.Name, request.Description, request.IsActive, cancellationToken);
        return Ok(result);
    }

    [Authorize(Policy = "AdminOnly")]
    [HttpDelete("admin/job-categories/{categoryId:guid}")]
    public async Task<IActionResult> DeleteCategory(Guid categoryId, CancellationToken cancellationToken)
    {
        await _jobCategoryService.DeleteAsync(categoryId, cancellationToken);
        return NoContent();
    }

    [Authorize(Policy = "AdminOnly")]
    [HttpGet("admin/job-experiences")]
    public async Task<IActionResult> GetExperiences(CancellationToken cancellationToken)
    {
        var result = await _jobExperienceService.GetAsync(cancellationToken);
        return Ok(result);
    }

    [Authorize(Policy = "AdminOnly")]
    [HttpPost("admin/job-experiences")]
    public async Task<IActionResult> CreateExperience([FromBody] CreateJobCategoryRequest request, CancellationToken cancellationToken)
    {
        var result = await _jobExperienceService.CreateAsync(request.Name, request.Description, cancellationToken);
        return Ok(result);
    }

    [Authorize(Policy = "AdminOnly")]
    [HttpPut("admin/job-experiences/{experienceId:guid}")]
    public async Task<IActionResult> UpdateExperience(Guid experienceId, [FromBody] UpdateJobCategoryRequest request, CancellationToken cancellationToken)
    {
        var result = await _jobExperienceService.UpdateAsync(experienceId, request.Name, request.Description, request.IsActive, cancellationToken);
        return Ok(result);
    }

    [Authorize(Policy = "AdminOnly")]
    [HttpDelete("admin/job-experiences/{experienceId:guid}")]
    public async Task<IActionResult> DeleteExperience(Guid experienceId, CancellationToken cancellationToken)
    {
        await _jobExperienceService.DeleteAsync(experienceId, cancellationToken);
        return NoContent();
    }

    [Authorize(Roles = "Employer")]
    [HttpGet("employers/jobs")]
    public async Task<IActionResult> GetEmployerJobs(CancellationToken cancellationToken)
    {
        var employerUserId = GetCurrentUserId();
        var jobs = await _jobService.GetEmployerJobsAsync(employerUserId, cancellationToken);
        return Ok(jobs);
    }

    [Authorize(Roles = "Employer")]
    [HttpPost("employers/jobs")]
    public async Task<IActionResult> CreateEmployerJob([FromBody] CreateJobRequest request, CancellationToken cancellationToken)
    {
        var employerUserId = GetCurrentUserId();
        var job = await _jobService.CreateAsync(employerUserId, request, cancellationToken);
        return Ok(job);
    }

    [Authorize(Roles = "Employer")]
    [HttpGet("employers/jobs/{jobId:guid}")]
    public async Task<IActionResult> GetEmployerJob(Guid jobId, CancellationToken cancellationToken)
    {
        var employerUserId = GetCurrentUserId();
        var job = await _jobService.GetEmployerJobAsync(employerUserId, jobId, cancellationToken);
        return Ok(job);
    }

    [Authorize(Roles = "Employer")]
    [HttpPut("employers/jobs/{jobId:guid}")]
    public async Task<IActionResult> UpdateEmployerJob(Guid jobId, [FromBody] UpdateJobRequest request, CancellationToken cancellationToken)
    {
        var employerUserId = GetCurrentUserId();
        var job = await _jobService.UpdateAsync(employerUserId, jobId, request, cancellationToken);
        return Ok(job);
    }

    [Authorize(Roles = "Employer")]
    [HttpPatch("employers/jobs/{jobId:guid}/status")]
    public async Task<IActionResult> UpdateEmployerJobStatus(Guid jobId, [FromBody] JobStatusUpdateRequest request, CancellationToken cancellationToken)
    {
        var employerUserId = GetCurrentUserId();
        var job = await _jobService.UpdateStatusAsync(employerUserId, jobId, request, cancellationToken);
        return Ok(job);
    }

    [Authorize(Roles = "Employer")]
    [HttpDelete("employers/jobs/{jobId:guid}/draft")]
    public async Task<IActionResult> DeleteDraft(Guid jobId, CancellationToken cancellationToken)
    {
        var employerUserId = GetCurrentUserId();
        await _jobService.DeleteDraftAsync(employerUserId, jobId, cancellationToken);
        return NoContent();
    }

    [AllowAnonymous]
    [HttpGet("jobs")]
    public async Task<IActionResult> SearchJobs([FromQuery] JobSearchRequest request, CancellationToken cancellationToken)
    {
        var result = await _jobService.SearchPublicAsync(request, cancellationToken);
        return Ok(result);
    }

    [AllowAnonymous]
    [HttpGet("jobs/{jobId:guid}")]
    public async Task<IActionResult> GetPublicJob(Guid jobId, CancellationToken cancellationToken)
    {
        var job = await _jobService.GetPublicAsync(jobId, cancellationToken);
        return Ok(job);
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

public sealed record CreateJobCategoryRequest(string Name, string? Description);
public sealed record UpdateJobCategoryRequest(string Name, string? Description, bool? IsActive = null);
