namespace Spmis.Web.Models.Admin;

// What the confirmation page shows.
public class UserStatusViewModel
{
    public Guid UserId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public List<string> Roles { get; set; } = new();
    public bool IsActive { get; set; }

    // Things the admin should know before confirming.
    public List<string> Warnings { get; set; } = new();

    // If set, the change is not allowed and this says why.
    public string? Blocker { get; set; }

    // Echoed back when the server rejects the reason.
    public ReasonInput Reason { get; set; } = new();
}