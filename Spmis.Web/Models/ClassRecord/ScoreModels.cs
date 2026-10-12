using Spmis.Web.Models;
using System.ComponentModel.DataAnnotations;
namespace Spmis.Web.Models.ClassRecord;

public class ScoreRowInput
{
    // What the form posts back. Identity and version are checked against the DB.
    public int EnrollmentId { get; set; }
    public int ScoreId { get; set; }                 // 0 = no score row yet
    public long RowVersionTicks { get; set; }        // 0 when no row yet

    public string Status { get; set; } = "Pending";
    public decimal? RawScore { get; set; }
    public DateTime? SubmittedAt { get; set; }

    // Display only (refilled by the server)
    public string StudentNo { get; set; } = "";
    public string StudentName { get; set; } = "";
}

public class ScoreEntryViewModel
{
    public int AssessmentId { get; set; }
    public int ClassOfferingId { get; set; }
    public string Header { get; set; } = "";
    public string Title { get; set; } = "";
    public string ComponentName { get; set; } = "";
    public string PeriodName { get; set; } = "";
    public decimal MaxScore { get; set; }
    public DateTime? DueDate { get; set; }
    public List<ScoreRowInput> Rows { get; set; } = new();
    public ReasonInput Reason { get; set; } = new();
}

public record ScoreSaveResult(bool Success, string? Field, string? Error, int Changed);

public class SimilarityHistoryItem
{
    public DateTime CheckedAt { get; set; }
    public decimal Percent { get; set; }
    public string Band { get; set; } = "";
    public string CheckedBy { get; set; } = "";
    public string? Note { get; set; }
}

public class SimilarityViewModel
{
    public int AssessmentId { get; set; }
    public int EnrollmentId { get; set; }

    // Display only (filled by the server)
    public string Header { get; set; } = "";
    public string AssessmentTitle { get; set; } = "";
    public string StudentNo { get; set; } = "";
    public string StudentName { get; set; } = "";
    public string ScoreStatus { get; set; } = "";
    public bool CanCheck { get; set; }
    public decimal AcceptedMax { get; set; }
    public decimal RevisionMax { get; set; }
    public List<SimilarityHistoryItem> History { get; set; } = new();

    [Required(ErrorMessage = "Enter the similarity percentage.")]
    [Display(Name = "AI similarity (%)")]
    public decimal? Percent { get; set; }

    [StringLength(500)]
    public string? Note { get; set; }
}