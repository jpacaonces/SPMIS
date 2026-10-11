using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microting.EntityFrameworkCore.MySql.Infrastructure;
using Spmis.Web.Data;
using Spmis.Web.Data.Auth;
using Spmis.Web.Data.Entities;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();

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
// -----------------------------------------------------------------------

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();

app.UseAuthentication();   // added: who are you?  (must come BEFORE...)
app.UseAuthorization();    // ...what are you allowed to do?

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

// ---- TEMPORARY startup check (remove in Step 3c) ---------------------
using (var scope = app.Services.CreateScope())
{
    var identityDb = scope.ServiceProvider.GetRequiredService<AppIdentityDbContext>();
    var businessDb = scope.ServiceProvider.GetRequiredService<SpmisDbContext>();
    Console.WriteLine($"[TEMP] users via Identity context:    {identityDb.Users.Count()}");
    Console.WriteLine($"[TEMP] roles via Identity context:    {identityDb.Roles.Count()}");
    Console.WriteLine($"[TEMP] students via business context: {businessDb.Students.Count()}");
}
// -----------------------------------------------------------------------

app.Run();
