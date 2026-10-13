using System.Diagnostics;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Spmis.Web.Data.Auth;
using Spmis.Web.Models;

namespace Spmis.Web.Controllers;

public class HomeController : Controller
{
    public IActionResult Index()
    {
        // The tiles page decides what to show per role. A signed-in user
        // with no known role is still turned away.
        var hasKnownRole =
            User.IsInRole(RoleNames.Administrator) ||
            User.IsInRole(RoleNames.ProgramHead) ||
            User.IsInRole(RoleNames.Faculty) ||
            User.IsInRole(RoleNames.Student);

        if (!hasKnownRole)
            return RedirectToAction("AccessDenied", "Account");

        return View();
    }

    public IActionResult Privacy()
    {
        return View();
    }

    // Shown when an unhandled exception occurs (non-Development only).
    // The framework has already logged the exception before we get here.
    [AllowAnonymous]
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel
        {
            RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier
        });
    }

    // Shown for 404, 403 and similar status codes, for everyone.
    // (Not named "StatusCode": that would hide a method every controller inherits.)
    [AllowAnonymous]
    public IActionResult HttpStatus(int code)
    {
        Response.StatusCode = code;
        return View(code);
    }

    
}