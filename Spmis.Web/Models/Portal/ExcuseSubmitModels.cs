using System.ComponentModel.DataAnnotations;

namespace Spmis.Web.Models.Portal;

public class ExcuseSubmitViewModel
{
    [Required(ErrorMessage = "Guardian name is required.")]
    [StringLength(120)]
    [Display(Name = "Guardian name")]
    public string? GuardianName { get; set; }

    [Required(ErrorMessage = "Relationship is required.")]
    [StringLength(40)]
    [Display(Name = "Relationship to the student")]
    public string? GuardianRelationship { get; set; }

    [Required(ErrorMessage = "The signatory name is required.")]
    [StringLength(120)]
    [Display(Name = "Name of the person who signed the letter")]
    public string? SignatoryName { get; set; }

    [Required(ErrorMessage = "Choose the first date.")]
    [Display(Name = "Absent from")]
    public DateOnly? DateFrom { get; set; }

    [Required(ErrorMessage = "Choose the last date.")]
    [Display(Name = "Absent until")]
    public DateOnly? DateTo { get; set; }

    [Required(ErrorMessage = "Reason is required.")]
    [StringLength(500)]
    public string? Reason { get; set; }

    [Display(Name = "Proof (PDF, JPG or PNG, up to 5 MB)")]
    public IFormFile? File { get; set; }
}

public class MyLetterDecision
{
    public string SubjectCode { get; set; } = "";
    public string Status { get; set; } = "";
    public string? DecisionReason { get; set; }
    public DateTime? DecidedAt { get; set; }
}

public class MyLetterItem
{
    public int LetterId { get; set; }
    public DateTime SubmittedAt { get; set; }
    public DateOnly DateFrom { get; set; }
    public DateOnly DateTo { get; set; }
    public string Reason { get; set; } = "";
    public string FileName { get; set; } = "";
    public List<MyLetterDecision> Decisions { get; set; } = new();
}

public record LetterResult(bool Success, string? Field, string? Error);