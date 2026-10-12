using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Spmis.Web.Data.Auth;
using Spmis.Web.Services;

namespace Spmis.Web.Controllers;

[Authorize(Roles = RoleNames.ProgramHead)]
public class AnalyticsController : Controller
{
    private readonly IProgramAnalyticsService _service;

    public AnalyticsController(IProgramAnalyticsService service) => _service = service;

    public async Task<IActionResult> Index()
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        return View(await _service.GetDashboardAsync(userId));
    }

    // GET /Analytics/Detail/123   (123 = class offering id)
    public async Task<IActionResult> Detail(int id)
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var vm = await _service.GetClassAsync(userId, id);
        return vm is null ? NotFound() : View(vm);
    }
}