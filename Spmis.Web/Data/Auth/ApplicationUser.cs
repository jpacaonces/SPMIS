using Microsoft.AspNetCore.Identity;

namespace Spmis.Web.Data.Auth;

// Our login account. Extends Identity's built-in user with the
// extra columns that exist in the users table.
public class ApplicationUser : IdentityUser<Guid>
{
    public string FirstName { get; set; } = null!;
    public string? MiddleName { get; set; }
    public string LastName { get; set; } = null!;

    // Accounts are never deleted. Inactive accounts cannot sign in.
    public bool IsActive { get; set; } = true;
}

public class ApplicationRole : IdentityRole<Guid>
{
}