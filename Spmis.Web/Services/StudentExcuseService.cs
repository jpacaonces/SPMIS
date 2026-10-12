using Microsoft.EntityFrameworkCore;
using Spmis.Web.Data.Entities;
using Spmis.Web.Models.Portal;

namespace Spmis.Web.Services;

public class StudentExcuseService : IStudentExcuseService
{
    private const long MaxBytes = 5 * 1024 * 1024;

    private readonly SpmisDbContext _db;
    private readonly IAuditLogger _audit;
    private readonly IExcuseFileStore _files;
    private readonly ILogger<StudentExcuseService> _logger;

    public StudentExcuseService(SpmisDbContext db, IAuditLogger audit,
        IExcuseFileStore files, ILogger<StudentExcuseService> logger)
    {
        _db = db;
        _audit = audit;
        _files = files;
        _logger = logger;
    }

    private Task<uint?> StudentIdAsync(Guid userId) =>
        _db.Students.AsNoTracking()
            .Where(s => s.UserId == userId && s.IsActive == true)
            .Select(s => (uint?)s.Id).FirstOrDefaultAsync();

    public async Task<List<MyLetterItem>> GetMyLettersAsync(Guid userId)
    {
        var studentId = await StudentIdAsync(userId);
        if (studentId is null) return new();

        var letters = await _db.ExcuseLetters.AsNoTracking()
            .Where(l => l.StudentId == studentId)
            .OrderByDescending(l => l.SubmittedAt).Take(100).ToListAsync();
        var ids = letters.Select(l => l.Id).ToList();

        var decisions = await (
            from d in _db.ExcuseDecisions.AsNoTracking()
            where ids.Contains(d.LetterId)
            join c in _db.ClassOfferings on d.ClassOfferingId equals c.Id
            join s in _db.Subjects on c.SubjectId equals s.Id
            orderby s.Code
            select new { d.LetterId, s.Code, d.Status, d.DecisionReason, d.DecidedAt }).ToListAsync();

        return letters.Select(l => new MyLetterItem
        {
            LetterId = (int)l.Id,
            SubmittedAt = l.SubmittedAt,
            DateFrom = l.DateFrom,
            DateTo = l.DateTo,
            Reason = l.Reason,
            FileName = l.OriginalFileName,
            Decisions = decisions.Where(d => d.LetterId == l.Id).Select(d => new MyLetterDecision
            {
                SubjectCode = d.Code,
                Status = d.Status,
                DecisionReason = d.DecisionReason,
                DecidedAt = d.DecidedAt
            }).ToList()
        }).ToList();
    }

    // First bytes of the file must match what the extension claims.
    private static bool SignatureMatches(Stream s, string ext)
    {
        var head = new byte[8];
        var read = s.Read(head, 0, head.Length);
        return ext switch
        {
            ".pdf" => read >= 4 && head[0] == 0x25 && head[1] == 0x50 && head[2] == 0x44 && head[3] == 0x46,
            ".jpg" => read >= 3 && head[0] == 0xFF && head[1] == 0xD8 && head[2] == 0xFF,
            ".png" => read >= 8 && head[0] == 0x89 && head[1] == 0x50 && head[2] == 0x4E && head[3] == 0x47
                      && head[4] == 0x0D && head[5] == 0x0A && head[6] == 0x1A && head[7] == 0x0A,
            _ => false
        };
    }

