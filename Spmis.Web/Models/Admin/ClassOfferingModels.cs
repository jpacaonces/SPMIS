using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Spmis.Web.Models.Admin;

public class ClassOfferingListItem
{
    public int Id { get; set; }
    public string SectionCode { get; set; } = "";
    public string SubjectCode { get; set; } = "";
    public string SubjectTitle { get; set; } = "";
    public string Faculty { get; set; } = "";
    public string AttendanceMode { get; set; } = "";
    public int Enrolled { get; set; }
    public bool IsActive { get; set; }
}

public class ClassOfferingListViewModel
{
    public string? TermLabel { get; set; }
    public string? Search { get; set; }
    public int Page { get; set; } = 1;
    public int TotalPages { get; set; } = 1;
    public int TotalCount { get; set; }
    public List<ClassOfferingListItem> Items { get; set; } = [];
}

public class ClassOfferingCreateViewModel
{
    public string? TermLabel { get; set; }

    [Required, Display(Name = "Section")]
    public int? SectionId { get; set; }

    [Required, Display(Name = "Subject")]
    public int? SubjectId { get; set; }

    [Required, Display(Name = "Faculty")]
    public int? FacultyId { get; set; }

    [Required, Display(Name = "Transmutation table")]
    public int? TransmutationTableId { get; set; }

    [Required, Display(Name = "Attendance mode")]
    public string AttendanceMode { get; set; } = "Meetings";

    public IEnumerable<SelectListItem> Sections { get; set; } = [];
    public IEnumerable<SelectListItem> Subjects { get; set; } = [];
    public IEnumerable<SelectListItem> FacultyOptions { get; set; } = [];
    public IEnumerable<SelectListItem> Tables { get; set; } = [];
}

public class ClassOfferingStatusViewModel
{
    public int ClassOfferingId { get; set; }
    public string Label { get; set; } = "";
    public bool IsActive { get; set; }
    public bool Activate { get; set; }
    public string? Blocker { get; set; }
    public ReasonInput Reason { get; set; } = new();
}