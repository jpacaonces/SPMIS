using System.Globalization;
using Microsoft.EntityFrameworkCore;
using Spmis.Web.Data.Entities;
using Spmis.Web.Grading;
using EngineRow = Spmis.Web.Grading.TransmutationRow;   // the entity has the same name
using EngineTable = Spmis.Web.Grading.TransmutationTable;

namespace Spmis.Web.Services;

public class GradeCalculationService : IGradeCalculationService
{
    private readonly SpmisDbContext _db;

    public GradeCalculationService(SpmisDbContext db) => _db = db;

    public Task<ClassGradeSheet?> GetClassSheetAsync(int classOfferingId) =>
        LoadAsync(classOfferingId, null);

    public async Task<StudentGradeRow?> GetStudentGradeAsync(int enrollmentId)
    {
        if (enrollmentId <= 0) return null;
        var id = (uint)enrollmentId;

        var classId = await _db.Enrollments.AsNoTracking()
            .Where(e => e.Id == id)
            .Select(e => e.ClassOfferingId)
            .FirstOrDefaultAsync();
        if (classId == 0) return null;

        var sheet = await LoadAsync((int)classId, enrollmentId);
        return sheet?.Students.FirstOrDefault();
    }

    private async Task<ClassGradeSheet?> LoadAsync(int classOfferingId, int? onlyEnrollmentId)
    {
        if (classOfferingId <= 0) return null;
        var classId = (uint)classOfferingId;

        var offering = await _db.ClassOfferings.AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == classId);
        if (offering is null) return null;

        var engine = new GradingEngine(new GradingOptions(await LoadLatePenaltyAsync()));

        // ---- everything the class needs, loaded once (no query per student) ----
        var components = await _db.ClassComponents.AsNoTracking()
            .Where(c => c.ClassOfferingId == classId && c.IsActive == true)
            .OrderBy(c => c.SortOrder)
            .ToListAsync();
        var componentIds = components.Select(c => c.Id).ToList();

        var periods = await (
            from tp in _db.TermPeriods.AsNoTracking()
            where tp.TermId == offering.TermId
            join g in _db.GradingPeriods on tp.GradingPeriodId equals g.Id
            orderby g.SortOrder
            select new { g.Id, g.Code, tp.StartDate, tp.EndDate }).ToListAsync();
        if (periods.Count == 0)
            throw new InvalidOperationException("The class's term has no grading period dates.");

        var tableRows = await _db.TransmutationRows.AsNoTracking()
            .Where(r => r.TableId == offering.TransmutationTableId)
            .Select(r => new { r.MinRaw, r.ReportedGrade, r.IsPassing })
            .ToListAsync();
        var table = new EngineTable(
            tableRows.Select(r => new EngineRow(r.MinRaw, r.ReportedGrade, r.IsPassing == true)));

        var assessments = await _db.Assessments.AsNoTracking()
            .Where(a => componentIds.Contains(a.ClassComponentId) && a.IsActive == true)
            .ToListAsync();
        var assessmentsById = assessments.ToDictionary(a => a.Id);

        var enrollmentsQuery =
            from e in _db.Enrollments.AsNoTracking()
            where e.ClassOfferingId == classId && e.Status == "Enrolled"
            join s in _db.Students on e.StudentId equals s.Id
            join u in _db.Users on s.UserId equals u.Id
            orderby s.StudentNo
            select new { e.Id, s.StudentNo, u.LastName, u.FirstName };
        if (onlyEnrollmentId is not null)
        {
            var only = (uint)onlyEnrollmentId.Value;
            enrollmentsQuery = enrollmentsQuery.Where(x => x.Id == only);
        }
        var enrollments = await enrollmentsQuery.ToListAsync();

        var assessmentIds = assessments.Select(a => a.Id).ToList();
        var enrollmentIds = enrollments.Select(e => e.Id).ToList();

        var scoreMap = (await _db.Scores.AsNoTracking()
                .Where(s => assessmentIds.Contains(s.AssessmentId) && enrollmentIds.Contains(s.EnrollmentId))
                .ToListAsync())
            .ToDictionary(s => (s.EnrollmentId, s.AssessmentId));

        var meetings = await _db.ClassMeetings.AsNoTracking()
            .Where(m => m.ClassOfferingId == classId && m.IsActive == true)
            .ToListAsync();
        var meetingIds = meetings.Select(m => m.Id).ToList();

        var attendanceMap = (await _db.AttendanceRecords.AsNoTracking()
                .Where(a => meetingIds.Contains(a.MeetingId) && enrollmentIds.Contains(a.EnrollmentId))
                .ToListAsync())
            .ToDictionary(a => (a.EnrollmentId, a.MeetingId), a => a.Status);

        var activityBased = offering.AttendanceMode == "ActivityBased";

