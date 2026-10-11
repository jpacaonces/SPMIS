using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace Spmis.Web.Data.Entities;

public partial class SpmisDbContext : DbContext
{
    public SpmisDbContext(DbContextOptions<SpmisDbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<AcademicTerm> AcademicTerms { get; set; }

    public virtual DbSet<ActivityLog> ActivityLogs { get; set; }

    public virtual DbSet<Assessment> Assessments { get; set; }

    public virtual DbSet<AttendanceRecord> AttendanceRecords { get; set; }

    public virtual DbSet<ChangeReason> ChangeReasons { get; set; }

    public virtual DbSet<ClassComponent> ClassComponents { get; set; }

    public virtual DbSet<ClassMeeting> ClassMeetings { get; set; }

    public virtual DbSet<ClassOffering> ClassOfferings { get; set; }

    public virtual DbSet<CurriculumSubject> CurriculumSubjects { get; set; }

    public virtual DbSet<Department> Departments { get; set; }

    public virtual DbSet<Enrollment> Enrollments { get; set; }

    public virtual DbSet<ExcuseDecision> ExcuseDecisions { get; set; }

    public virtual DbSet<ExcuseLetter> ExcuseLetters { get; set; }

    public virtual DbSet<Faculty> Faculties { get; set; }

    public virtual DbSet<FinalGrade> FinalGrades { get; set; }

    public virtual DbSet<GradingPeriod> GradingPeriods { get; set; }

    public virtual DbSet<GradingTemplate> GradingTemplates { get; set; }

    public virtual DbSet<Program> Programs { get; set; }

    public virtual DbSet<ProgramHead> ProgramHeads { get; set; }

    public virtual DbSet<Role> Roles { get; set; }

    public virtual DbSet<RoleClaim> RoleClaims { get; set; }

    public virtual DbSet<Score> Scores { get; set; }

    public virtual DbSet<Section> Sections { get; set; }

    public virtual DbSet<Shift> Shifts { get; set; }

    public virtual DbSet<Student> Students { get; set; }

    public virtual DbSet<Subject> Subjects { get; set; }

    public virtual DbSet<SubmissionCheck> SubmissionChecks { get; set; }

    public virtual DbSet<SystemSetting> SystemSettings { get; set; }

    public virtual DbSet<TemplateComponent> TemplateComponents { get; set; }

    public virtual DbSet<TermPeriod> TermPeriods { get; set; }

    public virtual DbSet<TransmutationRow> TransmutationRows { get; set; }

    public virtual DbSet<TransmutationTable> TransmutationTables { get; set; }

    public virtual DbSet<User> Users { get; set; }

    public virtual DbSet<UserClaim> UserClaims { get; set; }

    public virtual DbSet<UserLogin> UserLogins { get; set; }

    public virtual DbSet<UserToken> UserTokens { get; set; }

    public virtual DbSet<YearLevelShift> YearLevelShifts { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder
            .UseCollation("utf8mb4_unicode_ci")
            .HasCharSet("utf8mb4");

        modelBuilder.Entity<AcademicTerm>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.ToTable("academic_terms");

            entity.HasIndex(e => new { e.SchoolYear, e.Semester }, "uq_terms_year_sem").IsUnique();

            entity.Property(e => e.Id)
                .HasColumnType("int(10) unsigned")
                .HasColumnName("id");
            entity.Property(e => e.EndDate).HasColumnName("end_date");
            entity.Property(e => e.IsCurrent).HasColumnName("is_current");
            entity.Property(e => e.SchoolYear)
                .HasMaxLength(9)
                .IsFixedLength()
                .HasColumnName("school_year");
            entity.Property(e => e.Semester)
                .HasColumnType("tinyint(3) unsigned")
                .HasColumnName("semester");
            entity.Property(e => e.StartDate).HasColumnName("start_date");
        });

        modelBuilder.Entity<ActivityLog>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.ToTable("activity_logs");

            entity.HasIndex(e => e.ActionType, "ix_logs_action");

            entity.HasIndex(e => new { e.EntityType, e.EntityId }, "ix_logs_entity");

            entity.HasIndex(e => e.OccurredAt, "ix_logs_time");

            entity.HasIndex(e => e.UserId, "ix_logs_user");

            entity.Property(e => e.Id)
                .HasColumnType("bigint(20) unsigned")
                .HasColumnName("id");
            entity.Property(e => e.ActionType)
                .HasMaxLength(40)
                .HasColumnName("action_type");
            entity.Property(e => e.EntityId)
                .HasMaxLength(40)
                .HasColumnName("entity_id");
            entity.Property(e => e.EntityType)
                .HasMaxLength(40)
                .HasColumnName("entity_type");
            entity.Property(e => e.NewValues)
                .HasColumnType("json")
                .HasColumnName("new_values");
            entity.Property(e => e.OccurredAt)
                .HasMaxLength(6)
                .HasDefaultValueSql("current_timestamp(6)")
                .HasColumnName("occurred_at");
            entity.Property(e => e.OldValues)
                .HasColumnType("json")
                .HasColumnName("old_values");
            entity.Property(e => e.Reason)
                .HasMaxLength(500)
                .HasColumnName("reason");
            entity.Property(e => e.Role)
                .HasMaxLength(30)
                .HasColumnName("role");
            entity.Property(e => e.UserId).HasColumnName("user_id");

