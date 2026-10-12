using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;
using Spmis.Web.Models;
namespace Spmis.Web.Models.ClassRecord;

public class AssessmentListItem
{
    public int Id { get; set; }
    public string PeriodName { get; set; } = "";
    public int PeriodOrder { get; set; }
    public string ComponentName { get; set; } = "";
    public string Title { get; set; } = "";
    public decimal MaxScore { get; set; }
    public DateTime? DueDate { get; set; }
    public decimal PassingPercent { get; set; }
    public bool IsRequired { get; set; }
    public bool IsActive { get; set; }
    public int ScoreRows { get; set; }
}

public class AssessmentsViewModel
{
    public int ClassOfferingId { get; set; }
    public string Header { get; set; } = "";
    public string AttendanceMode { get; set; } = "";
    public List<AssessmentListItem> Items { get; set; } = new();
}

public class AssessmentCreateViewModel
{
    public int ClassOfferingId { get; set; }
    public string Header { get; set; } = "";

    [Required(ErrorMessage = "Choose a component.")]
    [Display(Name = "Component")]
    public int? ComponentId { get; set; }

    [Required(ErrorMessage = "Choose a grading period.")]
    [Display(Name = "Grading period")]
    public int? PeriodId { get; set; }

    [Required(ErrorMessage = "Title is required.")]
    [StringLength(150)]
    public string? Title { get; set; }

    [Required(ErrorMessage = "Maximum score is required.")]
    [Display(Name = "Maximum score")]
    public decimal? MaxScore { get; set; }

    [Display(Name = "Due date (optional)")]
    public DateTime? DueDate { get; set; }

    [Required]
    [Display(Name = "Passing percent")]
    public decimal? PassingPercent { get; set; } = 75;

    [Display(Name = "Required")]
    public bool IsRequired { get; set; } = true;

    public List<SelectListItem> Components { get; set; } = new();
    public List<SelectListItem> Periods { get; set; } = new();
}

public record AssessmentResult(bool Success, string? Field, string? Error);

public class AssessmentEditViewModel
{
    public int AssessmentId { get; set; }

    // Display only (filled by the server, never trusted from the form)
    public int ClassOfferingId { get; set; }
    public string Header { get; set; } = "";
    public string ComponentName { get; set; } = "";
    public string PeriodName { get; set; } = "";
    public int ScoreRows { get; set; }

    [Required(ErrorMessage = "Title is required.")]
    [StringLength(150)]
    public string? Title { get; set; }

    [Required(ErrorMessage = "Maximum score is required.")]
    [Display(Name = "Maximum score")]
    public decimal? MaxScore { get; set; }

    [Display(Name = "Due date (optional)")]
    public DateTime? DueDate { get; set; }

    [Required]
    [Display(Name = "Passing percent")]
    public decimal? PassingPercent { get; set; }

    [Display(Name = "Required")]
    public bool IsRequired { get; set; }

    public ReasonInput Reason { get; set; } = new();
}

public class AssessmentStatusViewModel
{
    public int AssessmentId { get; set; }
    public int ClassOfferingId { get; set; }
    public string Title { get; set; } = "";
    public string ComponentName { get; set; } = "";
    public string PeriodName { get; set; } = "";
    public int ScoreRows { get; set; }
    public bool IsActive { get; set; }
    public bool Activate { get; set; }   // what the faculty member is asking for
    public ReasonInput Reason { get; set; } = new();
}