using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;
using Spmis.Web.Models;

namespace Spmis.Web.Models.ClassRecord;

public class MeetingListItem
{
    public int Id { get; set; }
    public DateOnly Date { get; set; }
    public string PeriodName { get; set; } = "";
    public string? Topic { get; set; }
    public string? LinkedAssessment { get; set; }
    public bool IsActive { get; set; }
    public int RecordCount { get; set; }
}

public class MeetingsViewModel
{
    public int ClassOfferingId { get; set; }
    public string Header { get; set; } = "";
    public string AttendanceMode { get; set; } = "";
    public List<MeetingListItem> Items { get; set; } = new();
}

public class MeetingCreateViewModel
{
    public int ClassOfferingId { get; set; }
    public string Header { get; set; } = "";
    public string AttendanceMode { get; set; } = "";

    [Required(ErrorMessage = "Choose a date.")]
    [Display(Name = "Meeting date")]
    public DateOnly? MeetingDate { get; set; }

    [StringLength(150)]
    public string? Topic { get; set; }

    [Display(Name = "Linked assessment")]
    public int? LinkedAssessmentId { get; set; }

    public List<SelectListItem> Assessments { get; set; } = new();
}

public class MeetingStatusViewModel
{
    public int MeetingId { get; set; }
    public int ClassOfferingId { get; set; }
    public DateOnly Date { get; set; }
    public string? Topic { get; set; }
    public bool IsActive { get; set; }
    public bool Activate { get; set; }
    public int RecordCount { get; set; }
    public ReasonInput Reason { get; set; } = new();
}

public record MeetingResult(bool Success, string? Field, string? Error);