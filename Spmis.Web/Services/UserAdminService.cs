using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Spmis.Web.Data.Auth;
using Spmis.Web.Data.Entities;
using Spmis.Web.Models.Admin;
using AcademicProgram = Spmis.Web.Data.Entities.Program;
using Spmis.Web.Models;

namespace Spmis.Web.Services;

public class UserAdminService : IUserAdminService
{
    private const int PageSize = 20;

    private readonly SpmisDbContext _db;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IAuditLogger _audit;
    private readonly ICurrentUser _currentUser;
    private readonly IReasonService _reasons;
    private readonly ILogger<UserAdminService> _logger;

    public UserAdminService(
        SpmisDbContext db,
        UserManager<ApplicationUser> userManager,
        IAuditLogger audit,
        ICurrentUser currentUser,
        IReasonService reasons,
        ILogger<UserAdminService> logger)
    {
        _db = db;
        _userManager = userManager;
        _audit = audit;
        _currentUser = currentUser;
        _reasons = reasons;
        _logger = logger;
    }

    // ---------------------------------------------------------------- list

    public async Task<UserListViewModel> GetPageAsync(
        string? search, string? role, string? status, int page)
    {
        var cleanRole = RoleNames.All.Contains(role) ? role : null;
        var cleanStatus = status is "active" or "inactive" ? status : null;
        var cleanSearch = string.IsNullOrWhiteSpace(search) ? null : search.Trim();

        var query = _db.Users.AsNoTracking();

        if (cleanSearch is not null)
        {
            query = query.Where(u =>
                u.UserName.Contains(cleanSearch) ||
                u.FirstName.Contains(cleanSearch) ||
                u.LastName.Contains(cleanSearch));
        }

        if (cleanRole is not null)
            query = query.Where(u => u.Roles.Any(r => r.Name == cleanRole));

        if (cleanStatus == "active")
            query = query.Where(u => u.IsActive == true);
        else if (cleanStatus == "inactive")
            query = query.Where(u => u.IsActive == false);

        var totalCount = await query.CountAsync();
        var totalPages = Math.Max(1, (int)Math.Ceiling(totalCount / (double)PageSize));
        var currentPage = Math.Clamp(page, 1, totalPages);

        var items = await query
            .OrderBy(u => u.LastName)
            .ThenBy(u => u.FirstName)
            .Skip((currentPage - 1) * PageSize)
            .Take(PageSize)
            .Select(u => new UserListItem
            {
                Id = u.Id,
                Email = u.UserName,
                FullName = u.LastName + ", " + u.FirstName,
                Roles = u.Roles.Select(r => r.Name).ToList(),
                IdentifierNo = u.Student != null ? u.Student.StudentNo
                             : u.Faculty != null ? u.Faculty.EmployeeNo
                             : null,
                IsActive = u.IsActive == true
            })
            .ToListAsync();

        return new UserListViewModel
        {
            Items = items,
            Search = cleanSearch,
            Role = cleanRole,
            Status = cleanStatus,
            Page = currentPage,
            TotalPages = totalPages,
            TotalCount = totalCount
        };
    }

    // ------------------------------------------------------------- lookups

    public async Task LoadLookupsAsync(UserCreateViewModel model)
    {
        model.Programs = await _db.Set<AcademicProgram>()
            .AsNoTracking()
            .Where(p => p.IsActive == true)
            .OrderBy(p => p.Code)
            .Select(p => new LookupItem { Id = (int)p.Id, Label = p.Code + " - " + p.Name })
            .ToListAsync();

        model.Departments = await _db.Set<Department>()
            .AsNoTracking()
            .Where(d => d.IsActive == true)
            .OrderBy(d => d.Code)
            .Select(d => new LookupItem { Id = (int)d.Id, Label = d.Code + " - " + d.Name })
            .ToListAsync();
    }

    // -------------------------------------------------------------- create

