using System.Globalization;
using Microsoft.EntityFrameworkCore;
using Spmis.Web.Data.Entities;
using Spmis.Web.Models.Analytics;

namespace Spmis.Web.Services;

public class ProgramAnalyticsService : IProgramAnalyticsService
{
    private readonly SpmisDbContext _db;
    private readonly IGradeCalculationService _grades;

    public ProgramAnalyticsService(SpmisDbContext db, IGradeCalculationService grades)
    {
        _db = db;
        _grades = grades;
    }

    private Task<uint?> HeadProgramIdAsync(Guid userId) =>
        _db.ProgramHeads.AsNoTracking()
            .Where(h => h.UserId == userId && h.IsActive == true)
            .Select(h => (uint?)h.ProgramId).FirstOrDefaultAsync();

    // ---------------------------------------------------------------- dashboard

    public async Task<ProgramDashboardViewModel> GetDashboardAsync(Guid userId)
    {
        var vm = new ProgramDashboardViewModel();

        // The program comes from the signed-in head's own profile, never from the URL.
        var programId = await HeadProgramIdAsync(userId);
        if (programId is null)
        {
            vm.Message = "Your account has no active Program Head profile.";
            return vm;
        }

        vm.ProgramLabel = await _db.Programs.AsNoTracking()
            .Where(p => p.Id == programId)
            .Select(p => p.Code + " - " + p.Name).FirstAsync();

        var term = await _db.AcademicTerms.AsNoTracking().FirstOrDefaultAsync(t => t.IsCurrent == true);
        if (term is null)
        {
            vm.Message = "There is no current academic term.";
            return vm;
        }

        vm.MarginPoints = await MarginAsync();
        var ratio = await WarningRatioAsync();

        var classes = await (
            from c in _db.ClassOfferings.AsNoTracking()
            where c.TermId == term.Id && c.IsActive == true
            join s in _db.Sections on c.SectionId equals s.Id
            where s.ProgramId == programId
            join sub in _db.Subjects on c.SubjectId equals sub.Id
            join f in _db.Faculties on c.FacultyId equals f.Id
            join u in _db.Users on f.UserId equals u.Id
            orderby sub.Code, s.YearLevel, s.SectionNo
            select new
            {
                c.Id,
                c.Status,
                c.TransmutationTableId,
                c.AttendanceMode,
                c.AbsenceLimitPercent,
                Subject = sub.Code + " - " + sub.Title,
                s.YearLevel,
                s.SectionNo,
                Faculty = u.LastName + ", " + u.FirstName
            }).ToListAsync();

        var tableIds = classes.Select(c => c.TransmutationTableId).Distinct().ToList();
        var passByTable = (await _db.TransmutationRows.AsNoTracking()
                .Where(r => tableIds.Contains(r.TableId) && r.IsPassing == true)
                .GroupBy(r => r.TableId)
                .Select(g => new { g.Key, Min = g.Min(r => r.MinRaw) })
                .ToListAsync())
            .ToDictionary(x => x.Key, x => x.Min);

        vm.TermLabel = $"Current term (id {term.Id})";

        foreach (var c in classes)
        {
            decimal? passRaw = passByTable.TryGetValue(c.TransmutationTableId, out var p) ? p : null;
            var (rows, _) = await BuildRowsAsync(c.Id, c.AttendanceMode, c.AbsenceLimitPercent,
                passRaw, vm.MarginPoints, ratio);

            var graded = rows.Where(r => r.RawGrade is not null).ToList();
            vm.Rows.Add(new ProgramClassRow
            {
                ClassOfferingId = (int)c.Id,
                Subject = c.Subject,
                Section = $"Y{c.YearLevel}-{c.SectionNo}",
                Faculty = c.Faculty,
                ClassStatus = c.Status,
                Students = rows.Count,
                Graded = graded.Count,
                AverageRaw = graded.Count > 0
                    ? Math.Round(graded.Average(r => r.RawGrade!.Value), 2, MidpointRounding.AwayFromZero)
                    : null,
                Passing = rows.Count(r => r.IsPassing == true),
                AtRisk = rows.Count(r => r.IsAtRisk)
            });
        }

        vm.TotalStudents = vm.Rows.Sum(r => r.Students);
        vm.TotalAtRisk = vm.Rows.Sum(r => r.AtRisk);
        return vm;
    }

    // ------------------------------------------------------------ class detail

    public async Task<ClassRiskViewModel?> GetClassAsync(Guid userId, int classOfferingId)
    {
        var programId = await HeadProgramIdAsync(userId);
        if (programId is null) return null;

        // Layer 2: the class must belong to THIS head's program.
        var cid = (uint)classOfferingId;
        var c = await (
            from o in _db.ClassOfferings.AsNoTracking()
            where o.Id == cid
            join s in _db.Sections on o.SectionId equals s.Id
            where s.ProgramId == programId
            join sub in _db.Subjects on o.SubjectId equals sub.Id
            join f in _db.Faculties on o.FacultyId equals f.Id
            join u in _db.Users on f.UserId equals u.Id
            select new
            {
                o.Id,
                o.Status,
                o.TransmutationTableId,
                o.AttendanceMode,
                o.AbsenceLimitPercent,
                Subject = sub.Code + " - " + sub.Title,
                s.YearLevel,
                s.SectionNo,
                Faculty = u.LastName + ", " + u.FirstName
            }).FirstOrDefaultAsync();
        if (c is null) return null;

        var margin = await MarginAsync();
        var ratio = await WarningRatioAsync();
        var passRaw = await _db.TransmutationRows.AsNoTracking()
            .Where(r => r.TableId == c.TransmutationTableId && r.IsPassing == true)
            .MinAsync(r => (decimal?)r.MinRaw);

        var (rows, held) = await BuildRowsAsync(c.Id, c.AttendanceMode, c.AbsenceLimitPercent,
            passRaw, margin, ratio);

        return new ClassRiskViewModel
        {
            ClassOfferingId = classOfferingId,
            Header = c.Subject,
            Section = $"Y{c.YearLevel}-{c.SectionNo}",
            Faculty = c.Faculty,
            ClassStatus = c.Status,
            AttendanceMode = c.AttendanceMode,
            HeldMeetings = held,
            AbsenceLimitPercent = c.AbsenceLimitPercent,
            MarginPoints = margin,
            PassingRaw = passRaw,
            // At-risk students first, then the lowest grades first.
            Rows = rows.OrderByDescending(r => r.IsAtRisk)
                       .ThenBy(r => r.RawGrade ?? 101m)
                       .ThenBy(r => r.Name).ToList()
        };
    }

