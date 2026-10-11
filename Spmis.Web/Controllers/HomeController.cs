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
        if (User.IsInRole(RoleNames.Administrator))
            return RedirectToAction("Index", "Admin");
        if (User.IsInRole(RoleNames.ProgramHead))
            return RedirectToAction("Index", "ProgramHead");
        if (User.IsInRole(RoleNames.Faculty))
            return RedirectToAction("Index", "Faculty");
        if (User.IsInRole(RoleNames.Student))
            return RedirectToAction("Index", "Student");

        return RedirectToAction("AccessDenied", "Account");
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