    public async Task<CreateUserResult> CreateAsync(UserCreateViewModel model)
    {
        var errors = new List<(string Key, string Message)>();

        var email = model.Email.Trim();
        var normalizedName = _userManager.NormalizeName(email);
        var normalizedEmail = _userManager.NormalizeEmail(email);

        // ---- 1. Check every rule BEFORE touching anything ----------------

        if (!RoleNames.All.Contains(model.Role))
            errors.Add(("Role", "Choose a valid role."));

        if (await _db.Users.AnyAsync(u =>
                u.NormalizedUserName == normalizedName ||
                u.NormalizedEmail == normalizedEmail))
        {
            errors.Add(("Email", "An account with this email already exists."));
        }

        AcademicProgram? program = null;
        Department? department = null;
        var studentNo = (model.StudentNo ?? string.Empty).Trim();
        var employeeNo = (model.EmployeeNo ?? string.Empty).Trim();

        if (model.Role is RoleNames.Student or RoleNames.ProgramHead)
        {
            var programs = await _db.Set<AcademicProgram>()
                .Where(p => p.IsActive == true).ToListAsync();
            program = programs.FirstOrDefault(p => p.Id == model.ProgramId);
            if (program is null)
                errors.Add(("ProgramId", "Choose a program."));
        }

        if (model.Role == RoleNames.Student)
        {
            if (studentNo.Length == 0)
                errors.Add(("StudentNo", "Enter the student number."));
            else if (await _db.Set<Student>().AnyAsync(s => s.StudentNo == studentNo))
                errors.Add(("StudentNo", "This student number is already used."));
        }

        if (model.Role == RoleNames.Faculty)
        {
            var departments = await _db.Set<Department>()
                .Where(d => d.IsActive == true).ToListAsync();
            department = departments.FirstOrDefault(d => d.Id == model.DepartmentId);
            if (department is null)
                errors.Add(("DepartmentId", "Choose a department."));

            if (employeeNo.Length == 0)
                errors.Add(("EmployeeNo", "Enter the employee number."));
            else if (await _db.Set<Faculty>().AnyAsync(f => f.EmployeeNo == employeeNo))
                errors.Add(("EmployeeNo", "This employee number is already used."));
        }

        // Identity's own password rules (length, upper, lower, digit).
        // The user object is only a throw-away argument for the hasher.
        var tempUser = new ApplicationUser { UserName = email, Email = email };
        foreach (var validator in _userManager.PasswordValidators)
        {
            var check = await validator.ValidateAsync(_userManager, tempUser, model.Password);
            if (!check.Succeeded)
                errors.AddRange(check.Errors.Select(e => ("Password", e.Description)));
        }

        var role = await _db.Roles.FirstOrDefaultAsync(r => r.Name == model.Role);
        if (role is null && RoleNames.All.Contains(model.Role))
            errors.Add(("Role", "That role is missing from the database."));

        if (errors.Count > 0)
            return new CreateUserResult(false, errors, null);

        // ---- 2. Build everything in memory --------------------------------

        var user = new User
        {
            Id = Guid.NewGuid(),
            UserName = email,
            NormalizedUserName = normalizedName,
            Email = email,
            NormalizedEmail = normalizedEmail,
            EmailConfirmed = true,
            PasswordHash = _userManager.PasswordHasher.HashPassword(tempUser, model.Password),
            SecurityStamp = Guid.NewGuid().ToString(),
            ConcurrencyStamp = Guid.NewGuid().ToString(),
            LockoutEnabled = true,
            FirstName = model.FirstName.Trim(),
            MiddleName = string.IsNullOrWhiteSpace(model.MiddleName) ? null : model.MiddleName.Trim(),
            LastName = model.LastName.Trim(),
            IsActive = true
        };

        user.Roles.Add(role!);

        switch (model.Role)
        {
            case RoleNames.Student:
                user.Student = new Student
                {
                    StudentNo = studentNo,
                    Program = program!,
                    IsActive = true
                };
                break;
            case RoleNames.Faculty:
                user.Faculty = new Faculty
                {
                    EmployeeNo = employeeNo,
                    Department = department!,
                    IsActive = true
                };
                break;
            case RoleNames.ProgramHead:
                user.ProgramHead = new ProgramHead
                {
                    Program = program!,
                    IsActive = true
                };
                break;
        }

        _db.Users.Add(user);

        // The log row joins the SAME unit of work. The password is NOT logged.
        _audit.Add("UserCreated", "User", user.Id.ToString(),
            newValues: new
            {
                email,
                role = model.Role,
                firstName = user.FirstName,
                lastName = user.LastName,
                studentNo = model.Role == RoleNames.Student ? studentNo : null,
                employeeNo = model.Role == RoleNames.Faculty ? employeeNo : null,
                program = program?.Code,
                department = department?.Code
            });

        // ---- 3. ONE save = ONE transaction --------------------------------
        try
        {
            await _db.SaveChangesAsync();
        }
        catch (DbUpdateException ex)
        {
            _logger.LogError(ex, "Creating user {Email} failed.", email);
            errors.Add((string.Empty,
                "The account could not be saved. The email or ID number may already exist. Nothing was created."));
            return new CreateUserResult(false, errors, null);
        }

        return new CreateUserResult(true, errors, user.Id);
    }

