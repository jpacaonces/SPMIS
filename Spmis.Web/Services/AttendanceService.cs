using Microsoft.EntityFrameworkCore;
using Spmis.Web.Data.Entities;
using Spmis.Web.Models.ClassRecord;

namespace Spmis.Web.Services;


public class AttendanceService : IAttendanceService
{
    private const string FinalizedMessage =
        "This class has been finalized. No further changes are allowed.";
    private static readonly string[] Statuses = { "Present", "Late", "Absent", "Excused" };

    private readonly SpmisDbContext _db;
    private readonly IAuditLogger _audit;
    private readonly IReasonService _reasons;
    private readonly ILogger<AttendanceService> _logger;

    public AttendanceService(SpmisDbContext db, IAuditLogger audit,
        IReasonService reasons, ILogger<AttendanceService> logger)
    {
        _db = db;
        _audit = audit;
        _reasons = reasons;
        _logger = logger;
    }

    private sealed record Owned(ClassMeeting M, ClassOffering O);

    // Meeting -> class -> must belong to THIS faculty member, Meetings mode only.
    private async Task<Owned?> OwnedAsync(Guid userId, int meetingId)
    {
        var facultyId = await _db.Faculties.AsNoTracking()
            .Where(f => f.UserId == userId && f.IsActive == true)
            .Select(f => (uint?)f.Id).FirstOrDefaultAsync();
        if (facultyId is null) return null;

        var id = (uint)meetingId;
        var m = await _db.ClassMeetings.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id);
        if (m is null) return null;