        // ---- compute in memory with the pure engine ----
        var rows = new List<StudentGradeRow>();
        foreach (var e in enrollments)
        {
            var periodResults = new List<PeriodResult>();

            foreach (var p in periods)
            {
                var inputs = new List<ComponentInput>();
                foreach (var c in components)
                {
                    var type = Enum.Parse<ComponentType>(c.ComponentType);

                    if (c.IsAttendance == true)
                    {
                        var statuses = activityBased
                            ? ActivityAttendance(e.Id, p.StartDate, p.EndDate, meetings, assessmentsById, scoreMap)
                            : RecordedAttendance(e.Id, p.StartDate, p.EndDate, meetings, attendanceMap);

                        inputs.Add(new ComponentInput(c.Name, type, c.WeightPct, true, [],
                            GradingEngine.ComputeAttendancePercent(statuses)));
                    }
                    else
                    {
                        var items = assessments
                            .Where(a => a.ClassComponentId == c.Id && a.GradingPeriodId == p.Id)
                            .Select(a => ToItem(a, scoreMap.GetValueOrDefault((e.Id, a.Id))))
                            .ToList();
                        inputs.Add(new ComponentInput(c.Name, type, c.WeightPct, false, items));
                    }
                }

                periodResults.Add(engine.ComputePeriod(new PeriodInput(
                    p.Code, offering.ClassStandingWeight, offering.MajorExamWeight, inputs)));
            }

            rows.Add(new StudentGradeRow(
                (int)e.Id, e.StudentNo, $"{e.LastName}, {e.FirstName}",
                engine.ComputeSemester(periodResults, table)));
        }

        return new ClassGradeSheet(classOfferingId, rows);
    }

    // A missing score row means the item is not graded yet (Pending).
    private static ItemInput ToItem(Assessment a, Score? s) => new(
        a.Title,
        a.MaxScore,
        s?.RawScore,
        s is null ? ScoreStatus.Pending : Enum.Parse<ScoreStatus>(s.Status),
        a.DueDate,
        s?.SubmittedAt,
        a.IsRequired == true);

    // Meetings mode: the recorded attendance of the active meetings inside the period.
    private static List<AttendanceStatus> RecordedAttendance(
        uint enrollmentId, DateOnly start, DateOnly end,
        List<ClassMeeting> meetings,
        Dictionary<(uint, uint), string> attendanceMap)
    {
        var list = new List<AttendanceStatus>();
        foreach (var m in meetings.Where(m => m.MeetingDate >= start && m.MeetingDate <= end))
        {
            if (attendanceMap.TryGetValue((enrollmentId, m.Id), out var status))
                list.Add(Enum.Parse<AttendanceStatus>(status));
            // no record yet: not counted
        }
        return list;
    }

    // Activity-based mode: the score on the meeting's linked activity decides.
    private static List<AttendanceStatus> ActivityAttendance(
        uint enrollmentId, DateOnly start, DateOnly end,
        List<ClassMeeting> meetings,
        Dictionary<uint, Assessment> assessmentsById,
        Dictionary<(uint, uint), Score> scoreMap)
    {
        var list = new List<AttendanceStatus>();
        foreach (var m in meetings.Where(m => m.MeetingDate >= start && m.MeetingDate <= end))
        {
            if (m.LinkedAssessmentId is null) continue;
            if (!assessmentsById.TryGetValue(m.LinkedAssessmentId.Value, out var a)) continue;
            if (!scoreMap.TryGetValue((enrollmentId, a.Id), out var s)) continue;

            switch (Enum.Parse<ScoreStatus>(s.Status))
            {
                case ScoreStatus.Excused:
                    list.Add(AttendanceStatus.Excused);
                    break;
                case ScoreStatus.Missing:
                case ScoreStatus.Violation:
                    list.Add(AttendanceStatus.Absent);
                    break;
                case ScoreStatus.Submitted when s.RawScore is not null:
                    list.Add(s.RawScore.Value * 100m / a.MaxScore >= a.PassingPercent
                        ? AttendanceStatus.Present
                        : AttendanceStatus.Absent);
                    break;
                    // Pending, or submitted without a score yet: not counted
            }
        }
        return list;
    }

    private async Task<decimal> LoadLatePenaltyAsync()
    {
        var value = await _db.SystemSettings.AsNoTracking()
            .Where(s => s.SettingKey == "late.penalty_points_per_day")
            .Select(s => s.SettingValue)
            .FirstOrDefaultAsync();

        if (value is null) return 5m;   // the documented default
        if (decimal.TryParse(value, NumberStyles.Number, CultureInfo.InvariantCulture, out var d) && d >= 0)
            return d;

        // A bad setting must not silently change grades.
        throw new InvalidOperationException(
            $"System setting late.penalty_points_per_day has an invalid value: '{value}'.");
    }
}