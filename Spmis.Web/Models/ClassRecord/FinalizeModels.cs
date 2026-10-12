namespace Spmis.Web.Models.ClassRecord;

public class FinalizeRow
{
    public string StudentNo { get; set; } = "";
    public string StudentName { get; set; } = "";
    public decimal? RawGrade { get; set; }
    public decimal? ReportedGrade { get; set; }
    public bool? IsPassing { get; set; }
    public string Status { get; set; } = "";     // Final / Provisional / NoData
}

public class FinalizeReviewViewModel
{
    public int ClassOfferingId { get; set; }
    public string Header { get; set; } = "";
    public bool IsFinalized { get; set; }
    public DateTime? FinalizedAt { get; set; }
    public List<FinalizeRow> Rows { get; set; } = new();
    public List<string> Blockers { get; set; } = new();
    public bool CanFinalize => !IsFinalized && Blockers.Count == 0;
}

public record FinalizeResult(bool Success, string? Error);