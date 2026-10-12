using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Spmis.Web.Data.Auth;

// Runs before every controller action. A signed-in person whose account is flagged
// MustChangePassword is sent to the change-password page, whatever they asked for.
// The flag is read from the database each time, so an admin reset takes effect at once.
public class MustChangePasswordFilter : IAsyncActionFilter
{
    private readonly UserManager<ApplicationUser> _users;

    public MustChangePasswordFilter(UserManager<ApplicationUser> users) => _users = users;

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var principal = context.HttpContext.User;

        if (principal.Identity?.IsAuthenticated == true)
        {
            // The Account controller (change password, logout, ...) must stay reachable.
            var controller = context.RouteData.Values["controller"] as string;
            var isAccount = string.Equals(controller, "Account", StringComparison.OrdinalIgnoreCase);

            if (!isAccount)
            {
                var user = await _users.GetUserAsync(principal);
                if (user is { MustChangePassword: true })
                {
                    context.Result = new RedirectToActionResult("ChangePassword", "Account", null);
                    return;
                }
            }
        }

        await next();
    }
}