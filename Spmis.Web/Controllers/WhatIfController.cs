using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Spmis.Web.Data.Auth;
using Spmis.Web.Services;

namespace Spmis.Web.Controllers;

[Authorize(Roles = RoleNames.Student)]
public class WhatIfController : Controller
{
    private readonly IWhatIfService _service;

    public WhatIfController(IWhatIfService service) => _service = service;

    private Guid CurrentUserId() =>
        Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    // GET /WhatIf/Show/123   (123 = enrollment id)
    [HttpGet]
    public async Task<IActionResult> Show(int id)
    {
        var vm = await _service.BuildAsync(CurrentUserId(), id, null);
        return vm is null ? NotFound() : View(vm);
    }

    // Read-only calculation, but it is still a POST form, so it carries the anti-forgery token.
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Show(int id, Dictionary<string, string?>? assumed)
    {
        var vm = await _service.BuildAsync(CurrentUserId(), id, assumed ?? new Dictionary<string, string?>());
        return vm is null ? NotFound() : View(vm);
    }
}