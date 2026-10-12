using Spmis.Web.Grading;

namespace Spmis.Web.Models.Portal;

public class MyClassItem
{
    public int EnrollmentId { get; set; }
    public string SubjectCode { get; set; } = "";
    public string SubjectTitle { get; set; } = "";
    public string FacultyName { get; set; } = "";
    public decimal? RawGrade { get; set; }
    public decimal? ReportedGrade { get; set; }
    public bool? IsPassing { get; set; }
    public string Status { get; set; } = "";      // Final / Provisional / NoData
    public bool IsFinalized { get; set; }
}

public class MyClassesViewModel
{
    public string? TermLabel { get; set; }
    public string? Message { get; set; }
    public List<MyClassItem> Items { get; set; } = new();
}

public class MyAttendanceItem
{
    public DateOnly Date { get; set; }
    public string? Topic { get; set; }
    public string Mark { get; set; } = "Not marked";
    public bool ExcusedByLetter { get; set; }
}

public class MyClassDetailViewModel
{
    public int EnrollmentId { get; set; }
    public string Header { get; set; } = "";
    public string FacultyName { get; set; } = "";
    public string AttendanceMode { get; set; } = "";
    public bool IsFinalized { get; set; }

    // Shown at the top. For a finalized class these come from the saved snapshot.
    public string Status { get; set; } = "";
    public decimal? RawGrade { get; set; }
    public decimal? ReportedGrade { get; set; }
    public bool? IsPassing { get; set; }

    public IReadOnlyList<PeriodResult> Periods { get; set; } = Array.Empty<PeriodResult>();
    public List<MyAttendanceItem> Attendance { get; set; } = new();
}