    // ------------------------------------------------- the ONE at-risk rule

    private async Task<(List<ClassRiskRow> Rows, int Held)> BuildRowsAsync(
        uint classId, string attendanceMode, decimal absenceLimit,
        decimal? passRaw, decimal margin, decimal ratio)
    {
        var sheet = await _grades.GetClassSheetAsync((int)classId);
        var rows = new List<ClassRiskRow>();
        if (sheet is null) return (rows, 0);

        // Absences per enrollment (Meetings mode only), minus absences covered by approved letters.
        var absences = new Dictionary<int, int>();
        var held = 0;
        var useAttendance = attendanceMode == "Meetings";

        if (useAttendance)
        {
            var today = DateOnly.FromDateTime(DateTime.Today);
            var meetings = await _db.ClassMeetings.AsNoTracking()
                .Where(m => m.ClassOfferingId == classId && m.IsActive == true && m.MeetingDate <= today)
                .Select(m => new { m.Id, m.MeetingDate }).ToListAsync();
            held = meetings.Count;

            var dateById = meetings.ToDictionary(m => m.Id, m => m.MeetingDate);
            var heldIds = dateById.Keys.ToList();

            var absent = await _db.AttendanceRecords.AsNoTracking()
                .Where(r => heldIds.Contains(r.MeetingId) && r.Status == "Absent")
                .Select(r => new { r.EnrollmentId, r.MeetingId }).ToListAsync();

            var studentByEnrollment = await _db.Enrollments.AsNoTracking()
                .Where(e => e.ClassOfferingId == classId)
                .Select(e => new { e.Id, e.StudentId })
                .ToDictionaryAsync(x => x.Id, x => x.StudentId);

            var approved = await (
                from d in _db.ExcuseDecisions.AsNoTracking()
                where d.ClassOfferingId == classId && d.Status == "Approved"
                join l in _db.ExcuseLetters on d.LetterId equals l.Id
                select new { l.StudentId, l.DateFrom, l.DateTo }).ToListAsync();

            foreach (var a in absent)
            {
                var date = dateById[a.MeetingId];
                var sid = studentByEnrollment[a.EnrollmentId];
                var excused = approved.Any(x => x.StudentId == sid && date >= x.DateFrom && date <= x.DateTo);
                if (excused) continue;

                var key = (int)a.EnrollmentId;
                absences[key] = absences.GetValueOrDefault(key) + 1;
            }
        }

        foreach (var s in sheet.Students)
        {
            var row = new ClassRiskRow
            {
                StudentNo = s.StudentNo,
                Name = s.Name,
                Status = s.Semester.Status.ToString(),
                RawGrade = s.Semester.RawGrade,
                ReportedGrade = s.Semester.ReportedGrade,
                IsPassing = s.Semester.IsPassing
            };

            // Grade rule: estimated raw grade below passing plus the margin.
            if (row.RawGrade is not null && passRaw is not null && row.RawGrade.Value < passRaw.Value + margin)
                row.Reasons.Add(row.RawGrade.Value < passRaw.Value ? "Grade below passing" : "Grade near passing");

            // Absence rule: same thresholds as the faculty attendance page.
            if (useAttendance && held > 0)
            {
                var n = absences.GetValueOrDefault(s.EnrollmentId);
                var pct = Math.Round(n * 100m / held, 1);
                row.Absences = n;
                row.AbsencePercent = pct;

                if (pct > absenceLimit) row.Reasons.Add("Absences over limit");
                else if (pct >= absenceLimit * ratio) row.Reasons.Add("Absences near limit");
            }

            rows.Add(row);
        }

        return (rows, held);
    }

    // ---------------------------------------------------------------- settings

    private async Task<decimal> MarginAsync()
    {
        var v = await _db.SystemSettings.AsNoTracking()
            .Where(s => s.SettingKey == "atrisk.grade_margin_points")
            .Select(s => s.SettingValue).FirstOrDefaultAsync();
        return v is not null && decimal.TryParse(v, NumberStyles.Number, CultureInfo.InvariantCulture, out var d) && d >= 0
            ? d : 5m;
    }

    private async Task<decimal> WarningRatioAsync()
    {
        var v = await _db.SystemSettings.AsNoTracking()
            .Where(s => s.SettingKey == "atrisk.absence_warning_ratio")
            .Select(s => s.SettingValue).FirstOrDefaultAsync();
        return v is not null && decimal.TryParse(v, NumberStyles.Number, CultureInfo.InvariantCulture, out var d) && d > 0
            ? d : 0.80m;
    }
}