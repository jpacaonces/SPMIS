using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations.Schema;
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
    // True = the person must choose a new password before using the system.
    [Column("must_change_password")]
    public bool MustChangePassword { get; set; }
}

public class ApplicationRole : IdentityRole<Guid>
{
}