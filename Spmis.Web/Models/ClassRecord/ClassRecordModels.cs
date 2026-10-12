using Spmis.Web.Grading;

namespace Spmis.Web.Models.ClassRecord;

public class MyClassItem
{
    public int Id { get; set; }
    public string SectionCode { get; set; } = "";
    public string SubjectCode { get; set; } = "";
    public string SubjectTitle { get; set; } = "";
    public string AttendanceMode { get; set; } = "";
    public int Enrolled { get; set; }
    public string Status { get; set; } = "";
}

public class MyClassesViewModel
{
    public string? TermLabel { get; set; }
    public bool NoFacultyProfile { get; set; }
    public List<MyClassItem> Items { get; set; } = [];
}

public class ClassRecordViewModel
{
    public int ClassOfferingId { get; set; }
    public string SectionCode { get; set; } = "";
    public string SubjectCode { get; set; } = "";
    public string SubjectTitle { get; set; } = "";
    public string TermLabel { get; set; } = "";
    public string AttendanceMode { get; set; } = "";
    public string Status { get; set; } = "";
    public decimal ClassStandingWeight { get; set; }
    public decimal MajorExamWeight { get; set; }
    public ClassGradeSheet Sheet { get; set; } = new(0, []);
}