    // ------------------------------------------------------- status change

    public async Task<UserStatusViewModel?> GetStatusViewAsync(Guid id)
    {
        var user = await _db.Users
            .AsNoTracking()
            .Where(u => u.Id == id)
            .Select(u => new
            {
                u.Id,
                u.FirstName,
                u.LastName,
                u.UserName,
                IsActive = u.IsActive == true,
                Roles = u.Roles.Select(r => r.Name).ToList(),
                IsStudent = u.Student != null,
                IsFaculty = u.Faculty != null,
                IsProgramHead = u.ProgramHead != null
            })
            .FirstOrDefaultAsync();

        if (user is null)
            return null;

        var model = new UserStatusViewModel
        {
            UserId = user.Id,
            FullName = user.LastName + ", " + user.FirstName,
            Email = user.UserName,
            Roles = user.Roles,
            IsActive = user.IsActive
        };

        // Only deactivation needs warnings and blockers.
        if (!user.IsActive)
            return model;

        model.Blocker = await GetDeactivationBlockerAsync(
            user.Id, user.Roles.Contains(RoleNames.Administrator));

        if (user.IsStudent)
        {
            var enrolled = await _db.Set<Enrollment>().CountAsync(e =>
                e.Student.UserId == id &&
                e.Status == "Enrolled" &&
                e.ClassOffering.Term.IsCurrent == true);

            model.Warnings.Add(enrolled > 0
                ? $"This student is enrolled in {enrolled} class(es) this term. Their records are kept, but they will not appear in new enrollment lists."
                : "Their records are kept, but they will not appear in new enrollment lists.");
        }

        if (user.IsFaculty)
        {
            var classes = await _db.Set<ClassOffering>()
                .AsNoTracking()
                .Where(c => c.Faculty.UserId == id &&
                            c.IsActive == true &&
                            c.Term.IsCurrent == true)
                .Select(c => c.Subject.Code)
                .ToListAsync();

            if (classes.Count > 0)
            {
                model.Warnings.Add(
                    $"This instructor handles {classes.Count} active class(es) this term: {string.Join(", ", classes)}. Those classes will have no active instructor until reassigned.");
            }
        }

        if (user.IsProgramHead)
            model.Warnings.Add("This program will have no active program head.");

        return model;
    }

    public async Task<StatusChangeResult> ChangeStatusAsync(
        Guid id, bool activate, ReasonInput reasonInput)
    {
        var context = activate ? "Reactivate" : "Deactivate";

        var user = await _db.Users
            .Include(u => u.Roles)
            .Include(u => u.Student)
            .Include(u => u.Faculty)
            .Include(u => u.ProgramHead)
            .FirstOrDefaultAsync(u => u.Id == id);

        if (user is null)
            return Fail(string.Empty, "Account not found.");

        // The page the admin looked at may be stale (another admin, another tab).
        // Decide from the CURRENT database state, not from what the page said.
        var currentlyActive = user.IsActive == true;
        if (currentlyActive == activate)
        {
            return Fail(string.Empty, activate
                ? "This account is already active. Nothing was changed."
                : "This account is already inactive. Nothing was changed.");
        }

        if (!activate)
        {
            var blocker = await GetDeactivationBlockerAsync(
                user.Id, user.Roles.Any(r => r.Name == RoleNames.Administrator));
            if (blocker is not null)
                return Fail(string.Empty, blocker);
        }

        var reason = await _reasons.ValidateAsync(context, reasonInput);
        if (!reason.Success)
            return Fail("Reason.ReasonId", reason.Error!);

        // Account and profile flip together.
        user.IsActive = activate;
        if (user.Student is not null) user.Student.IsActive = activate;
        if (user.Faculty is not null) user.Faculty.IsActive = activate;
        if (user.ProgramHead is not null) user.ProgramHead.IsActive = activate;

        // A new security stamp invalidates sign-in cookies that already exist.
        user.SecurityStamp = Guid.NewGuid().ToString();
        user.ConcurrencyStamp = Guid.NewGuid().ToString();

        _audit.Add(
            activate ? "UserReactivated" : "UserDeactivated",
            "User",
            user.Id.ToString(),
            oldValues: new { isActive = currentlyActive },
            newValues: new { isActive = activate },
            reason: reason.Text);

        try
        {
            await _db.SaveChangesAsync();   // change + log row: one transaction
        }
        catch (DbUpdateException ex)
        {
            _logger.LogError(ex, "Status change for {UserId} failed.", id);
            return Fail(string.Empty, "The change could not be saved. Nothing was changed.");
        }

        return new StatusChangeResult(true, null, null,
            $"{(activate ? "Reactivated" : "Deactivated")} {user.UserName}.");
    }

