using System;
using System.Collections.Generic;

namespace Spmis.Web.Data.Entities;

public partial class User
{
    public Guid Id { get; set; }

    public string UserName { get; set; } = null!;

    public string NormalizedUserName { get; set; } = null!;

    public string Email { get; set; } = null!;

    public string NormalizedEmail { get; set; } = null!;

    public bool EmailConfirmed { get; set; }

    public string? PasswordHash { get; set; }

    public string? SecurityStamp { get; set; }

    public string? ConcurrencyStamp { get; set; }

    public string? PhoneNumber { get; set; }

    public bool PhoneNumberConfirmed { get; set; }

    public bool TwoFactorEnabled { get; set; }

    public DateTime? LockoutEnd { get; set; }

    public bool? LockoutEnabled { get; set; }

    public int AccessFailedCount { get; set; }

    public string FirstName { get; set; } = null!;

    public string? MiddleName { get; set; }

    public string LastName { get; set; } = null!;

    public bool? IsActive { get; set; }

    public virtual ICollection<ActivityLog> ActivityLogs { get; set; } = new List<ActivityLog>();

    public virtual ICollection<ExcuseDecision> ExcuseDecisions { get; set; } = new List<ExcuseDecision>();

    public virtual Faculty? Faculty { get; set; }

    public virtual ICollection<FinalGrade> FinalGrades { get; set; } = new List<FinalGrade>();

    public virtual ProgramHead? ProgramHead { get; set; }

    public virtual Student? Student { get; set; }

    public virtual ICollection<SubmissionCheck> SubmissionChecks { get; set; } = new List<SubmissionCheck>();

    public virtual ICollection<UserClaim> UserClaims { get; set; } = new List<UserClaim>();

    public virtual ICollection<UserLogin> UserLogins { get; set; } = new List<UserLogin>();

    public virtual ICollection<UserToken> UserTokens { get; set; } = new List<UserToken>();

    public virtual ICollection<Role> Roles { get; set; } = new List<Role>();
}
