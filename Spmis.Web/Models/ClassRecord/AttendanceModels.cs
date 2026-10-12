using Spmis.Web.Models;

namespace Spmis.Web.Models.ClassRecord;

public class AttendanceRowInput
{
    // Posted back by the form; checked against the DB on save.
    public int EnrollmentId { get; set; }
    public int RecordId { get; set; }            // 0 = not marked yet
    public long RowVersionTicks { get; set; }
    public string? Status { get; set; }          // null or "" = not marked

    // Display only (refilled by the server)
    public string StudentNo { get; set; } = "";
    public string StudentName { get; set; } = "";
    public int Absences { get; set; }
    public decimal AbsencePercent { get; set; }
    public string Flag { get; set; } = "";       // "", "Near limit", "Over limit"
}

public class AttendanceViewModel
{
    public int MeetingId { get; set; }
    public int ClassOfferingId { get; set; }
    public string Header { get; set; } = "";
    public DateOnly Date { get; set; }
    public string? Topic { get; set; }
    public decimal AbsenceLimitPercent { get; set; }
    public int HeldMeetings { get; set; }
    public List<AttendanceRowInput> Rows { get; set; } = new();
    public ReasonInput Reason { get; set; } = new();
}

public record AttendanceSaveResult(bool Success, string? Field, string? Error, int Changed);