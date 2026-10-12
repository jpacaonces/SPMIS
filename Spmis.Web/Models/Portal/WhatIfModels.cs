namespace Spmis.Web.Models.Portal;

public class WhatIfPeriodRow
{
    public string Code { get; set; } = "";
    public string Status { get; set; } = "";     // Complete / Provisional / NoData
    public decimal? CurrentRaw { get; set; }
    public bool IsLocked { get; set; }           // Complete periods cannot be changed
    public string? Assumed { get; set; }         // what the student typed
}

public class WhatIfScenario
{
    public string Label { get; set; } = "";
    public decimal Raw { get; set; }
    public decimal Reported { get; set; }
    public bool IsPassing { get; set; }
}

public class WhatIfViewModel
{
    public int EnrollmentId { get; set; }
    public string Header { get; set; } = "";
    public string? Blocked { get; set; }         // set when the page cannot be used
    public string? Error { get; set; }
    public string? NeededMessage { get; set; }
    public List<WhatIfPeriodRow> Periods { get; set; } = new();
    public List<WhatIfScenario> Scenarios { get; set; } = new();
}