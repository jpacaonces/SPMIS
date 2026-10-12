using Microsoft.EntityFrameworkCore;
using Spmis.Web.Data.Entities;
using Spmis.Web.Models.ClassRecord;

namespace Spmis.Web.Services;


public class ExcuseService : IExcuseService
{
    private const string FinalizedMessage =
        "This class has been finalized. No further changes are allowed.";
    private static readonly string[] Filters = { "pending", "decided", "all" };

    private readonly SpmisDbContext _db;
    private readonly IAuditLogger _audit;
    private readonly IReasonService _reasons;
    private readonly ILogger<ExcuseService> _logger;

    public ExcuseService(SpmisDbContext db, IAuditLogger audit,
        IReasonService reasons, ILogger<ExcuseService> logger)
    {
        _db = db;
        _audit = audit;
        _reasons = reasons;
        _logger = logger;
    }

    private Task<uint?> FacultyIdAsync(Guid userId) =>
        _db.Faculties.AsNoTracking()
            .Where(f => f.UserId == userId && f.IsActive == true)
            .Select(f => (uint?)f.Id).FirstOrDefaultAsync();

    public async Task<ExcuseListViewModel> GetListAsync(Guid userId, string? filter)
    {
        // Whitelist the query-string value.
        var f = Filters.Contains(filter ?? "") ? filter! : "pending";
        var vm = new ExcuseListViewModel { Filter = f };

        var facultyId = await FacultyIdAsync(userId);
        if (facultyId is null)
        {
            vm.NoFacultyMessage = "Your account has no active faculty profile.";
            return vm;
        }

        var query =
            from d in _db.ExcuseDecisions.AsNoTracking()
            join c in _db.ClassOfferings on d.ClassOfferingId equals c.Id
            where c.FacultyId == facultyId && c.IsActive == true
                  && _db.AcademicTerms.Any(t => t.Id == c.TermId && t.IsCurrent == true)
            join l in _db.ExcuseLetters on d.LetterId equals l.Id
            join s in _db.Students on l.StudentId equals s.Id
            join u in _db.Users on s.UserId equals u.Id
            join sub in _db.Subjects on c.SubjectId equals sub.Id
            select new { d, c, l, s, u, sub };

        if (f == "pending") query = query.Where(x => x.d.Status == "Pending");
        else if (f == "decided") query = query.Where(x => x.d.Status != "Pending");

        var rows = await query
            .OrderBy(x => x.d.Status == "Pending" ? 0 : 1)
            .ThenByDescending(x => x.l.SubmittedAt)
            .Take(300)
            .Select(x => new ExcuseListItem
            {
                DecisionId = (int)x.d.Id,
                StudentNo = x.s.StudentNo,
                StudentName = x.u.LastName + ", " + x.u.FirstName,
                SubjectCode = x.sub.Code,
                DateFrom = x.l.DateFrom,
                DateTo = x.l.DateTo,
                Reason = x.l.Reason,
                SubmittedAt = x.l.SubmittedAt,
                Status = x.d.Status,
                MeetingsInRange = _db.ClassMeetings.Count(m =>
                    m.ClassOfferingId == x.c.Id && m.IsActive == true &&
                    m.MeetingDate >= x.l.DateFrom && m.MeetingDate <= x.l.DateTo)
            }).ToListAsync();

        vm.Items = rows;
        return vm;
    }

    private sealed record Loaded(ExcuseDecision D, ExcuseLetter L, ClassOffering C);

    // Decision -> class -> must belong to THIS faculty member (current term).
    private async Task<Loaded?> LoadOwnedAsync(Guid userId, int decisionId)
    {
        var facultyId = await FacultyIdAsync(userId);
        if (facultyId is null) return null;

        var id = (uint)decisionId;
        var d = await _db.ExcuseDecisions.FirstOrDefaultAsync(x => x.Id == id);   // tracked
        if (d is null) return null;

        var c = await _db.ClassOfferings.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == d.ClassOfferingId && x.FacultyId == facultyId
                                      && x.IsActive == true);
        if (c is null) return null;