    // Returns the reason a deactivation is NOT allowed, or null if it is allowed.
    private async Task<string?> GetDeactivationBlockerAsync(Guid id, bool isAdministrator)
    {
        if (id == _currentUser.UserId)
            return "You cannot deactivate your own account.";

        if (isAdministrator)
        {
            var otherActiveAdmins = await _db.Users.CountAsync(u =>
                u.Id != id &&
                u.IsActive == true &&
                u.Roles.Any(r => r.Name == RoleNames.Administrator));

            if (otherActiveAdmins == 0)
                return "This is the last active Administrator. Create or reactivate another one first.";
        }

        return null;
    }

    private static StatusChangeResult Fail(string key, string message) =>
        new(false, key, message, null);

    // ------------------------------------------------------------- edit

    public async Task<UserEditViewModel?> GetEditViewAsync(Guid id)
    {
        var user = await _db.Users
            .AsNoTracking()
            .Include(u => u.Student)
            .Include(u => u.Faculty)
            .Include(u => u.ProgramHead)
            .FirstOrDefaultAsync(u => u.Id == id);

        if (user is null)
            return null;

        int? programId = null;
        if (user.Student is not null)
            programId = (int)user.Student.ProgramId;
        else if (user.ProgramHead is not null)
            programId = (int)user.ProgramHead.ProgramId;

        int? departmentId = user.Faculty is not null
            ? (int)user.Faculty.DepartmentId
            : null;

        var model = new UserEditViewModel
        {
            UserId = user.Id,
            Email = user.UserName,
            FirstName = user.FirstName,
            MiddleName = user.MiddleName,
            LastName = user.LastName,
            StudentNo = user.Student?.StudentNo,
            EmployeeNo = user.Faculty?.EmployeeNo,
            ProgramId = programId,
            DepartmentId = departmentId,
            ConcurrencyStamp = user.ConcurrencyStamp ?? string.Empty
        };

        await PrepareEditViewAsync(model);
        return model;
    }

    public async Task PrepareEditViewAsync(UserEditViewModel model)
    {
        var user = await _db.Users
            .AsNoTracking()
            .Include(u => u.Roles)
            .Include(u => u.Student)
            .Include(u => u.Faculty)
            .Include(u => u.ProgramHead)
            .FirstOrDefaultAsync(u => u.Id == model.UserId);

        if (user is not null)
        {
            model.Roles = user.Roles.Select(r => r.Name).ToList();
            model.IsStudent = user.Student is not null;
            model.IsFaculty = user.Faculty is not null;
            model.IsProgramHead = user.ProgramHead is not null;
        }

        model.Programs = await _db.Set<AcademicProgram>()
            .AsNoTracking()
            .Where(p => p.IsActive == true)
            .OrderBy(p => p.Code)
            .Select(p => new LookupItem { Id = (int)p.Id, Label = p.Code + " - " + p.Name })
            .ToListAsync();

        model.Departments = await _db.Set<Department>()
            .AsNoTracking()
            .Where(d => d.IsActive == true)
            .OrderBy(d => d.Code)
            .Select(d => new LookupItem { Id = (int)d.Id, Label = d.Code + " - " + d.Name })
            .ToListAsync();
    }

