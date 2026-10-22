using System.Security.Claims;
using Knect.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;

namespace Knect.API.Authorization;

public sealed class CurrentSessionAuthorizationHandler : AuthorizationHandler<CurrentSessionRequirement>
{
    private readonly KnectDbContext _dbContext;

    public CurrentSessionAuthorizationHandler(KnectDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context, CurrentSessionRequirement requirement)
    {
        if (!context.User.IsInRole("Admin"))
        {
            context.Fail();
            return;
        }

        var userIdClaim = context.User.FindFirstValue(ClaimTypes.NameIdentifier) ?? context.User.FindFirstValue("sub");
        var sessionIdClaim = context.User.FindFirstValue("sid");

        if (string.IsNullOrWhiteSpace(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId) || string.IsNullOrWhiteSpace(sessionIdClaim))
        {
            context.Fail();
            return;
        }

        var isCurrent = await _dbContext.AdminSessions.AnyAsync(x =>
            x.UserId == userId &&
            x.SessionIdentifier == sessionIdClaim &&
            x.RevokedAt == null &&
            x.ExpiresAt > DateTime.UtcNow,
            CancellationToken.None);

        if (isCurrent)
        {
            context.Succeed(requirement);
            return;
        }

        context.Fail();
    }
}
