using Spmis.Web.Models;

namespace Spmis.Web.Models.ClassRecord;

public class ExcuseListItem
{
    public int DecisionId { get; set; }
    public string StudentNo { get; set; } = "";
    public string StudentName { get; set; } = "";
    public string SubjectCode { get; set; } = "";
    public DateOnly DateFrom { get; set; }
    public DateOnly DateTo { get; set; }
    public string Reason { get; set; } = "";
    public DateTime SubmittedAt { get; set; }
    public string Status { get; set; } = "";
    public int MeetingsInRange { get; set; }
}

public class ExcuseListViewModel
{
    public string Filter { get; set; } = "pending";
    public string? NoFacultyMessage { get; set; }
    public List<ExcuseListItem> Items { get; set; } = new();
}

public class ExcuseMeetingItem
{
    public DateOnly Date { get; set; }
    public string? Topic { get; set; }
    public string Mark { get; set; } = "Not marked";
}

public class ExcuseDecideViewModel
{
    public int DecisionId { get; set; }

    // Display only (filled by the server)
    public string Header { get; set; } = "";
    public string StudentNo { get; set; } = "";
    public string StudentName { get; set; } = "";
    public DateOnly DateFrom { get; set; }
    public DateOnly DateTo { get; set; }
    public string LetterReason { get; set; } = "";
    public string GuardianName { get; set; } = "";
    public string GuardianRelationship { get; set; } = "";
    public string SignatoryName { get; set; } = "";
    public string FileName { get; set; } = "";
    public DateTime SubmittedAt { get; set; }
    public string Status { get; set; } = "";
    public string? DecisionReason { get; set; }
    public DateTime? DecidedAt { get; set; }
    public List<ExcuseMeetingItem> Meetings { get; set; } = new();

    // Posted by the form
    public bool? Approve { get; set; }
    public ReasonInput Reason { get; set; } = new();
}

public record ExcuseResult(bool Success, string? Field, string? Error);