    public async Task<UpdateUserResult> UpdateAsync(UserEditViewModel model)
    {
        var errors = new List<(string Key, string Message)>();

        var user = await _db.Users
            .Include(u => u.Student)
            .Include(u => u.Faculty)
            .Include(u => u.ProgramHead)
            .FirstOrDefaultAsync(u => u.Id == model.UserId);

        if (user is null)
            return FailUpdate(string.Empty, "Account not found.");

        // Optimistic concurrency: the form carries the version it was built from.
        // If the row has a different version now, someone else saved first.
        if ((user.ConcurrencyStamp ?? string.Empty) != model.ConcurrencyStamp)
        {
            return FailUpdate(string.Empty,
                "This account was changed by someone else while you were editing. Reload the page and try again. Nothing was saved.");
        }

        var email = model.Email.Trim();
        var firstName = model.FirstName.Trim();
        string? middleName = string.IsNullOrWhiteSpace(model.MiddleName)
            ? null : model.MiddleName.Trim();
        var lastName = model.LastName.Trim();
        var normalizedName = _userManager.NormalizeName(email);
        var normalizedEmail = _userManager.NormalizeEmail(email);

        if (await _db.Users.AnyAsync(u =>
                u.Id != user.Id &&
                (u.NormalizedUserName == normalizedName ||
                 u.NormalizedEmail == normalizedEmail)))
        {
            errors.Add(("Email", "Another account already uses this email."));
        }

        var student = user.Student;
        var faculty = user.Faculty;
        var programHead = user.ProgramHead;

        List<AcademicProgram>? programs = null;
        List<Department>? departments = null;
        AcademicProgram? program = null;
        Department? department = null;
        var studentNo = (model.StudentNo ?? string.Empty).Trim();
        var employeeNo = (model.EmployeeNo ?? string.Empty).Trim();

        // Only the profile this account really has is validated or changed,
        // whatever extra fields a tampered form might contain.
        if (student is not null || programHead is not null)
        {
            programs = await _db.Set<AcademicProgram>().ToListAsync();
            program = programs.FirstOrDefault(p => p.Id == model.ProgramId);
            if (program is null)
                errors.Add(("ProgramId", "Choose a program."));
        }

        if (student is not null)
        {
            if (studentNo.Length == 0)
                errors.Add(("StudentNo", "Enter the student number."));
            else if (await _db.Set<Student>().AnyAsync(s =>
                         s.Id != student.Id && s.StudentNo == studentNo))
                errors.Add(("StudentNo", "This student number is already used."));
        }

        if (faculty is not null)
        {
            departments = await _db.Set<Department>().ToListAsync();
            department = departments.FirstOrDefault(d => d.Id == model.DepartmentId);
            if (department is null)
                errors.Add(("DepartmentId", "Choose a department."));

            if (employeeNo.Length == 0)
                errors.Add(("EmployeeNo", "Enter the employee number."));
            else if (await _db.Set<Faculty>().AnyAsync(f =>
                         f.Id != faculty.Id && f.EmployeeNo == employeeNo))
                errors.Add(("EmployeeNo", "This employee number is already used."));
        }

        if (errors.Count > 0)
            return new UpdateUserResult(false, errors);

        // ---- What actually changed? (only changed fields are logged) --------
        var oldValues = new Dictionary<string, string?>();
        var newValues = new Dictionary<string, string?>();

        void Track(string name, string? oldValue, string? newValue)
        {
            if (!string.Equals(oldValue, newValue, StringComparison.Ordinal))
            {
                oldValues[name] = oldValue;
                newValues[name] = newValue;
            }
        }

        Track("email", user.UserName, email);
        Track("firstName", user.FirstName, firstName);
        Track("middleName", user.MiddleName, middleName);
        Track("lastName", user.LastName, lastName);

        if (student is not null)
        {
            Track("studentNo", student.StudentNo, studentNo);
            Track("program",
                programs!.FirstOrDefault(p => p.Id == student.ProgramId)?.Code,
                program!.Code);
        }

        if (programHead is not null)
        {
            Track("program",
                programs!.FirstOrDefault(p => p.Id == programHead.ProgramId)?.Code,
                program!.Code);
        }

        if (faculty is not null)
        {
            Track("employeeNo", faculty.EmployeeNo, employeeNo);
            Track("department",
                departments!.FirstOrDefault(d => d.Id == faculty.DepartmentId)?.Code,
                department!.Code);
        }

        if (oldValues.Count == 0)
            return FailUpdate(string.Empty, "No changes were made.");

        var reason = await _reasons.ValidateAsync("AdminCorrection", model.Reason);
        if (!reason.Success)
            return FailUpdate("Reason.ReasonId", reason.Error!);

        // ---- Apply ---------------------------------------------------------
        user.UserName = email;
        user.NormalizedUserName = normalizedName;
        user.Email = email;
        user.NormalizedEmail = normalizedEmail;
        user.FirstName = firstName;
        user.MiddleName = middleName;
        user.LastName = lastName;

        if (student is not null)
        {
            student.StudentNo = studentNo;
            student.Program = program!;
        }

        if (programHead is not null)
            programHead.Program = program!;

        if (faculty is not null)
        {
            faculty.EmployeeNo = employeeNo;
            faculty.Department = department!;
        }

        // New version of the row for the next editor.
        user.ConcurrencyStamp = Guid.NewGuid().ToString();

        // The sign-in name changed: end old sessions so the person signs in again.
        if (oldValues.ContainsKey("email"))
            user.SecurityStamp = Guid.NewGuid().ToString();

        _audit.Add("UserEdited", "User", user.Id.ToString(),
            oldValues: oldValues, newValues: newValues, reason: reason.Text);

        try
        {
            await _db.SaveChangesAsync();
        }
        catch (DbUpdateException ex)
        {
            _logger.LogError(ex, "Editing user {UserId} failed.", user.Id);
            return FailUpdate(string.Empty,
                "The changes could not be saved. The email or ID number may already exist. Nothing was changed.");
        }

        return new UpdateUserResult(true, errors);
    }

