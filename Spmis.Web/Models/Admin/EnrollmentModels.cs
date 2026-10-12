using System.ComponentModel.DataAnnotations;

namespace Spmis.Web.Models.Admin;

public class RosterItem
{
    public int EnrollmentId { get; set; }
    public string StudentNo { get; set; } = "";
    public string Name { get; set; } = "";
    public string Status { get; set; } = "";
}

public class EnrollInput
{
    [Required, StringLength(20)]
    [Display(Name = "Student number")]
    public string StudentNo { get; set; } = "";
}

public class RosterViewModel
{
    public int ClassOfferingId { get; set; }
    public string ClassLabel { get; set; } = "";
    public string? TermLabel { get; set; }
    public string Faculty { get; set; } = "";
    public bool CanEdit { get; set; }
    public string? CannotEditReason { get; set; }
    public int EnrolledCount { get; set; }
    public List<RosterItem> Items { get; set; } = [];
    public EnrollInput Enroll { get; set; } = new();
}

public class EnrollmentStatusViewModel
{
    public int EnrollmentId { get; set; }
    public int ClassOfferingId { get; set; }
    public string StudentLabel { get; set; } = "";
    public string ClassLabel { get; set; } = "";
    public string CurrentStatus { get; set; } = "";
    public string NewStatus { get; set; } = "";     // "Dropped" or "Enrolled"
    public string? Blocker { get; set; }
    public ReasonInput Reason { get; set; } = new();
}
public class BulkEnrollViewModel
{
    public string? TermLabel { get; set; }

    [Required, Display(Name = "Section")]
    public int? SectionId { get; set; }

    [Required, Display(Name = "Student numbers (one per line, or separated by commas)")]
    public string StudentNumbers { get; set; } = "";

    public IEnumerable<Microsoft.AspNetCore.Mvc.Rendering.SelectListItem> Sections { get; set; } = [];
}
