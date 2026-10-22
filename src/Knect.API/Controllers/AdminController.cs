using Knect.Application.Admin.DTOs;
using Knect.Application.Admin.Interfaces;
using Knect.Application.Authentication.Exceptions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Knect.API.Controllers;

[ApiController]
[Route("api/v1")]
[Authorize(Policy = "AdminOnly")]
public sealed class AdminController : ControllerBase
{
    private readonly IAdminDashboardService _dashboardService;
    private readonly IAdminUserService _userService;
    private readonly IAdminEmployerService _employerService;
    private readonly IAdminAuditService _auditService;
    private readonly IAdminReportService _reportService;

    public AdminController(
        IAdminDashboardService dashboardService,
        IAdminUserService userService,
        IAdminEmployerService employerService,
        IAdminAuditService auditService,
        IAdminReportService reportService)
    {
        _dashboardService = dashboardService;
        _userService = userService;
        _employerService = employerService;
        _auditService = auditService;
        _reportService = reportService;
    }

    [HttpGet("admin/dashboard/summary")]
    public async Task<IActionResult> GetDashboardSummary(CancellationToken cancellationToken)
    {
        var summary = await _dashboardService.GetSummaryAsync(cancellationToken);
        return Ok(summary);
    }

    [HttpGet("admin/dashboard/recent-activity")]
    public async Task<IActionResult> GetRecentActivity([FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken cancellationToken = default)
    {
        var registrations = await _dashboardService.GetRecentRegistrationsAsync(page, pageSize, cancellationToken);
        var jobs = await _dashboardService.GetRecentJobsAsync(page, pageSize, cancellationToken);
        var payments = await _dashboardService.GetRecentPaymentsAsync(page, pageSize, cancellationToken);
        return Ok(new { registrations, jobs, payments });
    }

    [HttpGet("admin/users")]
    public async Task<IActionResult> ListUsers([FromQuery] string? search, [FromQuery] string? role, [FromQuery] string? status, [FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken cancellationToken = default)
    {
        var result = await _userService.ListUsersAsync(search, role, status, page, pageSize, cancellationToken);
        return Ok(result);
    }

    [HttpGet("admin/users/{userId:guid}")]
    public async Task<IActionResult> GetUser(Guid userId, CancellationToken cancellationToken)
    {
        var result = await _userService.GetUserAsync(userId, cancellationToken);
        return Ok(result);
    }

    [HttpPatch("admin/users/{userId:guid}/status")]
    public async Task<IActionResult> UpdateUserStatus(Guid userId, [FromBody] UserStatusUpdateRequest request, CancellationToken cancellationToken)
    {
        var adminUserId = GetCurrentUserId();
        var result = await _userService.UpdateUserStatusAsync(adminUserId, userId, request, cancellationToken);
        return Ok(result);
    }

    [HttpGet("admin/employers")]
    public async Task<IActionResult> ListEmployers([FromQuery] string? search, [FromQuery] string? status, [FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken cancellationToken = default)
    {
        var result = await _employerService.ListEmployersAsync(search, status, page, pageSize, cancellationToken);
        return Ok(result);
    }

    [HttpGet("admin/employers/{userId:guid}")]
    public async Task<IActionResult> GetEmployer(Guid userId, CancellationToken cancellationToken)
    {
        var result = await _employerService.GetEmployerAsync(userId, cancellationToken);
        return Ok(result);
    }

    [HttpPatch("admin/employers/{userId:guid}/status")]
    public async Task<IActionResult> UpdateEmployerStatus(Guid userId, [FromBody] EmployerStatusUpdateRequest request, CancellationToken cancellationToken)
    {
        var adminUserId = GetCurrentUserId();
        var result = await _employerService.UpdateEmployerStatusAsync(adminUserId, userId, request, cancellationToken);
        return Ok(result);
    }

    [HttpGet("admin/audit-logs")]
    public async Task<IActionResult> ListAuditLogs([FromQuery] Guid? userId, [FromQuery] string? eventType, [FromQuery] string? entityType, [FromQuery] Guid? entityId, [FromQuery] DateTime? from, [FromQuery] DateTime? to, [FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken cancellationToken = default)
    {
        var filter = new AuditLogFilter(userId, eventType, entityType, entityId, from, to, page, pageSize);
        var result = await _auditService.ListAsync(filter, cancellationToken);
        return Ok(result);
    }

    [HttpGet("admin/audit-logs/{auditLogId:guid}")]
    public async Task<IActionResult> GetAuditLog(Guid auditLogId, CancellationToken cancellationToken)
    {
        var result = await _auditService.GetByIdAsync(auditLogId, cancellationToken);
        return Ok(result);
    }

    [HttpGet("admin/reports/users")]
    public async Task<IActionResult> GetUsersReport([FromQuery] DateTime? from, [FromQuery] DateTime? to, [FromQuery] string? status, [FromQuery] string? category, [FromQuery] Guid? employerId, [FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken cancellationToken = default)
    {
        var filter = new ReportFilter(from, to, status, category, employerId, page, pageSize);
        var result = await _reportService.GetUsersReportAsync(filter, cancellationToken);
        return Ok(result);
    }

    [HttpGet("admin/reports/jobs")]
    public async Task<IActionResult> GetJobsReport([FromQuery] DateTime? from, [FromQuery] DateTime? to, [FromQuery] string? status, [FromQuery] string? category, [FromQuery] Guid? employerId, [FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken cancellationToken = default)
    {
        var filter = new ReportFilter(from, to, status, category, employerId, page, pageSize);
        var result = await _reportService.GetJobsReportAsync(filter, cancellationToken);
        return Ok(result);
    }

    [HttpGet("admin/reports/applications")]
    public async Task<IActionResult> GetApplicationsReport([FromQuery] DateTime? from, [FromQuery] DateTime? to, [FromQuery] string? status, [FromQuery] string? category, [FromQuery] Guid? employerId, [FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken cancellationToken = default)
    {
        var filter = new ReportFilter(from, to, status, category, employerId, page, pageSize);
        var result = await _reportService.GetApplicationsReportAsync(filter, cancellationToken);
        return Ok(result);
    }

    [HttpGet("admin/reports/subscriptions")]
    public async Task<IActionResult> GetSubscriptionsReport([FromQuery] DateTime? from, [FromQuery] DateTime? to, [FromQuery] string? status, [FromQuery] string? category, [FromQuery] Guid? employerId, [FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken cancellationToken = default)
    {
        var filter = new ReportFilter(from, to, status, category, employerId, page, pageSize);
        var result = await _reportService.GetSubscriptionsReportAsync(filter, cancellationToken);
        return Ok(result);
    }

    [HttpGet("admin/reports/payments")]
    public async Task<IActionResult> GetPaymentsReport([FromQuery] DateTime? from, [FromQuery] DateTime? to, [FromQuery] string? status, [FromQuery] string? category, [FromQuery] Guid? employerId, [FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken cancellationToken = default)
    {
        var filter = new ReportFilter(from, to, status, category, employerId, page, pageSize);
        var result = await _reportService.GetPaymentsReportAsync(filter, cancellationToken);
        return Ok(result);
    }

    [HttpGet("admin/reports/{reportType}/export")]
    public async Task<IActionResult> ExportReport(string reportType, [FromQuery] string format = "csv", [FromQuery] DateTime? from = null, [FromQuery] DateTime? to = null, [FromQuery] string? status = null, [FromQuery] string? category = null, [FromQuery] Guid? employerId = null, CancellationToken cancellationToken = default)
    {
        if (!string.Equals(format, "csv", StringComparison.OrdinalIgnoreCase) && !string.Equals(format, "xlsx", StringComparison.OrdinalIgnoreCase))
        {
            return BadRequest("Unsupported export format. Use csv or xlsx.");
        }

        var filter = new ReportFilter(from, to, status, category, employerId, 1, 5000);
        IReadOnlyList<AdminReportRowDto> data = reportType.ToLowerInvariant() switch
        {
            "users" => await _reportService.GetUsersReportAsync(filter, cancellationToken),
            "jobs" => await _reportService.GetJobsReportAsync(filter, cancellationToken),
            "applications" => await _reportService.GetApplicationsReportAsync(filter, cancellationToken),
            "subscriptions" => await _reportService.GetSubscriptionsReportAsync(filter, cancellationToken),
            "payments" => await _reportService.GetPaymentsReportAsync(filter, cancellationToken),
            _ => throw new AuthException("Unsupported report type.", 400)
        };

        if (string.Equals(format, "csv", StringComparison.OrdinalIgnoreCase))
        {
            var csv = string.Join(Environment.NewLine, data.Select(row => $"{EscapeCsv(row.Label)},{row.Count},{row.Amount ?? 0m},{EscapeCsv(row.Currency ?? "")},{row.Date:yyyy-MM-dd}"));
            var fileBytes = System.Text.Encoding.UTF8.GetBytes("Label,Count,Amount,Currency,Date" + Environment.NewLine + csv + Environment.NewLine);
            return File(fileBytes, "text/csv", $"{reportType}-export.csv");
        }

        var workbook = new ClosedXML.Excel.XLWorkbook();
        var worksheet = workbook.Worksheets.Add("Report");
        worksheet.Cell("A1").Value = "Label";
        worksheet.Cell("B1").Value = "Count";
        worksheet.Cell("C1").Value = "Amount";
        worksheet.Cell("D1").Value = "Currency";
        worksheet.Cell("E1").Value = "Date";

        var rowIndex = 2;
        foreach (var item in data)
        {
            worksheet.Cell(rowIndex, 1).Value = SanitizeCell(item.Label);
            worksheet.Cell(rowIndex, 2).Value = item.Count;
            worksheet.Cell(rowIndex, 3).Value = item.Amount ?? 0m;
            worksheet.Cell(rowIndex, 4).Value = SanitizeCell(item.Currency ?? string.Empty);
            worksheet.Cell(rowIndex, 5).Value = item.Date?.ToString("yyyy-MM-dd");
            rowIndex++;
        }

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        var bytes = stream.ToArray();
        return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"{reportType}-export.xlsx");
    }

    private static string EscapeCsv(string value) => value.Replace("\"", "\"\"");

    private static string SanitizeCell(string value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return string.Empty;
        }

        return value.Replace("=", "'=").Replace("+", "'+").Replace("-", "'-").Replace("@", "'@");
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
