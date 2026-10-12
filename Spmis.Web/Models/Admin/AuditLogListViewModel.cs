namespace Spmis.Web.Models.Admin;

public class AuditLogListViewModel
{
    public List<AuditLogItem> Items { get; set; } = new();

    // Filters, echoed back into the form.
    public DateOnly? DateFrom { get; set; }
    public DateOnly? DateTo { get; set; }
    public string? ActionType { get; set; }
    public string? Role { get; set; }
    public string? Search { get; set; }

    // Contents of the Action dropdown (the types that really exist).
    public List<string> ActionTypes { get; set; } = new();

    public int Page { get; set; } = 1;
    public int TotalPages { get; set; } = 1;
    public int TotalCount { get; set; }

    public bool HasPrevious => Page > 1;
    public bool HasNext => Page < TotalPages;
}