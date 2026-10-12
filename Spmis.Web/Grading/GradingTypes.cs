namespace Spmis.Web.Grading;

// Mirrors scores.status in the database.
public enum ScoreStatus { Pending, Submitted, Missing, Excused, Violation }

// Mirrors class_components.component_type.
public enum ComponentType { ClassStanding, MajorExam }

// Mirrors attendance_records.status.
public enum AttendanceStatus { Present, Late, Absent, Excused }

// Rules that can be configured (they come from system_settings).
public sealed record GradingOptions(decimal LatePenaltyPointsPerDay = 5m);

// One graded item (a quiz, an exam, a case study) for one student.
public sealed record ItemInput(
    string Title,
    decimal MaxScore,
    decimal? RawScore,
    ScoreStatus Status,
    DateTime? DueDate,
    DateTime? SubmittedAt,
    bool IsRequired);

// One component of the class scheme, with one student's data for ONE grading period.
// For the attendance component, the caller supplies AttendancePercent (items are ignored).
public sealed record ComponentInput(
    string Name,
    ComponentType Type,
    decimal WeightPct,
    bool IsAttendance,
    IReadOnlyList<ItemInput> Items,
    decimal? AttendancePercent = null);

// What happened to one item, so screens can explain the number to the student.
public sealed record ItemResult(
    string Title,
    bool Counted,
    decimal Earned,
    decimal Max,
    int LateDays,
    string Note);

// Percent is null when nothing counted yet (nothing to average).
public sealed record ComponentResult(
    string Name,
    decimal? Percent,
    decimal EarnedPoints,
    decimal MaxPoints,
    int CountedItems,
    int ExcludedItems,
    IReadOnlyList<ItemResult> Items);

// One grading period (Prelim, Midterm, Pre-Final, Final) of one class for one student.
public sealed record PeriodInput(
    string Code,
    decimal ClassStandingWeight,
    decimal MajorExamWeight,
    IReadOnlyList<ComponentInput> Components);

// Complete    = Class Standing and Major Exam both have data.
// Provisional = Class Standing only (an estimate, no Major Exam yet).
// NoData      = nothing usable yet.
public enum PeriodStatus { Complete, Provisional, NoData }

public sealed record PeriodResult(
    string Code,
    PeriodStatus Status,
    decimal? ClassStandingPercent,
    decimal? MajorExamPercent,
    decimal? RawGrade,
    IReadOnlyList<ComponentResult> Components);

// Final       = all periods are Complete.
// Provisional = an average of the periods that have a raw grade (an estimate).
public enum SemesterStatus { Final, Provisional, NoData }

public sealed record SemesterResult(
    SemesterStatus Status,
    decimal? RawGrade,
    decimal? ReportedGrade,
    bool? IsPassing,
    int PeriodsUsed,
    IReadOnlyList<PeriodResult> Periods);