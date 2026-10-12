using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Spmis.Web.Models.Admin;

public class SectionListItem
{
    public int Id { get; set; }
    public string Code { get; set; } = "";
    public string Program { get; set; } = "";
    public int YearLevel { get; set; }
    public int Semester { get; set; }
    public string Shift { get; set; } = "";
    public bool IsActive { get; set; }
    public int ClassesThisTerm { get; set; }
}

public class SectionCreateViewModel
{
    [Required, Display(Name = "Program")]
    public int? ProgramId { get; set; }

    [Required, Range(1, 4), Display(Name = "Year level")]
    public int? YearLevel { get; set; }

    [Required, Range(1, 2)]
    public int? Semester { get; set; }

    [Required, Display(Name = "Shift")]
    public int? ShiftId { get; set; }

    [Required, Range(1, 20), Display(Name = "Section number")]
    public int? SectionNo { get; set; } = 1;

    public IEnumerable<SelectListItem> Programs { get; set; } = [];
    public IEnumerable<SelectListItem> Shifts { get; set; } = [];
}

public class SectionStatusViewModel
{
    public int SectionId { get; set; }
    public string Code { get; set; } = "";
    public bool IsActive { get; set; }
    public bool Activate { get; set; }
    public string? Blocker { get; set; }
    public ReasonInput Reason { get; set; } = new();
}