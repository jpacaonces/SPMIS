namespace Spmis.Web.Models.Admin;

public class UserListViewModel
{
    public List<UserListItem> Items { get; set; } = new();

    // What the admin typed or picked, echoed back into the filter form.
    public string? Search { get; set; }
    public string? Role { get; set; }
    public string? Status { get; set; }

    public int Page { get; set; } = 1;
    public int TotalPages { get; set; } = 1;
    public int TotalCount { get; set; }

    public bool HasPrevious => Page > 1;
    public bool HasNext => Page < TotalPages;
}