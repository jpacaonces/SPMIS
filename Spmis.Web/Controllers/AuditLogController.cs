using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Spmis.Web.Data.Auth;
using Spmis.Web.Services;

namespace Spmis.Web.Controllers;

[Authorize(Roles = RoleNames.Administrator)]
public class AuditLogController : Controller
{
    private readonly IAuditLogQueryService _logs;

    public AuditLogController(IAuditLogQueryService logs)
    {
        _logs = logs;
    }

    public async Task<IActionResult> Index(
        DateOnly? dateFrom, DateOnly? dateTo,
        string? actionType, string? role, string? search, int page = 1)
    {
        var model = await _logs.GetPageAsync(
            dateFrom, dateTo, actionType, role, search, page);
        return View(model);
    }
}
