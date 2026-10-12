using System.ComponentModel.DataAnnotations;

namespace Spmis.Web.Models.Admin;

public class SubjectListItem
{
    public int Id { get; set; }
    public string Code { get; set; } = "";
    public string Title { get; set; } = "";
    public int LectureUnits { get; set; }
    public int LabUnits { get; set; }
    public bool IsActive { get; set; }
    public int ActiveClassesThisTerm { get; set; }
}

public class SubjectCreateViewModel
{
    [Required, StringLength(20, MinimumLength = 2)]
    [Display(Name = "Code (e.g. CC113-1)")]
    public string Code { get; set; } = "";

    [Required, StringLength(150)]
    public string Title { get; set; } = "";

    [Range(0, 9)]
    [Display(Name = "Lecture units")]
    public int LectureUnits { get; set; } = 3;

    [Range(0, 9)]
    [Display(Name = "Lab units (0 = lecture only)")]
    public int LabUnits { get; set; } = 0;
}

public class SubjectStatusViewModel
{
    public int SubjectId { get; set; }
    public string Code { get; set; } = "";
    public string Title { get; set; } = "";
    public bool IsActive { get; set; }
    public bool Activate { get; set; }
    public string? Blocker { get; set; }
    public ReasonInput Reason { get; set; } = new();
}