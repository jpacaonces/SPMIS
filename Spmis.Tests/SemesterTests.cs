using Spmis.Web.Grading;

namespace Spmis.Tests;

public class SemesterTests
{
    private static readonly GradingEngine Engine = new(new GradingOptions(5m));

    // The SAMPLE table from the seed.
    private static readonly TransmutationTable Sample = new(new TransmutationRow[]
    {
        new(97m, 1.00m, true), new(94m, 1.25m, true), new(91m, 1.50m, true),
        new(88m, 1.75m, true), new(85m, 2.00m, true), new(82m, 2.25m, true),
        new(79m, 2.50m, true), new(76m, 2.75m, true), new(75m, 3.00m, true),
        new(0m,  5.00m, false)
    });

    private static PeriodResult P(PeriodStatus status, decimal? raw) =>
        new("P", status, null, null, raw, []);

    private static PeriodResult Done(decimal raw) => P(PeriodStatus.Complete, raw);
    private static PeriodResult Empty() => P(PeriodStatus.NoData, null);

    [Fact]
    public void Four_complete_periods_give_a_final_grade()
    {
        var r = Engine.ComputeSemester(
            [Done(80m), Done(84m), Done(86m), Done(90m)], Sample);   // average 85

        Assert.Equal(SemesterStatus.Final, r.Status);
        Assert.Equal(85m, r.RawGrade);
        Assert.Equal(2.00m, r.ReportedGrade);
        Assert.True(r.IsPassing);
    }

    [Fact]
    public void The_agreed_example_84_34_becomes_2_25()
    {
        var r = Engine.ComputeSemester(
            [Done(84.34m), Done(84.34m), Done(84.34m), Done(84.34m)], Sample);

        Assert.Equal(84.34m, r.RawGrade);
        Assert.Equal(2.25m, r.ReportedGrade);
    }

    [Fact]
    public void Rounding_happens_before_the_lookup()
    {
        // Average 84.995 rounds half away from zero to 85.00, which is 2.00 (not 2.25).
        var r = Engine.ComputeSemester(
            [Done(84.99m), Done(85.00m), Done(84.99m), Done(85.00m)], Sample);

        Assert.Equal(85.00m, r.RawGrade);
        Assert.Equal(2.00m, r.ReportedGrade);
    }

    [Fact]
    public void Missing_periods_give_a_provisional_average_of_the_ones_that_exist()
    {
        var r = Engine.ComputeSemester([Done(80m), Done(90m), Empty(), Empty()], Sample);

        Assert.Equal(SemesterStatus.Provisional, r.Status);
        Assert.Equal(85m, r.RawGrade);
        Assert.Equal(2, r.PeriodsUsed);
        Assert.Equal(2.00m, r.ReportedGrade);
    }

    [Fact]
    public void One_provisional_period_keeps_the_whole_semester_provisional()
    {
        var r = Engine.ComputeSemester(
            [Done(80m), Done(80m), Done(80m), P(PeriodStatus.Provisional, 80m)], Sample);

        Assert.Equal(SemesterStatus.Provisional, r.Status);
        Assert.Equal(4, r.PeriodsUsed);
    }

    [Fact]
    public void No_usable_period_means_no_grade()
    {
        var r = Engine.ComputeSemester([Empty(), Empty(), Empty(), Empty()], Sample);

        Assert.Equal(SemesterStatus.NoData, r.Status);
        Assert.Null(r.RawGrade);
        Assert.Null(r.ReportedGrade);
    }

    [Fact]
    public void Seventy_five_passes_and_just_below_fails()
    {
        var pass = Engine.ComputeSemester([Done(75m), Done(75m), Done(75m), Done(75m)], Sample);
        Assert.Equal(3.00m, pass.ReportedGrade);
        Assert.True(pass.IsPassing);

        var fail = Engine.ComputeSemester([Done(74.99m), Done(74.99m), Done(74.99m), Done(74.99m)], Sample);
        Assert.Equal(5.00m, fail.ReportedGrade);
        Assert.False(fail.IsPassing);
    }

    // ---- transmutation table ----

    [Theory]
    [InlineData(100, 1.00)]
    [InlineData(97, 1.00)]
    [InlineData(96.99, 1.25)]
    [InlineData(76, 2.75)]
    [InlineData(75.99, 3.00)]
    [InlineData(0, 5.00)]
    public void Lookup_uses_the_highest_lower_bound(double raw, double expected)
    {
        Assert.Equal((decimal)expected, Sample.Lookup((decimal)raw).ReportedGrade);
    }

    [Fact]
    public void Lookup_rejects_values_outside_0_to_100()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => Sample.Lookup(100.01m));
        Assert.Throws<ArgumentOutOfRangeException>(() => Sample.Lookup(-0.01m));
    }

    [Fact]
    public void A_table_without_a_zero_row_is_rejected()
    {
        Assert.Throws<ArgumentException>(() =>
            new TransmutationTable(new[] { new TransmutationRow(60m, 3.00m, true) }));
    }

    [Fact]
    public void A_table_with_duplicate_bounds_is_rejected()
    {
        Assert.Throws<ArgumentException>(() =>
            new TransmutationTable(new[]
            {
                new TransmutationRow(0m, 5.00m, false),
                new TransmutationRow(0m, 4.00m, false)
            }));
    }
}