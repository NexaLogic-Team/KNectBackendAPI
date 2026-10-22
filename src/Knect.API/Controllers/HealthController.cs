using Knect.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc;

namespace Knect.API.Controllers;

[ApiController]
[Route("api/v1")]
public class HealthController : ControllerBase
{
    private readonly KnectDbContext _dbContext;

    public HealthController(KnectDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    [HttpGet("health/live")]
    public IActionResult Live() => Ok(new { status = "ok" });

    [HttpGet("health/ready")]
    public async Task<IActionResult> Ready(CancellationToken cancellationToken)
    {
        try
        {
            var canConnect = await _dbContext.Database.CanConnectAsync(cancellationToken);
            return canConnect ? Ok(new { status = "ready" }) : StatusCode(503, new { status = "not-ready" });
        }
        catch
        {
            return StatusCode(503, new { status = "not-ready" });
        }
    }
}