    private static UpdateUserResult FailUpdate(string key, string message) =>
        new(false, new List<(string Key, string Message)> { (key, message) });

    // --------------------------------------------------- password reset

    public async Task<UserPasswordResetViewModel?> GetPasswordResetViewAsync(Guid id)
    {
        return await _db.Users
            .AsNoTracking()
            .Where(u => u.Id == id)
            .Select(u => new UserPasswordResetViewModel
            {
                UserId = u.Id,
                FullName = u.LastName + ", " + u.FirstName,
                Email = u.UserName
            })
            .FirstOrDefaultAsync();
    }

    public async Task<UpdateUserResult> ResetPasswordAsync(UserPasswordResetViewModel model)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == model.UserId);
        if (user is null)
            return FailUpdate(string.Empty, "Account not found.");

        // Same rules as account creation: Identity's own validators.
        // The user object is only a throw-away argument for them.
        var tempUser = new ApplicationUser { UserName = user.UserName, Email = user.Email };
        var errors = new List<(string Key, string Message)>();

        foreach (var validator in _userManager.PasswordValidators)
        {
            var check = await validator.ValidateAsync(_userManager, tempUser, model.Password);
            if (!check.Succeeded)
                errors.AddRange(check.Errors.Select(e => ("Password", e.Description)));
        }

        if (errors.Count > 0)
            return new UpdateUserResult(false, errors);

        var reason = await _reasons.ValidateAsync("PasswordReset", model.Reason);
        if (!reason.Success)
            return FailUpdate("Reason.ReasonId", reason.Error!);

        user.PasswordHash = _userManager.PasswordHasher.HashPassword(tempUser, model.Password);

        // End every existing session, and clear any lockout so the person
        // can sign in with the new password straight away.
        user.SecurityStamp = Guid.NewGuid().ToString();
        user.ConcurrencyStamp = Guid.NewGuid().ToString();
        user.AccessFailedCount = 0;
        user.LockoutEnd = null;

        // The log says WHO and WHY. It never contains the password or its hash.
        _audit.Add("PasswordReset", "User", user.Id.ToString(),
            reason: reason.Text);

        try
        {
            await _db.SaveChangesAsync();
        }
        catch (DbUpdateException ex)
        {
            _logger.LogError(ex, "Password reset for {UserId} failed.", user.Id);
            return FailUpdate(string.Empty,
                "The password could not be changed. Nothing was changed.");
        }

        return new UpdateUserResult(true, errors);
    }
}