    public async Task<LetterResult> SubmitAsync(Guid userId, ExcuseSubmitViewModel vm)
    {
        var studentId = await StudentIdAsync(userId);
        if (studentId is null) return new(false, "", "Your account has no active student profile.");

        var term = await _db.AcademicTerms.AsNoTracking().FirstOrDefaultAsync(t => t.IsCurrent == true);
        if (term is null) return new(false, "", "There is no current term.");

        // ---- text fields ----
        var guardian = (vm.GuardianName ?? "").Trim();
        var relationship = (vm.GuardianRelationship ?? "").Trim();
        var signatory = (vm.SignatoryName ?? "").Trim();
        var reason = (vm.Reason ?? "").Trim();
        if (guardian.Length == 0 || guardian.Length > 120)
            return new(false, nameof(vm.GuardianName), "Guardian name is required (up to 120 characters).");
        if (relationship.Length == 0 || relationship.Length > 40)
            return new(false, nameof(vm.GuardianRelationship), "Relationship is required (up to 40 characters).");
        if (signatory.Length == 0 || signatory.Length > 120)
            return new(false, nameof(vm.SignatoryName), "The signatory name is required (up to 120 characters).");
        if (reason.Length < 10 || reason.Length > 500)
            return new(false, nameof(vm.Reason), "The reason must be 10 to 500 characters.");

        // ---- dates ----
        if (vm.DateFrom is null || vm.DateTo is null)
            return new(false, nameof(vm.DateFrom), "Choose both dates.");
        var from = vm.DateFrom.Value;
        var to = vm.DateTo.Value;
        var today = DateOnly.FromDateTime(DateTime.Today);

        var bounds = await _db.TermPeriods.AsNoTracking()
            .Where(p => p.TermId == term.Id)
            .GroupBy(p => 1)
            .Select(g => new { Start = g.Min(p => p.StartDate), End = g.Max(p => p.EndDate) })
            .FirstOrDefaultAsync();
        if (bounds is null) return new(false, "", "The current term has no grading period dates.");

        if (to < from)
            return new(false, nameof(vm.DateTo), "The last date cannot be before the first date.");
        if (from < bounds.Start || to > bounds.End)
            return new(false, nameof(vm.DateFrom), "The dates must fall inside the current term.");
        if (to > today)
            return new(false, nameof(vm.DateTo), "An excuse letter covers absences that already happened.");
        if (to.DayNumber - from.DayNumber > 31)
            return new(false, nameof(vm.DateTo), "A letter can cover at most 32 days. Submit a second letter for more.");

        // ---- which classes get a Pending decision ----
        var classIds = await (
            from e in _db.Enrollments.AsNoTracking()
            where e.StudentId == studentId && e.Status == "Enrolled"
            join c in _db.ClassOfferings on e.ClassOfferingId equals c.Id
            where c.TermId == term.Id && c.IsActive == true && c.Status == "Open"
            select c.Id).ToListAsync();
        if (classIds.Count == 0)
            return new(false, "", "You are not enrolled in any open class this term.");

        // ---- file ----
        var file = vm.File;
        if (file is null || file.Length == 0)
            return new(false, nameof(vm.File), "Attach the proof file.");
        if (file.Length > MaxBytes)
            return new(false, nameof(vm.File), "The file is larger than 5 MB.");

        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (ext == ".jpeg") ext = ".jpg";
        if (ext is not (".pdf" or ".jpg" or ".png"))
            return new(false, nameof(vm.File), "Only PDF, JPG or PNG files are accepted.");

        await using (var probe = file.OpenReadStream())
        {
            if (!SignatureMatches(probe, ext))
                return new(false, nameof(vm.File), "The file content does not match its type. Upload a real PDF, JPG or PNG.");
        }

        var original = Path.GetFileName(file.FileName);
        if (original.Length > 255) original = original[^255..];

        // Save the file first; remove it again if the database save fails.
        var stored = await _files.SaveAsync(file, ext);
        try
        {
            var now = DateTime.Now;
            var letter = new ExcuseLetter
            {
                StudentId = studentId.Value,
                GuardianName = guardian,
                GuardianRelationship = relationship,
                SignatoryName = signatory,
                DateFrom = from,
                DateTo = to,
                Reason = reason,
                FilePath = stored,
                OriginalFileName = original,
                SubmittedAt = new DateTime(now.Year, now.Month, now.Day, now.Hour, now.Minute, now.Second)
            };

            // One transaction: the letter, its Pending decisions and the audit row.
            await using var tx = await _db.Database.BeginTransactionAsync();

            _db.ExcuseLetters.Add(letter);
            await _db.SaveChangesAsync();                    // gives the letter its id

            foreach (var cid in classIds)
                _db.ExcuseDecisions.Add(new ExcuseDecision
                {
                    LetterId = letter.Id,
                    ClassOfferingId = cid,
                    Status = "Pending"
                });

            _audit.Add("ExcuseLetterSubmitted", "ExcuseLetter", letter.Id.ToString(),
                newValues: new
                {
                    dateFrom = from.ToString("yyyy-MM-dd"),
                    dateTo = to.ToString("yyyy-MM-dd"),
                    classes = classIds.Count,
                    file = original
                });
            await _db.SaveChangesAsync();

            await tx.CommitAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Could not save excuse letter for student {Id}", studentId);
            _files.Delete(stored);
            return new(false, "", "The letter could not be saved. Nothing was submitted.");
        }

        return new(true, null, null);
    }

    public async Task<(string Stored, string Original)?> GetFileInfoAsync(Guid userId, int letterId)
    {
        var studentId = await StudentIdAsync(userId);
        if (studentId is null) return null;

        var id = (uint)letterId;
        var l = await _db.ExcuseLetters.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id && x.StudentId == studentId);
        return l is null ? null : (l.FilePath, l.OriginalFileName);
    }
}