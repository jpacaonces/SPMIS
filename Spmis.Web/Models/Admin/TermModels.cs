using System.ComponentModel.DataAnnotations;

namespace Spmis.Web.Models.Admin;

public class TermListItem
{
    public int Id { get; set; }
    public string SchoolYear { get; set; } = "";
    public int Semester { get; set; }
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }
    public bool IsCurrent { get; set; }
    public int ClassCount { get; set; }
}

public class TermCreateViewModel
{
    [Required, StringLength(9)]
    [Display(Name = "School year (e.g. 2026-2027)")]
    public string SchoolYear { get; set; } = "";

    [Required, Range(1, 2)]
    public int Semester { get; set; } = 1;

    [Required, DataType(DataType.Date)]
    [Display(Name = "Start date")]
    public DateOnly? StartDate { get; set; }

    [Required, DataType(DataType.Date)]
    [Display(Name = "End date")]
    public DateOnly? EndDate { get; set; }
}

public class TermCurrentViewModel
{
    public int TermId { get; set; }
    public string Label { get; set; } = "";
    public string? CurrentLabel { get; set; }
    public ReasonInput Reason { get; set; } = new();
}