        var l = await _db.ExcuseLetters.AsNoTracking().FirstAsync(x => x.Id == d.LetterId);
        return new Loaded(d, l, c);
    }

    public async Task<ExcuseDecideViewModel?> GetDecideAsync(Guid userId, int decisionId)
    {
        var x = await LoadOwnedAsync(userId, decisionId);
        if (x is null) return null;

        var student = await (
            from s in _db.Students.AsNoTracking()
            where s.Id == x.L.StudentId
            join u in _db.Users on s.UserId equals u.Id
            select new { s.StudentNo, u.LastName, u.FirstName }).FirstAsync();

        var header = await _db.Subjects.AsNoTracking().Where(s => s.Id == x.C.SubjectId)
            .Select(s => s.Code + " - " + s.Title).FirstAsync();

        // The student's enrollment in THIS class, to show the current marks.
        var enrollmentId = await _db.Enrollments.AsNoTracking()
            .Where(e => e.ClassOfferingId == x.C.Id && e.StudentId == x.L.StudentId)
            .Select(e => (uint?)e.Id).FirstOrDefaultAsync();

        var meetings = await _db.ClassMeetings.AsNoTracking()
            .Where(m => m.ClassOfferingId == x.C.Id && m.IsActive == true
                        && m.MeetingDate >= x.L.DateFrom && m.MeetingDate <= x.L.DateTo)
            .OrderBy(m => m.MeetingDate).ToListAsync();

        var marks = enrollmentId is null
            ? new Dictionary<uint, string>()
            : await _db.AttendanceRecords.AsNoTracking()
                .Where(r => r.EnrollmentId == enrollmentId
                            && meetings.Select(m => m.Id).Contains(r.MeetingId))
                .ToDictionaryAsync(r => r.MeetingId, r => r.Status);

        return new ExcuseDecideViewModel
        {
            DecisionId = decisionId,
            Header = header,
            StudentNo = student.StudentNo,
            StudentName = $"{student.LastName}, {student.FirstName}",
            DateFrom = x.L.DateFrom,
            DateTo = x.L.DateTo,
            LetterReason = x.L.Reason,
            GuardianName = x.L.GuardianName,
            GuardianRelationship = x.L.GuardianRelationship,
            SignatoryName = x.L.SignatoryName,
            FileName = x.L.OriginalFileName,
            SubmittedAt = x.L.SubmittedAt,
            Status = x.D.Status,
            DecisionReason = x.D.DecisionReason,
            DecidedAt = x.D.DecidedAt,
            Meetings = meetings.Select(m => new ExcuseMeetingItem
            {
                Date = m.MeetingDate,
                Topic = m.Topic,
                Mark = marks.TryGetValue(m.Id, out var s) ? s : "Not marked"
            }).ToList()
        };
    }

    public async Task<ExcuseResult> DecideAsync(Guid userId, ExcuseDecideViewModel vm)
    {
        var x = await LoadOwnedAsync(userId, vm.DecisionId);
        if (x is null) return new(false, "", "This letter is not available.");

        var termIsCurrent = await _db.AcademicTerms.AsNoTracking()
            .AnyAsync(t => t.Id == x.C.TermId && t.IsCurrent == true);
        if (!termIsCurrent)
            return new(false, "", "Decisions can only be made in the current term.");

        if (x.C.Status == "Finalized")
            return new(false, "", FinalizedMessage);

        // Decide from the CURRENT database state, not from the page.
        if (x.D.Status != "Pending")
            return new(false, "", $"This letter was already {x.D.Status.ToLowerInvariant()}. Nothing was changed.");

        if (vm.Approve is null)
            return new(false, nameof(vm.Approve), "Choose Approve or Reject.");

        var reason = await _reasons.ValidateAsync("ExcuseDecision", vm.Reason);
        if (!reason.Success)
            return new(false, "Reason.ReasonId", reason.Error!);

        var text = (reason.Text ?? "").Trim();
        if (text.Length < 10)
            return new(false, "Reason.ReasonId", "The reason must be at least 10 characters.");
        if (text.Length > 500)
            return new(false, "Reason.ReasonId", "The reason must be at most 500 characters.");

        var newStatus = vm.Approve == true ? "Approved" : "Rejected";
        var now = DateTime.Now;
        x.D.Status = newStatus;
        x.D.DecidedBy = userId;
        x.D.DecidedAt = new DateTime(now.Year, now.Month, now.Day, now.Hour, now.Minute, now.Second);
        x.D.DecisionReason = text;

        _audit.Add(vm.Approve == true ? "ExcuseApproved" : "ExcuseRejected",
            "ExcuseDecision", x.D.Id.ToString(),
            oldValues: new { status = "Pending" },
            newValues: new
            {
                status = newStatus,
                classOfferingId = x.C.Id,
                letterId = x.L.Id,
                dateFrom = x.L.DateFrom.ToString("yyyy-MM-dd"),
                dateTo = x.L.DateTo.ToString("yyyy-MM-dd")
            },
            reason: text);

        try
        {
            await _db.SaveChangesAsync();
        }
        catch (DbUpdateException ex)
        {
            _logger.LogError(ex, "Could not save excuse decision {Id}", x.D.Id);
            return new(false, "", "The decision could not be saved. Nothing was changed.");
        }
        return new(true, null, null);
    }
    public async Task<(string Stored, string Original)?> GetFileInfoAsync(Guid userId, int decisionId)
    {
        var x = await LoadOwnedAsync(userId, decisionId);
        return x is null ? null : (x.L.FilePath, x.L.OriginalFileName);
    }
}