        var o = await _db.ClassOfferings.AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == m.ClassOfferingId && c.FacultyId == facultyId
                                      && c.IsActive == true && c.AttendanceMode == "Meetings");
        return o is null ? null : new Owned(m, o);
    }

    private async Task<List<(uint EnrollmentId, string StudentNo, string Name)>> EnrolledAsync(uint classId)
    {
        var rows = await (
            from e in _db.Enrollments.AsNoTracking()
            where e.ClassOfferingId == classId && e.Status == "Enrolled"
            join s in _db.Students on e.StudentId equals s.Id
            join u in _db.Users on s.UserId equals u.Id
            orderby s.StudentNo
            select new { e.Id, s.StudentNo, u.LastName, u.FirstName }).ToListAsync();
        return rows.Select(r => (r.Id, r.StudentNo, $"{r.LastName}, {r.FirstName}")).ToList();
    }

    private async Task<decimal> WarningRatioAsync()
    {
        var v = await _db.SystemSettings.AsNoTracking()
            .Where(s => s.SettingKey == "atrisk.absence_warning_ratio")
            .Select(s => s.SettingValue).FirstOrDefaultAsync();
        return v is null
            ? 0.80m
            : decimal.Parse(v, System.Globalization.CultureInfo.InvariantCulture);
    }

    private async Task FillHeaderAndFlagsAsync(Owned x, AttendanceViewModel vm,
        List<(uint EnrollmentId, string StudentNo, string Name)> students)
    {
        vm.MeetingId = (int)x.M.Id;
        vm.ClassOfferingId = (int)x.O.Id;
        vm.Date = x.M.MeetingDate;
        vm.Topic = x.M.Topic;
        vm.AbsenceLimitPercent = x.O.AbsenceLimitPercent;
        vm.Header = await _db.Subjects.AsNoTracking().Where(s => s.Id == x.O.SubjectId)
            .Select(s => s.Code + " - " + s.Title).FirstAsync();

        // Held meetings so far: active and dated today or earlier.
        var today = DateOnly.FromDateTime(DateTime.Today);
        var heldIds = await _db.ClassMeetings.AsNoTracking()
            .Where(m => m.ClassOfferingId == x.O.Id && m.IsActive == true && m.MeetingDate <= today)
            .Select(m => m.Id).ToListAsync();
        vm.HeldMeetings = heldIds.Count;

        // Absent marks, minus those inside an approved excuse letter for this class.
        var absentRows = await (
            from r in _db.AttendanceRecords.AsNoTracking()
            where heldIds.Contains(r.MeetingId) && r.Status == "Absent"
            join m in _db.ClassMeetings on r.MeetingId equals m.Id
            select new { r.EnrollmentId, m.MeetingDate }).ToListAsync();

        var approved = await (
            from d in _db.ExcuseDecisions.AsNoTracking()
            where d.ClassOfferingId == x.O.Id && d.Status == "Approved"
            join l in _db.ExcuseLetters on d.LetterId equals l.Id
            select new { l.StudentId, l.DateFrom, l.DateTo }).ToListAsync();

        var studentOf = await _db.Enrollments.AsNoTracking()
            .Where(e => e.ClassOfferingId == x.O.Id)
            .ToDictionaryAsync(e => e.Id, e => e.StudentId);

        var absences = absentRows
            .Where(a => !approved.Any(g =>
                g.StudentId == studentOf[a.EnrollmentId] &&
                a.MeetingDate >= g.DateFrom && a.MeetingDate <= g.DateTo))
            .GroupBy(a => a.EnrollmentId)
            .ToDictionary(g => g.Key, g => g.Count());

        var ratio = await WarningRatioAsync();
        var limit = x.O.AbsenceLimitPercent;

        var byId = students.ToDictionary(s => (int)s.EnrollmentId);
        foreach (var r in vm.Rows)
        {
            if (!byId.TryGetValue(r.EnrollmentId, out var st)) continue;
            r.StudentNo = st.StudentNo;
            r.StudentName = st.Name;
            r.Absences = absences.TryGetValue(st.EnrollmentId, out var n) ? n : 0;
            r.AbsencePercent = vm.HeldMeetings == 0
                ? 0m
                : Math.Round(r.Absences * 100m / vm.HeldMeetings, 1);
            r.Flag = vm.HeldMeetings == 0 ? ""
                : r.AbsencePercent > limit ? "Over limit"
                : r.AbsencePercent >= limit * ratio ? "Near limit"
                : "";
        }
    }

    public async Task<AttendanceViewModel?> LoadAsync(Guid userId, int meetingId)
    {
        var x = await OwnedAsync(userId, meetingId);
        if (x is null) return null;

        var students = await EnrolledAsync(x.O.Id);
        var records = await _db.AttendanceRecords.AsNoTracking()
            .Where(r => r.MeetingId == x.M.Id).ToDictionaryAsync(r => r.EnrollmentId);

        var vm = new AttendanceViewModel();
        foreach (var st in students)
        {
            records.TryGetValue(st.EnrollmentId, out var r);
            vm.Rows.Add(new AttendanceRowInput
            {
                EnrollmentId = (int)st.EnrollmentId,
                RecordId = r is null ? 0 : (int)r.Id,
                RowVersionTicks = r?.RowVersion.Ticks ?? 0,
                Status = r?.Status
            });
        }
        await FillHeaderAndFlagsAsync(x, vm, students);
        return vm;
    }

    public async Task<bool> FillDisplayAsync(Guid userId, AttendanceViewModel vm)
    {
        var x = await OwnedAsync(userId, vm.MeetingId);
        if (x is null) return false;
        await FillHeaderAndFlagsAsync(x, vm, await EnrolledAsync(x.O.Id));
        return true;
    }

    private sealed record Change(AttendanceRecord Record, bool IsNew, bool Recorded, string? OldStatus);

    public async Task<AttendanceSaveResult> SaveAsync(Guid userId, AttendanceViewModel vm)
    {
        var x = await OwnedAsync(userId, vm.MeetingId);
        if (x is null) return new(false, "", "This meeting is not available.", 0);

        if (x.O.Status == "Finalized")
            return new(false, "", FinalizedMessage, 0);

        if (x.M.IsActive != true)
            return new(false, "", "This meeting is cancelled. Reactivate it to mark attendance.", 0);

        if (x.M.MeetingDate > DateOnly.FromDateTime(DateTime.Today))
            return new(false, "", "Attendance cannot be marked for a future meeting.", 0);

        var termIsCurrent = await _db.AcademicTerms.AsNoTracking()
            .AnyAsync(t => t.Id == x.O.TermId && t.IsCurrent == true);
        if (!termIsCurrent)
            return new(false, "", "Attendance can only be changed in the current term.", 0);

        var enrolled = (await EnrolledAsync(x.O.Id)).Select(s => s.EnrollmentId).ToHashSet();
        var existing = await _db.AttendanceRecords.Where(r => r.MeetingId == x.M.Id)   // tracked
            .ToDictionaryAsync(r => r.EnrollmentId);

        var seen = new HashSet<int>();
        var changes = new List<Change>();

        foreach (var row in vm.Rows)
        {
            if (!seen.Add(row.EnrollmentId) || !enrolled.Contains((uint)row.EnrollmentId))
                return new(false, "", "The form contained a student who is not in this class. Reload the page.", 0);

            var status = string.IsNullOrWhiteSpace(row.Status) ? null : row.Status;
            if (status is not null && !Statuses.Contains(status))
                return new(false, "", "Invalid status in the form. Reload the page.", 0);

            existing.TryGetValue((uint)row.EnrollmentId, out var current);

            if (current is null && row.RecordId != 0)
                return new(false, "", "The attendance changed while this page was open. Reload and try again.", 0);
            if (current is not null &&
                (row.RecordId != (int)current.Id || row.RowVersionTicks != current.RowVersion.Ticks))
                return new(false, "", "A mark was changed by someone else while this page was open. " +
                    "Nothing was saved. Reload the page and try again.", 0);

            if (current is not null && status is null)
                return new(false, "", "A recorded mark cannot be cleared. Choose a status for every marked student.", 0);

            if (status is null) continue;                        // still not marked
            if (current is not null && current.Status == status) continue;   // unchanged

            var record = current ?? new AttendanceRecord
            {
                MeetingId = x.M.Id,
                EnrollmentId = (uint)row.EnrollmentId
            };
            record.Status = status;
            changes.Add(new Change(record, current is null, current is not null, current?.Status));
        }

        if (changes.Count == 0)
            return new(false, "", "Nothing was changed.", 0);

        string? reasonText = null;
        if (changes.Any(c => c.Recorded))
        {
            var reason = await _reasons.ValidateAsync("AttendanceEdit", vm.Reason);
            if (!reason.Success)
                return new(false, "Reason.ReasonId", reason.Error!, 0);
            reasonText = reason.Text;
        }

        foreach (var c in changes)
        {
            if (c.IsNew) _db.AttendanceRecords.Add(c.Record);

            _audit.Add(c.Recorded ? "AttendanceEdited" : "AttendanceMarked", "AttendanceRecord",
                $"{x.M.Id}:{c.Record.EnrollmentId}",
                oldValues: new { status = c.OldStatus },
                newValues: new { meetingId = x.M.Id, enrollmentId = c.Record.EnrollmentId, status = c.Record.Status },
                reason: c.Recorded ? reasonText : null);
        }

        try
        {
            await _db.SaveChangesAsync();    // all marks + all audit rows: one transaction
        }
        catch (DbUpdateConcurrencyException ex)
        {
            _logger.LogWarning(ex, "Concurrent attendance edit on meeting {Id}", x.M.Id);
            return new(false, "", "Someone changed this attendance at the same time. Nothing was saved. Reload and try again.", 0);
        }
        catch (DbUpdateException ex)
        {
            _logger.LogError(ex, "Could not save attendance for meeting {Id}", x.M.Id);
            return new(false, "", "The attendance could not be saved. Nothing was changed.", 0);
        }

        return new(true, null, null, changes.Count);
    }
}