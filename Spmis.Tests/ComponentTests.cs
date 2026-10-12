using Spmis.Web.Grading;

namespace Spmis.Tests;

public class ComponentTests
{
    private static readonly GradingEngine Engine = new(new GradingOptions(5m));
    private static readonly DateTime Due = new(2026, 10, 1, 12, 0, 0);

    private static ComponentInput Comp(params ItemInput[] items) =>
        new("Quiz", ComponentType.ClassStanding, 30m, false, items);

    private static ItemInput Sub(decimal max, decimal raw,
        DateTime? due = null, DateTime? at = null, bool required = true) =>
        new("item", max, raw, ScoreStatus.Submitted, due, at ?? due, required);

    private static ItemInput Other(ScoreStatus status, decimal max, bool required = true) =>
        new("item", max, null, status, null, null, required);

    [Fact]
    public void Percent_is_points_based_not_an_average_of_percents()
    {
        // 15/20 and 24/30: points = 39/50 = 78%. (Average of 75 and 80 would be 77.5.)
        var r = Engine.ComputeComponent(Comp(Sub(20, 15), Sub(30, 24)));
        Assert.Equal(78m, r.Percent);
    }

    [Fact]
    public void On_time_and_exactly_at_the_deadline_have_no_penalty()
    {
        var r = Engine.ComputeComponent(Comp(Sub(100, 90, Due, Due)));
        Assert.Equal(90m, r.Percent);
    }

    [Fact]
    public void Part_of_a_day_late_counts_as_one_day()
    {
        var r = Engine.ComputeComponent(Comp(Sub(100, 90, Due, Due.AddHours(23))));
        Assert.Equal(85m, r.Percent);
        Assert.Equal(1, r.Items[0].LateDays);
    }

    [Fact]
    public void Exactly_24_hours_late_is_one_day()
    {
        var r = Engine.ComputeComponent(Comp(Sub(100, 90, Due, Due.AddDays(1))));
        Assert.Equal(85m, r.Percent);
    }

    [Fact]
    public void One_second_over_a_day_is_two_days()
    {
        var r = Engine.ComputeComponent(Comp(Sub(100, 90, Due, Due.AddDays(1).AddSeconds(1))));
        Assert.Equal(80m, r.Percent);
    }

    [Fact]
    public void Penalty_scales_with_the_item_max_score()
    {
        // 5 points per day = 5% of 20 = 1 point.
        var r = Engine.ComputeComponent(Comp(Sub(20, 20, Due, Due.AddHours(1))));
        Assert.Equal(95m, r.Percent);
    }

    [Fact]
    public void Late_penalty_never_goes_below_zero()
    {
        var r = Engine.ComputeComponent(Comp(Sub(100, 10, Due, Due.AddDays(3))));
        Assert.Equal(0m, r.Percent);
    }

    [Fact]
    public void Penalty_comes_from_the_options()
    {
        var engine = new GradingEngine(new GradingOptions(10m));
        var r = engine.ComputeComponent(Comp(Sub(100, 90, Due, Due.AddHours(1))));
        Assert.Equal(80m, r.Percent);
    }

    [Fact]
    public void Statuses_follow_the_agreed_rules()
    {
        var r = Engine.ComputeComponent(Comp(
            Sub(10, 10),                              // counted: 10 / 10
            Other(ScoreStatus.Missing, 10, true),     // required missing: 0 / 10
            Other(ScoreStatus.Missing, 10, false),    // optional missing: left out
            Other(ScoreStatus.Pending, 10),           // left out
            Other(ScoreStatus.Excused, 10),           // left out
            Other(ScoreStatus.Violation, 20)));       // counted: 0 / 20

        Assert.Equal(25m, r.Percent);                 // 10 / 40
        Assert.Equal(3, r.CountedItems);
        Assert.Equal(3, r.ExcludedItems);
    }

    [Fact]
    public void No_counted_items_gives_no_percent()
    {
        var r = Engine.ComputeComponent(Comp(Other(ScoreStatus.Pending, 10)));
        Assert.Null(r.Percent);
    }

    [Fact]
    public void Score_above_the_max_counts_as_the_max()
    {
        var r = Engine.ComputeComponent(Comp(Sub(10, 12)));
        Assert.Equal(100m, r.Percent);
    }

    [Fact]
    public void Submitted_without_a_score_is_left_out()
    {
        var item = new ItemInput("item", 10, null, ScoreStatus.Submitted, null, Due, true);
        var r = Engine.ComputeComponent(Comp(item));
        Assert.Null(r.Percent);
    }

    [Fact]
    public void A_max_score_of_zero_is_rejected()
    {
        Assert.Throws<ArgumentException>(() => Engine.ComputeComponent(Comp(Sub(0, 0))));
    }
}