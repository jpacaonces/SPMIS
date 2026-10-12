namespace Spmis.Web.Models.Analytics;

public class ProgramClassRow
{
    public int ClassOfferingId { get; set; }
    public string Subject { get; set; } = "";
    public string Section { get; set; } = "";
    public string Faculty { get; set; } = "";
    public string ClassStatus { get; set; } = "";   // Open / Finalized
    public int Students { get; set; }
    public int Graded { get; set; }                 // students with any grade data
    public decimal? AverageRaw { get; set; }
    public int Passing { get; set; }                // estimated passing
    public int AtRisk { get; set; }
}

public class ProgramDashboardViewModel
{
    public string? ProgramLabel { get; set; }
    public string? TermLabel { get; set; }
    public string? Message { get; set; }
    public decimal MarginPoints { get; set; }
    public int TotalStudents { get; set; }
    public int TotalAtRisk { get; set; }
    public List<ProgramClassRow> Rows { get; set; } = new();
}