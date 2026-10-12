namespace Spmis.Web.Models.Admin;

public class UserListItem
{
    public Guid Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public List<string> Roles { get; set; } = new();

    // Student number or employee number, when the account has one.
    public string? IdentifierNo { get; set; }

    public bool IsActive { get; set; }
}
