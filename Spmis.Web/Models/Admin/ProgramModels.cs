using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Spmis.Web.Models.Admin;

public class ProgramListItem
{
    public int Id { get; set; }
    public string Code { get; set; } = "";
    public string Name { get; set; } = "";
    public string Department { get; set; } = "";
    public bool IsActive { get; set; }
    public int ActiveStudents { get; set; }
    public int ActiveSections { get; set; }
}

public class ProgramCreateViewModel
{
    [Required, StringLength(10, MinimumLength = 2)]
    [Display(Name = "Code (e.g. BSIT)")]
    public string Code { get; set; } = "";

    [Required, StringLength(100)]
    [Display(Name = "Program name")]
    public string Name { get; set; } = "";

    [Required]
    [Display(Name = "Department")]
    public int? DepartmentId { get; set; }

    public IEnumerable<SelectListItem> Departments { get; set; } = [];
}

public class ProgramStatusViewModel
{
    public int ProgramId { get; set; }
    public string Code { get; set; } = "";
    public string Name { get; set; } = "";
    public bool IsActive { get; set; }
    public bool Activate { get; set; }          // what the admin wants to do
    public string? Blocker { get; set; }
    public ReasonInput Reason { get; set; } = new();
}