            entity.HasOne(d => d.User).WithMany(p => p.ActivityLogs)
                .HasForeignKey(d => d.UserId)
                .HasConstraintName("fk_logs_user");
        });

        modelBuilder.Entity<Assessment>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.ToTable("assessments");

            entity.HasIndex(e => e.GradingPeriodId, "fk_assessments_period");

            entity.HasIndex(e => new { e.ClassComponentId, e.GradingPeriodId, e.Title }, "uq_assessments_period_title").IsUnique();

            entity.Property(e => e.Id)
                .HasColumnType("int(10) unsigned")
                .HasColumnName("id");
            entity.Property(e => e.ClassComponentId)
                .HasColumnType("int(10) unsigned")
                .HasColumnName("class_component_id");
            entity.Property(e => e.DueDate)
                .HasColumnType("datetime")
                .HasColumnName("due_date");
            entity.Property(e => e.GradingPeriodId)
                .HasColumnType("int(10) unsigned")
                .HasColumnName("grading_period_id");
            entity.Property(e => e.IsActive)
                .IsRequired()
                .HasDefaultValueSql("'1'")
                .HasColumnName("is_active");
            entity.Property(e => e.IsRequired)
                .IsRequired()
                .HasDefaultValueSql("'1'")
                .HasColumnName("is_required");
            entity.Property(e => e.MaxScore)
                .HasPrecision(6, 2)
                .HasColumnName("max_score");
            entity.Property(e => e.PassingPercent)
                .HasPrecision(5, 2)
                .HasDefaultValueSql("'75.00'")
                .HasColumnName("passing_percent");
            entity.Property(e => e.Title)
                .HasMaxLength(150)
                .HasColumnName("title");

            entity.HasOne(d => d.ClassComponent).WithMany(p => p.Assessments)
                .HasForeignKey(d => d.ClassComponentId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_assessments_component");

            entity.HasOne(d => d.GradingPeriod).WithMany(p => p.Assessments)
                .HasForeignKey(d => d.GradingPeriodId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_assessments_period");
        });

        modelBuilder.Entity<AttendanceRecord>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.ToTable("attendance_records");

            entity.HasIndex(e => e.EnrollmentId, "fk_ar_enrollment");

            entity.HasIndex(e => new { e.MeetingId, e.EnrollmentId }, "uq_attendance").IsUnique();

            entity.Property(e => e.Id)
                .HasColumnType("int(10) unsigned")
                .HasColumnName("id");
            entity.Property(e => e.EnrollmentId)
                .HasColumnType("int(10) unsigned")
                .HasColumnName("enrollment_id");
            entity.Property(e => e.MeetingId)
                .HasColumnType("int(10) unsigned")
                .HasColumnName("meeting_id");
            entity.Property(e => e.RowVersion)
                .ValueGeneratedOnAddOrUpdate()
                .HasDefaultValueSql("current_timestamp(6)")
                .HasColumnType("timestamp(6)")
                .HasColumnName("row_version");
            entity.Property(e => e.Status)
                .HasMaxLength(10)
                .HasColumnName("status");

            entity.HasOne(d => d.Enrollment).WithMany(p => p.AttendanceRecords)
                .HasForeignKey(d => d.EnrollmentId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_ar_enrollment");

            entity.HasOne(d => d.Meeting).WithMany(p => p.AttendanceRecords)
                .HasForeignKey(d => d.MeetingId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_ar_meeting");
        });

        modelBuilder.Entity<ChangeReason>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.ToTable("change_reasons");

            entity.HasIndex(e => new { e.Context, e.Text }, "uq_change_reasons").IsUnique();

            entity.Property(e => e.Id)
                .HasColumnType("int(10) unsigned")
                .HasColumnName("id");
            entity.Property(e => e.Context)
                .HasMaxLength(30)
                .HasColumnName("context");
            entity.Property(e => e.IsActive)
                .IsRequired()
                .HasDefaultValueSql("'1'")
                .HasColumnName("is_active");
            entity.Property(e => e.Text)
                .HasMaxLength(100)
                .HasColumnName("text");
        });

        modelBuilder.Entity<ClassComponent>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.ToTable("class_components");

            entity.HasIndex(e => new { e.ClassOfferingId, e.Name }, "uq_cc_name").IsUnique();

            entity.Property(e => e.Id)
                .HasColumnType("int(10) unsigned")
                .HasColumnName("id");
            entity.Property(e => e.ClassOfferingId)
                .HasColumnType("int(10) unsigned")
                .HasColumnName("class_offering_id");
            entity.Property(e => e.ComponentType)
                .HasMaxLength(15)
                .HasColumnName("component_type");
            entity.Property(e => e.GroupType)
                .HasMaxLength(12)
                .HasColumnName("group_type");
            entity.Property(e => e.IsActive)
                .IsRequired()
                .HasDefaultValueSql("'1'")
                .HasColumnName("is_active");
            entity.Property(e => e.IsAttendance).HasColumnName("is_attendance");
            entity.Property(e => e.Name)
                .HasMaxLength(100)
                .HasColumnName("name");
            entity.Property(e => e.SortOrder)
                .HasDefaultValueSql("'1'")
                .HasColumnType("tinyint(3) unsigned")
                .HasColumnName("sort_order");
            entity.Property(e => e.WeightPct)
                .HasPrecision(5, 2)
                .HasColumnName("weight_pct");

            entity.HasOne(d => d.ClassOffering).WithMany(p => p.ClassComponents)
                .HasForeignKey(d => d.ClassOfferingId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_cc_offering");
        });

        modelBuilder.Entity<ClassMeeting>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.ToTable("class_meetings");

            entity.HasIndex(e => e.LinkedAssessmentId, "fk_cm_assessment");

            entity.HasIndex(e => new { e.ClassOfferingId, e.MeetingDate }, "uq_class_meetings").IsUnique();

            entity.Property(e => e.Id)
                .HasColumnType("int(10) unsigned")
                .HasColumnName("id");
            entity.Property(e => e.ClassOfferingId)
                .HasColumnType("int(10) unsigned")
                .HasColumnName("class_offering_id");
            entity.Property(e => e.IsActive)
                .IsRequired()
                .HasDefaultValueSql("'1'")
                .HasColumnName("is_active");
            entity.Property(e => e.LinkedAssessmentId)
                .HasColumnType("int(10) unsigned")
                .HasColumnName("linked_assessment_id");
            entity.Property(e => e.MeetingDate).HasColumnName("meeting_date");
            entity.Property(e => e.Topic)
                .HasMaxLength(150)
                .HasColumnName("topic");

            entity.HasOne(d => d.ClassOffering).WithMany(p => p.ClassMeetings)
                .HasForeignKey(d => d.ClassOfferingId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_cm_offering");

            entity.HasOne(d => d.LinkedAssessment).WithMany(p => p.ClassMeetings)
                .HasForeignKey(d => d.LinkedAssessmentId)
                .HasConstraintName("fk_cm_assessment");
        });

        modelBuilder.Entity<ClassOffering>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.ToTable("class_offerings");

            entity.HasIndex(e => e.FacultyId, "fk_co_faculty");

            entity.HasIndex(e => e.SectionId, "fk_co_section");

            entity.HasIndex(e => e.SubjectId, "fk_co_subject");

            entity.HasIndex(e => e.TransmutationTableId, "fk_co_transmutation");

            entity.HasIndex(e => new { e.TermId, e.SubjectId, e.SectionId }, "uq_class_offerings").IsUnique();

            entity.Property(e => e.Id)
                .HasColumnType("int(10) unsigned")
                .HasColumnName("id");
            entity.Property(e => e.AbsenceLimitPercent)
                .HasPrecision(5, 2)
                .HasDefaultValueSql("'20.00'")
                .HasColumnName("absence_limit_percent");
            entity.Property(e => e.AttendanceMode)
                .HasMaxLength(15)
                .HasDefaultValueSql("'Meetings'")
                .HasColumnName("attendance_mode");
            entity.Property(e => e.ClassStandingWeight)
                .HasPrecision(5, 2)
                .HasDefaultValueSql("'60.00'")
                .HasColumnName("class_standing_weight");
            entity.Property(e => e.FacultyId)
                .HasColumnType("int(10) unsigned")
                .HasColumnName("faculty_id");
            entity.Property(e => e.IsActive)
                .IsRequired()
                .HasDefaultValueSql("'1'")
                .HasColumnName("is_active");
            entity.Property(e => e.MajorExamWeight)
                .HasPrecision(5, 2)
                .HasDefaultValueSql("'40.00'")
                .HasColumnName("major_exam_weight");
            entity.Property(e => e.SectionId)
                .HasColumnType("int(10) unsigned")
                .HasColumnName("section_id");
            entity.Property(e => e.Status)
                .HasMaxLength(10)
                .HasDefaultValueSql("'Open'")
                .HasColumnName("status");
            entity.Property(e => e.SubjectId)
                .HasColumnType("int(10) unsigned")
                .HasColumnName("subject_id");
            entity.Property(e => e.TermId)
                .HasColumnType("int(10) unsigned")
                .HasColumnName("term_id");
            entity.Property(e => e.TransmutationTableId)
                .HasColumnType("int(10) unsigned")
                .HasColumnName("transmutation_table_id");

            entity.HasOne(d => d.Faculty).WithMany(p => p.ClassOfferings)
                .HasForeignKey(d => d.FacultyId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_co_faculty");

            entity.HasOne(d => d.Section).WithMany(p => p.ClassOfferings)
                .HasForeignKey(d => d.SectionId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_co_section");

            entity.HasOne(d => d.Subject).WithMany(p => p.ClassOfferings)
                .HasForeignKey(d => d.SubjectId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_co_subject");

            entity.HasOne(d => d.Term).WithMany(p => p.ClassOfferings)
                .HasForeignKey(d => d.TermId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_co_term");

            entity.HasOne(d => d.TransmutationTable).WithMany(p => p.ClassOfferings)
                .HasForeignKey(d => d.TransmutationTableId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_co_transmutation");
        });

        modelBuilder.Entity<CurriculumSubject>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.ToTable("curriculum_subjects");

            entity.HasIndex(e => e.SubjectId, "fk_curr_subject");

            entity.HasIndex(e => new { e.ProgramId, e.YearLevel, e.Semester }, "ix_curriculum_slot");

            entity.HasIndex(e => new { e.ProgramId, e.SubjectId }, "uq_curriculum").IsUnique();

            entity.Property(e => e.Id)
                .HasColumnType("int(10) unsigned")
                .HasColumnName("id");
            entity.Property(e => e.IsActive)
                .IsRequired()
                .HasDefaultValueSql("'1'")
                .HasColumnName("is_active");
            entity.Property(e => e.ProgramId)
                .HasColumnType("int(10) unsigned")
                .HasColumnName("program_id");
            entity.Property(e => e.Semester)
                .HasColumnType("tinyint(3) unsigned")
                .HasColumnName("semester");
            entity.Property(e => e.SubjectId)
                .HasColumnType("int(10) unsigned")
                .HasColumnName("subject_id");
            entity.Property(e => e.YearLevel)
                .HasColumnType("tinyint(3) unsigned")
                .HasColumnName("year_level");

            entity.HasOne(d => d.Program).WithMany(p => p.CurriculumSubjects)
                .HasForeignKey(d => d.ProgramId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_curr_program");

            entity.HasOne(d => d.Subject).WithMany(p => p.CurriculumSubjects)
                .HasForeignKey(d => d.SubjectId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_curr_subject");
        });

        modelBuilder.Entity<Department>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.ToTable("departments");

            entity.HasIndex(e => e.Code, "uq_departments_code").IsUnique();

            entity.Property(e => e.Id)
                .HasColumnType("int(10) unsigned")
                .HasColumnName("id");
            entity.Property(e => e.Code)
                .HasMaxLength(10)
                .HasColumnName("code");
            entity.Property(e => e.IsActive)
                .IsRequired()
                .HasDefaultValueSql("'1'")
                .HasColumnName("is_active");
            entity.Property(e => e.Name)
                .HasMaxLength(100)
                .HasColumnName("name");
        });

        modelBuilder.Entity<Enrollment>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.ToTable("enrollments");

            entity.HasIndex(e => e.ClassOfferingId, "fk_en_offering");

            entity.HasIndex(e => new { e.StudentId, e.ClassOfferingId }, "uq_enrollments").IsUnique();

            entity.Property(e => e.Id)
                .HasColumnType("int(10) unsigned")
                .HasColumnName("id");
            entity.Property(e => e.ClassOfferingId)
                .HasColumnType("int(10) unsigned")
                .HasColumnName("class_offering_id");
            entity.Property(e => e.EnrolledAt)
                .HasDefaultValueSql("current_timestamp()")
                .HasColumnType("datetime")
                .HasColumnName("enrolled_at");
            entity.Property(e => e.Status)
                .HasMaxLength(10)
                .HasDefaultValueSql("'Enrolled'")
                .HasColumnName("status");
            entity.Property(e => e.StudentId)
                .HasColumnType("int(10) unsigned")
                .HasColumnName("student_id");

            entity.HasOne(d => d.ClassOffering).WithMany(p => p.Enrollments)
                .HasForeignKey(d => d.ClassOfferingId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_en_offering");

            entity.HasOne(d => d.Student).WithMany(p => p.Enrollments)
                .HasForeignKey(d => d.StudentId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_en_student");
        });

        modelBuilder.Entity<ExcuseDecision>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.ToTable("excuse_decisions");

            entity.HasIndex(e => e.DecidedBy, "fk_ed_decider");

            entity.HasIndex(e => e.ClassOfferingId, "fk_ed_offering");

            entity.HasIndex(e => new { e.LetterId, e.ClassOfferingId }, "uq_excuse_decisions").IsUnique();

            entity.Property(e => e.Id)
                .HasColumnType("int(10) unsigned")
                .HasColumnName("id");
            entity.Property(e => e.ClassOfferingId)
                .HasColumnType("int(10) unsigned")
                .HasColumnName("class_offering_id");
            entity.Property(e => e.DecidedAt)
                .HasColumnType("datetime")
                .HasColumnName("decided_at");
            entity.Property(e => e.DecidedBy).HasColumnName("decided_by");
            entity.Property(e => e.DecisionReason)
                .HasMaxLength(500)
                .HasColumnName("decision_reason");
            entity.Property(e => e.LetterId)
                .HasColumnType("int(10) unsigned")
                .HasColumnName("letter_id");
            entity.Property(e => e.Status)
                .HasMaxLength(10)
                .HasDefaultValueSql("'Pending'")
                .HasColumnName("status");

            entity.HasOne(d => d.ClassOffering).WithMany(p => p.ExcuseDecisions)
                .HasForeignKey(d => d.ClassOfferingId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_ed_offering");

            entity.HasOne(d => d.DecidedByNavigation).WithMany(p => p.ExcuseDecisions)
                .HasForeignKey(d => d.DecidedBy)
                .HasConstraintName("fk_ed_decider");

            entity.HasOne(d => d.Letter).WithMany(p => p.ExcuseDecisions)
                .HasForeignKey(d => d.LetterId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_ed_letter");
        });

        modelBuilder.Entity<ExcuseLetter>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.ToTable("excuse_letters");

            entity.HasIndex(e => e.StudentId, "ix_excuse_letters_student");

            entity.Property(e => e.Id)
                .HasColumnType("int(10) unsigned")
                .HasColumnName("id");
            entity.Property(e => e.DateFrom).HasColumnName("date_from");
            entity.Property(e => e.DateTo).HasColumnName("date_to");
            entity.Property(e => e.FilePath)
                .HasMaxLength(255)
                .HasColumnName("file_path");
            entity.Property(e => e.GuardianName)
                .HasMaxLength(120)
                .HasColumnName("guardian_name");
            entity.Property(e => e.GuardianRelationship)
                .HasMaxLength(40)
                .HasColumnName("guardian_relationship");
            entity.Property(e => e.OriginalFileName)
                .HasMaxLength(255)
                .HasColumnName("original_file_name");
            entity.Property(e => e.Reason)
                .HasMaxLength(500)
                .HasColumnName("reason");
            entity.Property(e => e.SignatoryName)
                .HasMaxLength(120)
                .HasColumnName("signatory_name");
            entity.Property(e => e.StudentId)
                .HasColumnType("int(10) unsigned")
                .HasColumnName("student_id");
            entity.Property(e => e.SubmittedAt)
                .HasDefaultValueSql("current_timestamp()")
                .HasColumnType("datetime")
                .HasColumnName("submitted_at");

            entity.HasOne(d => d.Student).WithMany(p => p.ExcuseLetters)
                .HasForeignKey(d => d.StudentId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_el_student");
        });

        modelBuilder.Entity<Faculty>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.ToTable("faculty");

            entity.HasIndex(e => e.DepartmentId, "fk_faculty_department");

            entity.HasIndex(e => e.EmployeeNo, "uq_faculty_no").IsUnique();

            entity.HasIndex(e => e.UserId, "uq_faculty_user").IsUnique();

            entity.Property(e => e.Id)
                .HasColumnType("int(10) unsigned")
                .HasColumnName("id");
            entity.Property(e => e.DepartmentId)
                .HasColumnType("int(10) unsigned")
                .HasColumnName("department_id");
            entity.Property(e => e.EmployeeNo)
                .HasMaxLength(20)
                .HasColumnName("employee_no");
            entity.Property(e => e.IsActive)
                .IsRequired()
                .HasDefaultValueSql("'1'")
                .HasColumnName("is_active");
            entity.Property(e => e.UserId).HasColumnName("user_id");

            entity.HasOne(d => d.Department).WithMany(p => p.Faculties)
                .HasForeignKey(d => d.DepartmentId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_faculty_department");

            entity.HasOne(d => d.User).WithOne(p => p.Faculty)
                .HasForeignKey<Faculty>(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_faculty_user");
        });

        modelBuilder.Entity<FinalGrade>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.ToTable("final_grades");

            entity.HasIndex(e => e.FinalizedBy, "fk_fg_user");

            entity.HasIndex(e => e.EnrollmentId, "uq_final_grades_enrollment").IsUnique();

            entity.Property(e => e.Id)
                .HasColumnType("int(10) unsigned")
                .HasColumnName("id");
            entity.Property(e => e.EnrollmentId)
                .HasColumnType("int(10) unsigned")
                .HasColumnName("enrollment_id");
            entity.Property(e => e.FinalizedAt)
                .HasDefaultValueSql("current_timestamp()")
                .HasColumnType("datetime")
                .HasColumnName("finalized_at");
            entity.Property(e => e.FinalizedBy).HasColumnName("finalized_by");
            entity.Property(e => e.RawGrade)
                .HasPrecision(5, 2)
                .HasColumnName("raw_grade");
            entity.Property(e => e.ReportedGrade)
                .HasPrecision(3, 2)
                .HasColumnName("reported_grade");

            entity.HasOne(d => d.Enrollment).WithOne(p => p.FinalGrade)
                .HasForeignKey<FinalGrade>(d => d.EnrollmentId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_fg_enrollment");

            entity.HasOne(d => d.FinalizedByNavigation).WithMany(p => p.FinalGrades)
                .HasForeignKey(d => d.FinalizedBy)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_fg_user");
        });

        modelBuilder.Entity<GradingPeriod>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.ToTable("grading_periods");

            entity.HasIndex(e => e.Code, "uq_grading_periods_code").IsUnique();

            entity.HasIndex(e => e.SortOrder, "uq_grading_periods_order").IsUnique();

            entity.Property(e => e.Id)
                .HasColumnType("int(10) unsigned")
                .HasColumnName("id");
            entity.Property(e => e.Code)
                .HasMaxLength(10)
                .HasColumnName("code");
            entity.Property(e => e.Name)
                .HasMaxLength(20)
                .HasColumnName("name");
            entity.Property(e => e.SortOrder)
                .HasColumnType("tinyint(3) unsigned")
                .HasColumnName("sort_order");
        });

        modelBuilder.Entity<GradingTemplate>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.ToTable("grading_templates");

            entity.HasIndex(e => e.DepartmentId, "fk_gt_department");

            entity.HasIndex(e => e.Name, "uq_grading_templates_name").IsUnique();

            entity.Property(e => e.Id)
                .HasColumnType("int(10) unsigned")
                .HasColumnName("id");
            entity.Property(e => e.ClassStandingWeight)
                .HasPrecision(5, 2)
                .HasColumnName("class_standing_weight");
            entity.Property(e => e.DepartmentId)
                .HasColumnType("int(10) unsigned")
                .HasColumnName("department_id");
            entity.Property(e => e.IsActive)
                .IsRequired()
                .HasDefaultValueSql("'1'")
                .HasColumnName("is_active");
            entity.Property(e => e.IsDefault).HasColumnName("is_default");
            entity.Property(e => e.MajorExamWeight)
                .HasPrecision(5, 2)
                .HasColumnName("major_exam_weight");
            entity.Property(e => e.Name)
                .HasMaxLength(100)
                .HasColumnName("name");

            entity.HasOne(d => d.Department).WithMany(p => p.GradingTemplates)
                .HasForeignKey(d => d.DepartmentId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_gt_department");
        });

        modelBuilder.Entity<Program>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.ToTable("programs");

            entity.HasIndex(e => e.DepartmentId, "fk_programs_department");

            entity.HasIndex(e => e.Code, "uq_programs_code").IsUnique();

            entity.Property(e => e.Id)
                .HasColumnType("int(10) unsigned")
                .HasColumnName("id");
            entity.Property(e => e.Code)
                .HasMaxLength(10)
                .HasColumnName("code");
            entity.Property(e => e.DepartmentId)
                .HasColumnType("int(10) unsigned")
                .HasColumnName("department_id");
            entity.Property(e => e.IsActive)
                .IsRequired()
                .HasDefaultValueSql("'1'")
                .HasColumnName("is_active");
            entity.Property(e => e.Name)
                .HasMaxLength(100)
                .HasColumnName("name");

            entity.HasOne(d => d.Department).WithMany(p => p.Programs)
                .HasForeignKey(d => d.DepartmentId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_programs_department");
        });

        modelBuilder.Entity<ProgramHead>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.ToTable("program_heads");

            entity.HasIndex(e => e.ProgramId, "fk_program_heads_program");

            entity.HasIndex(e => e.UserId, "uq_program_heads_user").IsUnique();

            entity.Property(e => e.Id)
                .HasColumnType("int(10) unsigned")
                .HasColumnName("id");
            entity.Property(e => e.IsActive)
                .IsRequired()
                .HasDefaultValueSql("'1'")
                .HasColumnName("is_active");
            entity.Property(e => e.ProgramId)
                .HasColumnType("int(10) unsigned")
                .HasColumnName("program_id");
            entity.Property(e => e.UserId).HasColumnName("user_id");

            entity.HasOne(d => d.Program).WithMany(p => p.ProgramHeads)
                .HasForeignKey(d => d.ProgramId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_program_heads_program");

            entity.HasOne(d => d.User).WithOne(p => p.ProgramHead)
                .HasForeignKey<ProgramHead>(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_program_heads_user");
        });

        modelBuilder.Entity<Role>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.ToTable("roles");

            entity.HasIndex(e => e.NormalizedName, "uq_roles_name").IsUnique();

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.ConcurrencyStamp)
                .HasMaxLength(100)
                .HasColumnName("concurrency_stamp");
            entity.Property(e => e.Name)
                .HasMaxLength(256)
                .HasColumnName("name");
            entity.Property(e => e.NormalizedName)
                .HasMaxLength(256)
                .HasColumnName("normalized_name");
        });

        modelBuilder.Entity<RoleClaim>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.ToTable("role_claims");

            entity.HasIndex(e => e.RoleId, "fk_role_claims_role");

            entity.Property(e => e.Id)
                .HasColumnType("int(11)")
                .HasColumnName("id");
            entity.Property(e => e.ClaimType)
                .HasColumnType("text")
                .HasColumnName("claim_type");
            entity.Property(e => e.ClaimValue)
                .HasColumnType("text")
                .HasColumnName("claim_value");
            entity.Property(e => e.RoleId).HasColumnName("role_id");

            entity.HasOne(d => d.Role).WithMany(p => p.RoleClaims)
                .HasForeignKey(d => d.RoleId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_role_claims_role");
        });

        modelBuilder.Entity<Score>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.ToTable("scores");

            entity.HasIndex(e => e.EnrollmentId, "fk_scores_enrollment");

            entity.HasIndex(e => new { e.AssessmentId, e.EnrollmentId }, "uq_scores").IsUnique();

            entity.Property(e => e.Id)
                .HasColumnType("int(10) unsigned")
                .HasColumnName("id");
            entity.Property(e => e.AssessmentId)
                .HasColumnType("int(10) unsigned")
                .HasColumnName("assessment_id");
            entity.Property(e => e.EnrollmentId)
                .HasColumnType("int(10) unsigned")
                .HasColumnName("enrollment_id");
            entity.Property(e => e.RawScore)
                .HasPrecision(6, 2)
                .HasColumnName("raw_score");
            entity.Property(e => e.RowVersion)
                .ValueGeneratedOnAddOrUpdate()
                .HasDefaultValueSql("current_timestamp(6)")
                .HasColumnType("timestamp(6)")
                .HasColumnName("row_version");
            entity.Property(e => e.Status)
                .HasMaxLength(10)
                .HasDefaultValueSql("'Pending'")
                .HasColumnName("status");
            entity.Property(e => e.SubmittedAt)
                .HasColumnType("datetime")
                .HasColumnName("submitted_at");

            entity.HasOne(d => d.Assessment).WithMany(p => p.Scores)
                .HasForeignKey(d => d.AssessmentId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_scores_assessment");

            entity.HasOne(d => d.Enrollment).WithMany(p => p.Scores)
                .HasForeignKey(d => d.EnrollmentId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_scores_enrollment");
        });

        modelBuilder.Entity<Section>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.ToTable("sections");

            entity.HasIndex(e => new { e.YearLevel, e.ShiftId }, "fk_sections_year_shift");

            entity.HasIndex(e => new { e.ProgramId, e.YearLevel, e.Semester, e.ShiftId, e.SectionNo }, "uq_sections_identity").IsUnique();

            entity.Property(e => e.Id)
                .HasColumnType("int(10) unsigned")
                .HasColumnName("id");
            entity.Property(e => e.IsActive)
                .IsRequired()
                .HasDefaultValueSql("'1'")
                .HasColumnName("is_active");
            entity.Property(e => e.ProgramId)
                .HasColumnType("int(10) unsigned")
                .HasColumnName("program_id");
            entity.Property(e => e.SectionNo)
                .HasColumnType("tinyint(3) unsigned")
                .HasColumnName("section_no");
            entity.Property(e => e.Semester)
                .HasColumnType("tinyint(3) unsigned")
                .HasColumnName("semester");
            entity.Property(e => e.ShiftId)
                .HasColumnType("int(10) unsigned")
                .HasColumnName("shift_id");
            entity.Property(e => e.YearLevel)
                .HasColumnType("tinyint(3) unsigned")
                .HasColumnName("year_level");

            entity.HasOne(d => d.Program).WithMany(p => p.Sections)
                .HasForeignKey(d => d.ProgramId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_sections_program");

            entity.HasOne(d => d.YearLevelShift).WithMany(p => p.Sections)
                .HasForeignKey(d => new { d.YearLevel, d.ShiftId })
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_sections_year_shift");
        });

        modelBuilder.Entity<Shift>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.ToTable("shifts");

            entity.HasIndex(e => e.Code, "uq_shifts_code").IsUnique();

            entity.Property(e => e.Id)
                .HasColumnType("int(10) unsigned")
                .HasColumnName("id");
            entity.Property(e => e.Code)
                .HasMaxLength(1)
                .IsFixedLength()
                .HasColumnName("code");
            entity.Property(e => e.Name)
                .HasMaxLength(20)
                .HasColumnName("name");
        });

        modelBuilder.Entity<Student>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.ToTable("students");

            entity.HasIndex(e => e.ProgramId, "fk_students_program");

            entity.HasIndex(e => e.StudentNo, "uq_students_no").IsUnique();

            entity.HasIndex(e => e.UserId, "uq_students_user").IsUnique();

            entity.Property(e => e.Id)
                .HasColumnType("int(10) unsigned")
                .HasColumnName("id");
            entity.Property(e => e.IsActive)
                .IsRequired()
                .HasDefaultValueSql("'1'")
                .HasColumnName("is_active");
            entity.Property(e => e.ProgramId)
                .HasColumnType("int(10) unsigned")
                .HasColumnName("program_id");
            entity.Property(e => e.StudentNo)
                .HasMaxLength(20)
                .HasColumnName("student_no");
            entity.Property(e => e.UserId).HasColumnName("user_id");

            entity.HasOne(d => d.Program).WithMany(p => p.Students)
                .HasForeignKey(d => d.ProgramId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_students_program");

            entity.HasOne(d => d.User).WithOne(p => p.Student)
                .HasForeignKey<Student>(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_students_user");
        });

        modelBuilder.Entity<Subject>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.ToTable("subjects");

            entity.HasIndex(e => e.Code, "uq_subjects_code").IsUnique();

            entity.Property(e => e.Id)
                .HasColumnType("int(10) unsigned")
                .HasColumnName("id");
            entity.Property(e => e.Code)
                .HasMaxLength(20)
                .HasColumnName("code");
            entity.Property(e => e.IsActive)
                .IsRequired()
                .HasDefaultValueSql("'1'")
                .HasColumnName("is_active");
            entity.Property(e => e.LabUnits)
                .HasColumnType("tinyint(3) unsigned")
                .HasColumnName("lab_units");
            entity.Property(e => e.LectureUnits)
                .HasColumnType("tinyint(3) unsigned")
                .HasColumnName("lecture_units");
            entity.Property(e => e.Title)
                .HasMaxLength(150)
                .HasColumnName("title");
        });

        modelBuilder.Entity<SubmissionCheck>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.ToTable("submission_checks");

            entity.HasIndex(e => e.CheckedBy, "fk_sc_checker");

            entity.HasIndex(e => e.ScoreId, "ix_submission_checks_score");

            entity.Property(e => e.Id)
                .HasColumnType("int(10) unsigned")
                .HasColumnName("id");
            entity.Property(e => e.CheckedAt)
                .HasDefaultValueSql("current_timestamp()")
                .HasColumnType("datetime")
                .HasColumnName("checked_at");
            entity.Property(e => e.CheckedBy).HasColumnName("checked_by");
            entity.Property(e => e.Note)
                .HasMaxLength(500)
                .HasColumnName("note");
            entity.Property(e => e.ScoreId)
                .HasColumnType("int(10) unsigned")
                .HasColumnName("score_id");
            entity.Property(e => e.SimilarityPct)
                .HasPrecision(5, 2)
                .HasColumnName("similarity_pct");

            entity.HasOne(d => d.CheckedByNavigation).WithMany(p => p.SubmissionChecks)
                .HasForeignKey(d => d.CheckedBy)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_sc_checker");

            entity.HasOne(d => d.Score).WithMany(p => p.SubmissionChecks)
                .HasForeignKey(d => d.ScoreId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_sc_score");
        });

        modelBuilder.Entity<SystemSetting>(entity =>
        {
            entity.HasKey(e => e.SettingKey).HasName("PRIMARY");

            entity.ToTable("system_settings");

            entity.Property(e => e.SettingKey)
                .HasMaxLength(60)
                .HasColumnName("setting_key");
            entity.Property(e => e.Description)
                .HasMaxLength(300)
                .HasColumnName("description");
            entity.Property(e => e.SettingValue)
                .HasMaxLength(200)
                .HasColumnName("setting_value");
        });

        modelBuilder.Entity<TemplateComponent>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.ToTable("template_components");

            entity.HasIndex(e => new { e.TemplateId, e.Name }, "uq_tc_name").IsUnique();

            entity.Property(e => e.Id)
                .HasColumnType("int(10) unsigned")
                .HasColumnName("id");
            entity.Property(e => e.ComponentType)
                .HasMaxLength(15)
                .HasColumnName("component_type");
            entity.Property(e => e.GroupType)
                .HasMaxLength(12)
                .HasColumnName("group_type");
            entity.Property(e => e.IsActive)
                .IsRequired()
                .HasDefaultValueSql("'1'")
                .HasColumnName("is_active");
            entity.Property(e => e.IsAttendance).HasColumnName("is_attendance");
            entity.Property(e => e.Name)
                .HasMaxLength(100)
                .HasColumnName("name");
            entity.Property(e => e.SortOrder)
                .HasDefaultValueSql("'1'")
                .HasColumnType("tinyint(3) unsigned")
                .HasColumnName("sort_order");
            entity.Property(e => e.TemplateId)
                .HasColumnType("int(10) unsigned")
                .HasColumnName("template_id");
            entity.Property(e => e.WeightPct)
                .HasPrecision(5, 2)
                .HasColumnName("weight_pct");

            entity.HasOne(d => d.Template).WithMany(p => p.TemplateComponents)
                .HasForeignKey(d => d.TemplateId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_tc_template");
        });

        modelBuilder.Entity<TermPeriod>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.ToTable("term_periods");

            entity.HasIndex(e => e.GradingPeriodId, "fk_tp_period");

            entity.HasIndex(e => new { e.TermId, e.GradingPeriodId }, "uq_term_periods").IsUnique();

            entity.Property(e => e.Id)
                .HasColumnType("int(10) unsigned")
                .HasColumnName("id");
            entity.Property(e => e.EndDate).HasColumnName("end_date");
            entity.Property(e => e.GradingPeriodId)
                .HasColumnType("int(10) unsigned")
                .HasColumnName("grading_period_id");
            entity.Property(e => e.StartDate).HasColumnName("start_date");
            entity.Property(e => e.TermId)
                .HasColumnType("int(10) unsigned")
                .HasColumnName("term_id");

            entity.HasOne(d => d.GradingPeriod).WithMany(p => p.TermPeriods)
                .HasForeignKey(d => d.GradingPeriodId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_tp_period");

            entity.HasOne(d => d.Term).WithMany(p => p.TermPeriods)
                .HasForeignKey(d => d.TermId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_tp_term");
        });

        modelBuilder.Entity<TransmutationRow>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.ToTable("transmutation_rows");

            entity.HasIndex(e => new { e.TableId, e.MinRaw }, "uq_tr_bound").IsUnique();

            entity.HasIndex(e => new { e.TableId, e.ReportedGrade }, "uq_tr_grade").IsUnique();

            entity.Property(e => e.Id)
                .HasColumnType("int(10) unsigned")
                .HasColumnName("id");
            entity.Property(e => e.IsPassing).HasColumnName("is_passing");
            entity.Property(e => e.MinRaw)
                .HasPrecision(5, 2)
                .HasColumnName("min_raw");
            entity.Property(e => e.ReportedGrade)
                .HasPrecision(3, 2)
                .HasColumnName("reported_grade");
            entity.Property(e => e.TableId)
                .HasColumnType("int(10) unsigned")
                .HasColumnName("table_id");

            entity.HasOne(d => d.Table).WithMany(p => p.TransmutationRows)
                .HasForeignKey(d => d.TableId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_tr_table");
        });

        modelBuilder.Entity<TransmutationTable>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.ToTable("transmutation_tables");

            entity.HasIndex(e => e.Name, "uq_transmutation_tables_name").IsUnique();

            entity.Property(e => e.Id)
                .HasColumnType("int(10) unsigned")
                .HasColumnName("id");
            entity.Property(e => e.IsActive)
                .IsRequired()
                .HasDefaultValueSql("'1'")
                .HasColumnName("is_active");
            entity.Property(e => e.IsSample)
                .IsRequired()
                .HasDefaultValueSql("'1'")
                .HasColumnName("is_sample");
            entity.Property(e => e.Name)
                .HasMaxLength(100)
                .HasColumnName("name");
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.ToTable("users");

            entity.HasIndex(e => e.NormalizedEmail, "uq_users_email").IsUnique();

            entity.HasIndex(e => e.NormalizedUserName, "uq_users_user_name").IsUnique();

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.AccessFailedCount)
                .HasColumnType("int(11)")
                .HasColumnName("access_failed_count");
            entity.Property(e => e.ConcurrencyStamp)
                .HasMaxLength(100)
                .HasColumnName("concurrency_stamp");
            entity.Property(e => e.Email)
                .HasMaxLength(256)
                .HasColumnName("email");
            entity.Property(e => e.EmailConfirmed).HasColumnName("email_confirmed");
            entity.Property(e => e.FirstName)
                .HasMaxLength(60)
                .HasColumnName("first_name");
            entity.Property(e => e.IsActive)
                .IsRequired()
                .HasDefaultValueSql("'1'")
                .HasColumnName("is_active");
            entity.Property(e => e.LastName)
                .HasMaxLength(60)
                .HasColumnName("last_name");
            entity.Property(e => e.LockoutEnabled)
                .IsRequired()
                .HasDefaultValueSql("'1'")
                .HasColumnName("lockout_enabled");
            entity.Property(e => e.LockoutEnd)
                .HasMaxLength(6)
                .HasColumnName("lockout_end");
            entity.Property(e => e.MiddleName)
                .HasMaxLength(60)
                .HasColumnName("middle_name");
            entity.Property(e => e.NormalizedEmail)
                .HasMaxLength(256)
                .HasColumnName("normalized_email");
            entity.Property(e => e.NormalizedUserName)
                .HasMaxLength(256)
                .HasColumnName("normalized_user_name");
            entity.Property(e => e.PasswordHash)
                .HasMaxLength(255)
                .HasColumnName("password_hash");
            entity.Property(e => e.PhoneNumber)
                .HasMaxLength(30)
                .HasColumnName("phone_number");
            entity.Property(e => e.PhoneNumberConfirmed).HasColumnName("phone_number_confirmed");
            entity.Property(e => e.SecurityStamp)
                .HasMaxLength(100)
                .HasColumnName("security_stamp");
            entity.Property(e => e.TwoFactorEnabled).HasColumnName("two_factor_enabled");
            entity.Property(e => e.UserName)
                .HasMaxLength(256)
                .HasColumnName("user_name");

            entity.HasMany(d => d.Roles).WithMany(p => p.Users)
                .UsingEntity<Dictionary<string, object>>(
                    "UserRole",
                    r => r.HasOne<Role>().WithMany()
                        .HasForeignKey("RoleId")
                        .OnDelete(DeleteBehavior.ClientSetNull)
                        .HasConstraintName("fk_user_roles_role"),
                    l => l.HasOne<User>().WithMany()
                        .HasForeignKey("UserId")
                        .OnDelete(DeleteBehavior.ClientSetNull)
                        .HasConstraintName("fk_user_roles_user"),
                    j =>
                    {
                        j.HasKey("UserId", "RoleId")
                            .HasName("PRIMARY")
                            .HasAnnotation("MySql:IndexPrefixLength", new[] { 0, 0 });
                        j.ToTable("user_roles");
                        j.HasIndex(new[] { "RoleId" }, "fk_user_roles_role");
                        j.IndexerProperty<Guid>("UserId").HasColumnName("user_id");
                        j.IndexerProperty<Guid>("RoleId").HasColumnName("role_id");
                    });
        });

        modelBuilder.Entity<UserClaim>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.ToTable("user_claims");

            entity.HasIndex(e => e.UserId, "fk_user_claims_user");

            entity.Property(e => e.Id)
                .HasColumnType("int(11)")
                .HasColumnName("id");
            entity.Property(e => e.ClaimType)
                .HasColumnType("text")
                .HasColumnName("claim_type");
            entity.Property(e => e.ClaimValue)
                .HasColumnType("text")
                .HasColumnName("claim_value");
            entity.Property(e => e.UserId).HasColumnName("user_id");

            entity.HasOne(d => d.User).WithMany(p => p.UserClaims)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_user_claims_user");
        });

        modelBuilder.Entity<UserLogin>(entity =>
        {
            entity.HasKey(e => new { e.LoginProvider, e.ProviderKey })
                .HasName("PRIMARY")
                .HasAnnotation("MySql:IndexPrefixLength", new[] { 0, 0 });

            entity.ToTable("user_logins");

            entity.HasIndex(e => e.UserId, "fk_user_logins_user");

            entity.Property(e => e.LoginProvider)
                .HasMaxLength(128)
                .HasColumnName("login_provider");
            entity.Property(e => e.ProviderKey)
                .HasMaxLength(128)
                .HasColumnName("provider_key");
            entity.Property(e => e.ProviderDisplayName)
                .HasColumnType("text")
                .HasColumnName("provider_display_name");
            entity.Property(e => e.UserId).HasColumnName("user_id");

            entity.HasOne(d => d.User).WithMany(p => p.UserLogins)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_user_logins_user");
        });

        modelBuilder.Entity<UserToken>(entity =>
        {
            entity.HasKey(e => new { e.UserId, e.LoginProvider, e.Name })
                .HasName("PRIMARY")
                .HasAnnotation("MySql:IndexPrefixLength", new[] { 0, 0, 0 });

            entity.ToTable("user_tokens");

            entity.Property(e => e.UserId).HasColumnName("user_id");
            entity.Property(e => e.LoginProvider)
                .HasMaxLength(128)
                .HasColumnName("login_provider");
            entity.Property(e => e.Name)
                .HasMaxLength(128)
                .HasColumnName("name");
            entity.Property(e => e.Value)
                .HasColumnType("text")
                .HasColumnName("value");

            entity.HasOne(d => d.User).WithMany(p => p.UserTokens)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_user_tokens_user");
        });

        modelBuilder.Entity<YearLevelShift>(entity =>
        {
            entity.HasKey(e => new { e.YearLevel, e.ShiftId })
                .HasName("PRIMARY")
                .HasAnnotation("MySql:IndexPrefixLength", new[] { 0, 0 });

            entity.ToTable("year_level_shifts");

            entity.HasIndex(e => e.ShiftId, "fk_yls_shift");

            entity.Property(e => e.YearLevel)
                .HasColumnType("tinyint(3) unsigned")
                .HasColumnName("year_level");
            entity.Property(e => e.ShiftId)
                .HasColumnType("int(10) unsigned")
                .HasColumnName("shift_id");

            entity.HasOne(d => d.Shift).WithMany(p => p.YearLevelShifts)
                .HasForeignKey(d => d.ShiftId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_yls_shift");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
