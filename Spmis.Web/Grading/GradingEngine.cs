namespace Spmis.Web.Grading;

public sealed class GradingEngine
{
    private readonly GradingOptions _options;

    public GradingEngine(GradingOptions options) => _options = options;

    // ------------------------------------------------------------------
    // Attendance: (present + late) / (meetings - excused) x 100.
    // Null when no meeting counts yet. The caller passes only the ACTIVE
    // meetings that fall inside the grading period's dates.
    // ------------------------------------------------------------------
    public static decimal? ComputeAttendancePercent(IEnumerable<AttendanceStatus> statuses)
    {
        ArgumentNullException.ThrowIfNull(statuses);
        var list = statuses.ToList();

        var counted = list.Count(s => s != AttendanceStatus.Excused);
        if (counted == 0) return null;

        var attended = list.Count(s => s is AttendanceStatus.Present or AttendanceStatus.Late);
        return attended * 100m / counted;
    }

    // ------------------------------------------------------------------
    // Component percent = sum(earned) / sum(max) of the items that count.
    // The attendance component takes its percent from the caller instead.
    // ------------------------------------------------------------------
    public ComponentResult ComputeComponent(ComponentInput component)
    {
        ArgumentNullException.ThrowIfNull(component);

        if (component.IsAttendance)
            return new ComponentResult(component.Name, component.AttendancePercent,
                0m, 0m, 0, 0, []);

        var results = component.Items.Select(EvaluateItem).ToList();
        var counted = results.Where(r => r.Counted).ToList();

        var earned = counted.Sum(r => r.Earned);
        var max = counted.Sum(r => r.Max);
        decimal? percent = max > 0 ? earned * 100m / max : null;

        return new ComponentResult(component.Name, percent, earned, max,
            counted.Count, results.Count - counted.Count, results);
    }

    public ItemResult EvaluateItem(ItemInput item)
    {
        ArgumentNullException.ThrowIfNull(item);
        if (item.MaxScore <= 0)
            throw new ArgumentException(
                $"Item '{item.Title}' has max score {item.MaxScore}; it must be above 0.");

        switch (item.Status)
        {
            case ScoreStatus.Submitted:
                if (item.RawScore is null)
                    return Excluded(item, "Submitted, not graded yet");
                if (item.RawScore < 0)
                    throw new ArgumentException(
                        $"Item '{item.Title}' has a negative score.");

                // A score above the maximum counts as the maximum (no extra credit).
                var raw = Math.Min(item.RawScore.Value, item.MaxScore);
                var days = LateDays(item.DueDate, item.SubmittedAt);

                // Penalty is percentage points of the item: 5 points per day = 5% of max per day.
                var penalty = days * _options.LatePenaltyPointsPerDay * item.MaxScore / 100m;
                var earned = Math.Max(0m, raw - penalty);

                return new ItemResult(item.Title, true, earned, item.MaxScore, days,
                    days > 0 ? $"Late {days} day(s)" : "Counted");

            case ScoreStatus.Violation:
                return new ItemResult(item.Title, true, 0m, item.MaxScore, 0, "Violation counts as 0");

            case ScoreStatus.Missing:
                return item.IsRequired
                    ? new ItemResult(item.Title, true, 0m, item.MaxScore, 0, "Missing (required) counts as 0")
                    : Excluded(item, "Missing (optional), left out");

            case ScoreStatus.Pending:
                return Excluded(item, "Pending, not graded yet");

            case ScoreStatus.Excused:
                return Excluded(item, "Excused, left out");

            default:
                throw new ArgumentOutOfRangeException(nameof(item), item.Status, "Unknown score status.");
        }
    }

    // Any started day counts as a full day. Integer maths on ticks: no floating-point surprises.
    public static int LateDays(DateTime? due, DateTime? submitted)
    {
        if (due is null || submitted is null || submitted <= due) return 0;
        var ticks = (submitted.Value - due.Value).Ticks;
        return (int)((ticks + TimeSpan.TicksPerDay - 1) / TimeSpan.TicksPerDay);
    }

    // ------------------------------------------------------------------
    // One grading period.
    //   Class Standing % = weighted average of its components that have data
    //                      (a component with no data is left out and the others are rescaled).
    //   Raw = CS% x CS weight + Major Exam% x ME weight.
    // Full precision is kept: rounding happens once, at the semester level.
    // ------------------------------------------------------------------
    public PeriodResult ComputePeriod(PeriodInput period)
    {
        ArgumentNullException.ThrowIfNull(period);
        if (period.ClassStandingWeight < 0 || period.MajorExamWeight < 0 ||
            period.ClassStandingWeight + period.MajorExamWeight != 100m)
            throw new ArgumentException(
                "Class Standing and Major Exam weights must be at least 0 and total 100.");

        var results = period.Components.Select(ComputeComponent).ToList();
        var cs = WeightedPercent(period.Components, results, ComponentType.ClassStanding);
        var me = WeightedPercent(period.Components, results, ComponentType.MajorExam);

        if (cs is not null && me is not null)
        {
            var raw = (cs.Value * period.ClassStandingWeight + me.Value * period.MajorExamWeight) / 100m;
            return new PeriodResult(period.Code, PeriodStatus.Complete, cs, me, raw, results);
        }

        if (cs is not null)   // no Major Exam yet: an estimate from Class Standing alone
            return new PeriodResult(period.Code, PeriodStatus.Provisional, cs, null, cs, results);

        return new PeriodResult(period.Code, PeriodStatus.NoData, null, me, null, results);
    }

    // ------------------------------------------------------------------
    // Semester: equal average of the periods' raw grades, rounded ONCE
    // (2 decimals, half away from zero), then transmuted.
    // Final only when every period is Complete; otherwise a provisional estimate.
    // ------------------------------------------------------------------
    public SemesterResult ComputeSemester(
        IReadOnlyList<PeriodResult> periods, TransmutationTable table)
    {
        ArgumentNullException.ThrowIfNull(periods);
        ArgumentNullException.ThrowIfNull(table);

        var usable = periods.Where(p => p.RawGrade is not null).ToList();
        if (usable.Count == 0)
            return new SemesterResult(SemesterStatus.NoData, null, null, null, 0, periods);

        var average = usable.Average(p => p.RawGrade!.Value);
        var raw = Math.Round(average, 2, MidpointRounding.AwayFromZero);
        var row = table.Lookup(raw);

        var isFinal = periods.All(p => p.Status == PeriodStatus.Complete);
        return new SemesterResult(
            isFinal ? SemesterStatus.Final : SemesterStatus.Provisional,
            raw, row.ReportedGrade, row.IsPassing, usable.Count, periods);
    }

    // Weighted average over the components of one type that have a percent.
    private static decimal? WeightedPercent(
        IReadOnlyList<ComponentInput> inputs,
        IReadOnlyList<ComponentResult> results,
        ComponentType type)
    {
        decimal weightSum = 0m, total = 0m;
        for (var i = 0; i < inputs.Count; i++)
        {
            if (inputs[i].Type != type || results[i].Percent is null) continue;

            var weight = inputs[i].WeightPct;
            if (weight < 0)
                throw new ArgumentException($"Component '{inputs[i].Name}' has a negative weight.");

            weightSum += weight;
            total += weight * results[i].Percent!.Value;
        }
        return weightSum > 0 ? total / weightSum : null;
    }

    private static ItemResult Excluded(ItemInput item, string note) =>
        new(item.Title, false, 0m, item.MaxScore, 0, note);
}