using Microsoft.EntityFrameworkCore;
using Spmis.Web.Data.Entities;
using Spmis.Web.Grading;
using Spmis.Web.Models.Portal;

namespace Spmis.Web.Services;

public class StudentPortalService : IStudentPortalService
{
    private readonly SpmisDbContext _db;
    private readonly IGradeCalculationService _grades;

    public StudentPortalService(SpmisDbContext db, IGradeCalculationService grades)
    {
        _db = db;
        _grades = grades;
    }

    private Task<uint?> StudentIdAsync(Guid userId) =>
        _db.Students.AsNoTracking()
            .Where(s => s.UserId == userId && s.IsActive == true)
            .Select(s => (uint?)s.Id).FirstOrDefaultAsync();

    public async Task<MyClassesViewModel> GetMyClassesAsync(Guid userId)
    {
        var vm = new MyClassesViewModel();

        var studentId = await StudentIdAsync(userId);
        if (studentId is null)
        {
            vm.Message = "Your account has no active student profile.";
            return vm;
        }

        var term = await _db.AcademicTerms.AsNoTracking().FirstOrDefaultAsync(t => t.IsCurrent == true);
        if (term is null)
        {
            vm.Message = "There is no current term.";
            return vm;
        }
        vm.TermLabel = $"{term.SchoolYear} Sem {term.Semester}";

        var rows = await (
            from e in _db.Enrollments.AsNoTracking()
            where e.StudentId == studentId && e.Status == "Enrolled"
            join c in _db.ClassOfferings on e.ClassOfferingId equals c.Id
            where c.TermId == term.Id && c.IsActive == true
            join sub in _db.Subjects on c.SubjectId equals sub.Id
            join f in _db.Faculties on c.FacultyId equals f.Id
            join fu in _db.Users on f.UserId equals fu.Id
            orderby sub.Code
            select new
            {
                e.Id,
                sub.Code,
                sub.Title,
                fu.LastName,
                fu.FirstName,
                c.Status
            }).ToListAsync();

        foreach (var r in rows)
        {
            var item = new MyClassItem
            {
                EnrollmentId = (int)r.Id,
                SubjectCode = r.Code,
                SubjectTitle = r.Title,
                FacultyName = $"{r.LastName}, {r.FirstName}",
                IsFinalized = r.Status == "Finalized"
            };

            if (item.IsFinalized)
            {
                var snap = await _db.FinalGrades.AsNoTracking()
                    .FirstOrDefaultAsync(f => f.EnrollmentId == r.Id);
                if (snap is not null)
                {
                    item.RawGrade = snap.RawGrade;
                    item.ReportedGrade = snap.ReportedGrade;
                    item.IsPassing = snap.ReportedGrade <= 3.00m;
                    item.Status = "Final";
                }
            }
            else
            {
                var row = await _grades.GetStudentGradeAsync((int)r.Id);
                if (row is not null)
                {
                    item.RawGrade = row.Semester.RawGrade;
                    item.ReportedGrade = row.Semester.ReportedGrade;
                    item.IsPassing = row.Semester.IsPassing;
                    item.Status = row.Semester.Status.ToString();
                }
            }
            vm.Items.Add(item);
        }
        return vm;
    }

    public async Task<MyClassDetailViewModel?> GetClassDetailAsync(Guid userId, int enrollmentId)
    {
        var studentId = await StudentIdAsync(userId);
        if (studentId is null) return null;

        // Layer 2 of authorization: the enrollment must be THIS student's.
        var id = (uint)enrollmentId;
        var e = await _db.Enrollments.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id && x.StudentId == studentId && x.Status == "Enrolled");
        if (e is null) return null;

        var c = await _db.ClassOfferings.AsNoTracking().FirstAsync(x => x.Id == e.ClassOfferingId);
        var header = await _db.Subjects.AsNoTracking().Where(s => s.Id == c.SubjectId)
            .Select(s => s.Code + " - " + s.Title).FirstAsync();
        var faculty = await (
            from f in _db.Faculties.AsNoTracking()
            where f.Id == c.FacultyId
            join u in _db.Users on f.UserId equals u.Id
            select u.LastName + ", " + u.FirstName).FirstAsync();

        var row = await _grades.GetStudentGradeAsync(enrollmentId);
        if (row is null) return null;

        var vm = new MyClassDetailViewModel
        {
            EnrollmentId = enrollmentId,
            Header = header,
            FacultyName = faculty,
            AttendanceMode = c.AttendanceMode,
            IsFinalized = c.Status == "Finalized",
            Status = row.Semester.Status.ToString(),
            RawGrade = row.Semester.RawGrade,
            ReportedGrade = row.Semester.ReportedGrade,
            IsPassing = row.Semester.IsPassing,
            Periods = row.Semester.Periods
        };

        if (vm.IsFinalized)
        {
            var snap = await _db.FinalGrades.AsNoTracking().FirstOrDefaultAsync(f => f.EnrollmentId == id);
            if (snap is not null)
            {
                vm.Status = "Final";
                vm.RawGrade = snap.RawGrade;
                vm.ReportedGrade = snap.ReportedGrade;
                vm.IsPassing = snap.ReportedGrade <= 3.00m;
            }
        }

        if (c.AttendanceMode == "Meetings")
        {
            var today = DateOnly.FromDateTime(DateTime.Today);
            var meetings = await _db.ClassMeetings.AsNoTracking()
                .Where(m => m.ClassOfferingId == c.Id && m.IsActive == true && m.MeetingDate <= today)
                .OrderBy(m => m.MeetingDate).ToListAsync();

            var marks = await _db.AttendanceRecords.AsNoTracking()
                .Where(r => r.EnrollmentId == id)
                .ToDictionaryAsync(r => r.MeetingId, r => r.Status);

            var approved = await (
                from d in _db.ExcuseDecisions.AsNoTracking()
                where d.ClassOfferingId == c.Id && d.Status == "Approved"
                join l in _db.ExcuseLetters on d.LetterId equals l.Id
                where l.StudentId == studentId
                select new { l.DateFrom, l.DateTo }).ToListAsync();

            vm.Attendance = meetings.Select(m =>
            {
                var mark = marks.TryGetValue(m.Id, out var s) ? s : "Not marked";
                return new MyAttendanceItem
                {
                    Date = m.MeetingDate,
                    Topic = m.Topic,
                    Mark = mark,
                    ExcusedByLetter = mark == "Absent" &&
                        approved.Any(a => m.MeetingDate >= a.DateFrom && m.MeetingDate <= a.DateTo)
                };
            }).ToList();
        }

        return vm;
    }
}