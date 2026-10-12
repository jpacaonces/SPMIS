using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microting.EntityFrameworkCore.MySql.Infrastructure;
using Spmis.Web.Data;
using Spmis.Web.Data.Auth;
using Spmis.Web.Data.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Authorization;
using Spmis.Web.Services;


var builder = WebApplication.CreateBuilder(args);

// Secure by default: every controller action requires a signed-in user.
// Public pages must opt out with [AllowAnonymous].
builder.Services.AddControllersWithViews(options =>
{
    options.Filters.Add(new AuthorizeFilter());
    options.Filters.Add(new AuthorizeFilter());
    options.Filters.Add<MustChangePasswordFilter>();
});

// ---- Database (added in Step 3b) -------------------------------------
// The connection string comes from user secrets (not from source code).
var connectionString = builder.Configuration.GetConnectionString("Spmis")
    ?? throw new InvalidOperationException(
        "Connection string 'Spmis' not found. Set it with dotnet user-secrets.");

// XAMPP's database is MariaDB 10.4, so we say so explicitly.
// This avoids a connection at startup just to detect the version.
var serverVersion = new MariaDbServerVersion(new Version(10, 4, 32));

builder.Services.AddDbContext<SpmisDbContext>(options =>
    options.UseMySql(connectionString, serverVersion));

builder.Services.AddDbContext<AppIdentityDbContext>(options =>
    options.UseMySql(connectionString, serverVersion));

builder.Services.AddSingleton<IExcuseFileStore, ExcuseFileStore>();
builder.Services.AddScoped<IStudentExcuseService, StudentExcuseService>();

// ---- Identity (added in Step 3b) -------------------------------------
builder.Services
    .AddIdentity<ApplicationUser, ApplicationRole>(options =>
    {
        options.Password.RequiredLength = 8;
        options.Password.RequireDigit = true;
        options.Password.RequireLowercase = true;
        options.Password.RequireUppercase = true;
        options.Password.RequireNonAlphanumeric = false;

        options.Lockout.MaxFailedAccessAttempts = 5;
        options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);

        options.User.RequireUniqueEmail = true;
    })
    .AddEntityFrameworkStores<AppIdentityDbContext>()
    .AddDefaultTokenProviders();

builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Account/Login";
    options.AccessDeniedPath = "/Account/AccessDenied";
});

// Check the security stamp on EVERY request (default is every 30 minutes),
// so deactivating an account signs the person out on their next click.
builder.Services.Configure<SecurityStampValidatorOptions>(options =>
{
    options.ValidationInterval = TimeSpan.Zero;
});

// ---- Services (added in Step 5) --------------------------------------
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUser, CurrentUser>();
builder.Services.AddScoped<IAuditLogger, AuditLogger>();

builder.Services.AddScoped<IReasonService, ReasonService>();
builder.Services.AddScoped<IUserAdminService, UserAdminService>();
builder.Services.AddScoped<IAuditLogQueryService, AuditLogQueryService>();
builder.Services.AddScoped<ITermAdminService, TermAdminService>();
builder.Services.AddScoped<IProgramAdminService, ProgramAdminService>();
builder.Services.AddScoped<ISubjectAdminService, SubjectAdminService>();
builder.Services.AddScoped<ISectionAdminService, SectionAdminService>();
builder.Services.AddScoped<IClassOfferingAdminService, ClassOfferingAdminService>();
builder.Services.AddScoped<IEnrollmentAdminService, EnrollmentAdminService>();
builder.Services.AddScoped<IGradeCalculationService, GradeCalculationService>();
builder.Services.AddScoped<IClassRecordService, ClassRecordService>();
builder.Services.AddScoped<IAssessmentService, AssessmentService>();
builder.Services.AddScoped<IScoreEntryService, ScoreEntryService>();
builder.Services.AddScoped<IMeetingService, MeetingService>();
builder.Services.AddScoped<IAttendanceService, AttendanceService>();
builder.Services.AddScoped<IExcuseService, ExcuseService>();
builder.Services.AddScoped<IFinalizeService, FinalizeService>();
builder.Services.AddScoped<IStudentPortalService, StudentPortalService>();
builder.Services.AddScoped<IWhatIfService, WhatIfService>();
builder.Services.AddScoped<IProgramAnalyticsService, ProgramAnalyticsService>();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

// Friendly pages for 404, 403 and similar (re-runs the request to our action).
app.UseStatusCodePagesWithReExecute("/Home/HttpStatus", "?code={0}");

app.UseHttpsRedirection();
app.UseRouting();

app.UseAuthentication();   // added: who are you?  (must come BEFORE...)
app.UseAuthorization();    // ...what are you allowed to do?

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

// ---- Development only: seed one shared dev password (Step 3c) --------
if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    await DevPasswordSeeder.RunAsync(
        scope.ServiceProvider, app.Configuration, app.Logger);
}
// -----------------------------------------------------------------------

app.Run();
