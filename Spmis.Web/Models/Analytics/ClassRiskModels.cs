namespace Spmis.Web.Models.Analytics;

public class ClassRiskRow
{
    public string StudentNo { get; set; } = "";
    public string Name { get; set; } = "";
    public string Status { get; set; } = "";        // Final / Provisional / NoData
    public decimal? RawGrade { get; set; }
    public decimal? ReportedGrade { get; set; }
    public bool? IsPassing { get; set; }
    public int? Absences { get; set; }              // null when the class does not use meeting attendance
    public decimal? AbsencePercent { get; set; }
    public List<string> Reasons { get; set; } = new();
    public bool IsAtRisk => Reasons.Count > 0;
}

public class ClassRiskViewModel
{
    public int ClassOfferingId { get; set; }
    public string Header { get; set; } = "";
    public string Section { get; set; } = "";
    public string Faculty { get; set; } = "";
    public string ClassStatus { get; set; } = "";
    public string AttendanceMode { get; set; } = "";
    public int HeldMeetings { get; set; }
    public decimal AbsenceLimitPercent { get; set; }
    public decimal MarginPoints { get; set; }
    public decimal? PassingRaw { get; set; }
    public List<ClassRiskRow> Rows { get; set; } = new();
}