using Spmis.Web.Grading;

namespace Spmis.Tests;

public class PeriodTests
{
    private static readonly GradingEngine Engine = new(new GradingOptions(5m));

    // One graded item that yields exactly 'percent' (out of 1000 points).
    private static ItemInput Item(decimal percent) =>
        new("item", 1000m, percent * 10m, ScoreStatus.Submitted, null, null, true);

    private static ComponentInput Standing(string name, decimal weight, decimal? percent) =>
        new(name, ComponentType.ClassStanding, weight, false,
            percent is null ? [] : [Item(percent.Value)]);

    private static ComponentInput Exam(decimal? percent) =>
        new("Major Exam", ComponentType.MajorExam, 100m, false,
            percent is null ? [] : [Item(percent.Value)]);

    private static PeriodInput Period(params ComponentInput[] components) =>
        new("PRELIM", 60m, 40m, components);

    // ---- attendance ----

    [Fact]
    public void Attendance_counts_present_and_late_and_ignores_excused()
    {
        var p = GradingEngine.ComputeAttendancePercent(new[]
        {
            AttendanceStatus.Present, AttendanceStatus.Late, AttendanceStatus.Absent,
            AttendanceStatus.Excused, AttendanceStatus.Present
        });
        Assert.Equal(75m, p);   // 3 attended / (5 - 1 excused)
    }

    [Fact]
    public void Attendance_is_null_when_no_meeting_counts()
    {
        Assert.Null(GradingEngine.ComputeAttendancePercent([]));
        Assert.Null(GradingEngine.ComputeAttendancePercent(new[] { AttendanceStatus.Excused }));
    }

    [Fact]
    public void Attendance_component_takes_the_supplied_percent()
    {
        var c = new ComponentInput("Attendance and Recitation", ComponentType.ClassStanding,
            10m, true, [], AttendancePercent: 92.5m);
        Assert.Equal(92.5m, Engine.ComputeComponent(c).Percent);
    }

    // ---- period ----

    [Fact]
    public void Period_combines_class_standing_and_exam_60_40()
    {
        // The agreed example: CS 85.9 and exam 82 give 84.34.
        var r = Engine.ComputePeriod(Period(Standing("Quiz", 100m, 85.9m), Exam(82m)));

        Assert.Equal(PeriodStatus.Complete, r.Status);
        Assert.Equal(85.9m, r.ClassStandingPercent);
        Assert.Equal(84.34m, r.RawGrade);
    }

    [Fact]
    public void Component_without_data_is_left_out_and_the_rest_rescaled()
    {
        // Weights 20, 20, 30 where the 30 has no data: (20x80 + 20x90) / 40 = 85.
        var r = Engine.ComputePeriod(Period(
            Standing("A", 20m, 80m),
            Standing("B", 20m, 90m),
            Standing("C", 30m, null),
            Exam(85m)));

        Assert.Equal(85m, r.ClassStandingPercent);
        Assert.Equal(85m, r.RawGrade);
    }

    [Fact]
    public void Attendance_takes_part_in_class_standing_like_any_component()
    {
        var attendance = new ComponentInput("Attendance", ComponentType.ClassStanding,
            10m, true, [], AttendancePercent: 100m);

        // CS = (10x100 + 30x70) / 40 = 77.5;  raw = 77.5x0.6 + 80x0.4 = 78.5
        var r = Engine.ComputePeriod(Period(attendance, Standing("Quiz", 30m, 70m), Exam(80m)));

        Assert.Equal(77.5m, r.ClassStandingPercent);
        Assert.Equal(78.5m, r.RawGrade);
    }

    [Fact]
    public void No_exam_yet_gives_a_provisional_class_standing_estimate()
    {
        var r = Engine.ComputePeriod(Period(Standing("Quiz", 100m, 88m), Exam(null)));

        Assert.Equal(PeriodStatus.Provisional, r.Status);
        Assert.Equal(88m, r.RawGrade);
        Assert.Null(r.MajorExamPercent);
    }

    [Fact]
    public void No_data_at_all_gives_no_raw_grade()
    {
        var r = Engine.ComputePeriod(Period(Standing("Quiz", 100m, null), Exam(null)));

        Assert.Equal(PeriodStatus.NoData, r.Status);
        Assert.Null(r.RawGrade);
    }

    [Fact]
    public void Exam_without_any_class_standing_is_not_enough_for_a_grade()
    {
        var r = Engine.ComputePeriod(Period(Standing("Quiz", 100m, null), Exam(90m)));

        Assert.Equal(PeriodStatus.NoData, r.Status);
        Assert.Null(r.RawGrade);
    }

    [Fact]
    public void Period_weights_must_total_100()
    {
        var bad = new PeriodInput("PRELIM", 60m, 30m, []);
        Assert.Throws<ArgumentException>(() => Engine.ComputePeriod(bad));
    }
}