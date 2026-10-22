using Knect.Application.Authentication.Exceptions;
using Knect.Application.Subscriptions.DTOs;
using Knect.Application.Subscriptions.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Knect.API.Controllers;

[ApiController]
[Route("api/v1")]
public sealed class SubscriptionController : ControllerBase
{
    private readonly ISubscriptionPlanService _planService;
    private readonly IEmployerSubscriptionService _employerSubscriptionService;
    private readonly IPaymentReviewService _paymentReviewService;

    public SubscriptionController(
        ISubscriptionPlanService planService,
        IEmployerSubscriptionService employerSubscriptionService,
        IPaymentReviewService paymentReviewService)
    {
        _planService = planService;
        _employerSubscriptionService = employerSubscriptionService;
        _paymentReviewService = paymentReviewService;
    }

    [Authorize(Policy = "AdminOnly")]
    [HttpGet("admin/subscription-plans")]
    public async Task<IActionResult> GetPlans([FromQuery] bool includeInactive = false, CancellationToken cancellationToken = default)
    {
        var plans = await _planService.GetPlansAsync(includeInactive, cancellationToken);
        return Ok(plans);
    }

    [Authorize(Policy = "AdminOnly")]
    [HttpGet("admin/subscription-plans/{planId:guid}")]
    public async Task<IActionResult> GetPlan(Guid planId, CancellationToken cancellationToken)
    {
        var plan = await _planService.GetPlanAsync(planId, cancellationToken);
        return Ok(plan);
    }

    [Authorize(Policy = "AdminOnly")]
    [HttpPost("admin/subscription-plans")]
    public async Task<IActionResult> CreatePlan([FromBody] CreateSubscriptionPlanRequest request, CancellationToken cancellationToken)
    {
        var plan = await _planService.CreateAsync(request, cancellationToken);
        return Ok(plan);
    }

    [Authorize(Policy = "AdminOnly")]
    [HttpPut("admin/subscription-plans/{planId:guid}")]
    public async Task<IActionResult> UpdatePlan(Guid planId, [FromBody] UpdateSubscriptionPlanRequest request, CancellationToken cancellationToken)
    {
        var plan = await _planService.UpdateAsync(planId, request, cancellationToken);
        return Ok(plan);
    }

    [Authorize(Policy = "AdminOnly")]
    [HttpPatch("admin/subscription-plans/{planId:guid}/active")]
    public async Task<IActionResult> SetPlanActive(Guid planId, [FromBody] TogglePlanRequest request, CancellationToken cancellationToken)
    {
        var plan = await _planService.SetActiveAsync(planId, request.IsActive, cancellationToken);
        return Ok(plan);
    }

    [Authorize(Roles = "Employer")]
    [HttpGet("employers/subscriptions/plans")]
    public async Task<IActionResult> GetAvailablePlans(CancellationToken cancellationToken)
    {
        var plans = await _employerSubscriptionService.GetAvailablePlansAsync(cancellationToken);
        return Ok(plans);
    }

    [Authorize(Roles = "Employer")]
    [HttpGet("employers/subscriptions")]
    public async Task<IActionResult> GetMySubscriptions(CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        var subscriptions = await _employerSubscriptionService.GetMySubscriptionsAsync(userId, cancellationToken);
        return Ok(subscriptions);
    }

    [Authorize(Roles = "Employer")]
    [HttpPost("employers/subscriptions")]
    public async Task<IActionResult> PurchaseSubscription([FromBody] EmployerSubscriptionPurchaseRequest request, CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        var subscription = await _employerSubscriptionService.PurchasePlanAsync(userId, request, cancellationToken);
        return Ok(subscription);
    }

    [Authorize(Roles = "Employer")]
    [HttpGet("employers/payments")]
    public async Task<IActionResult> GetMyPayments(CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        var payments = await _employerSubscriptionService.GetMyPaymentsAsync(userId, cancellationToken);
        return Ok(payments);
    }

    [Authorize(Policy = "AdminOnly")]
    [HttpGet("admin/payments/pending")]
    public async Task<IActionResult> GetPendingPayments([FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken cancellationToken = default)
    {
        var result = await _paymentReviewService.GetPendingPaymentsAsync(page, pageSize, cancellationToken);
        return Ok(result);
    }

    [Authorize(Policy = "AdminOnly")]
    [HttpGet("admin/payments/{paymentId:guid}")]
    public async Task<IActionResult> GetPayment(Guid paymentId, CancellationToken cancellationToken)
    {
        var result = await _paymentReviewService.GetPaymentAsync(paymentId, cancellationToken);
        return Ok(result);
    }

    [Authorize(Policy = "AdminOnly")]
    [HttpPatch("admin/payments/{paymentId:guid}/review")]
    public async Task<IActionResult> ReviewPayment(Guid paymentId, [FromBody] ReviewPaymentRequest request, CancellationToken cancellationToken)
    {
        var adminUserId = GetCurrentUserId();
        var result = await _paymentReviewService.ReviewPaymentAsync(adminUserId, paymentId, request, cancellationToken);
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

public sealed record TogglePlanRequest(bool IsActive);
