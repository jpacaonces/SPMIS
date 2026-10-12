namespace Spmis.Web.Models.Admin;

public class UserStatusInput
{
    public Guid UserId { get; set; }

    // "deactivate" or "reactivate": what the admin SAW when they clicked.
    public string Action { get; set; } = string.Empty;

    public ReasonInput Reason { get; set; } = new();
}