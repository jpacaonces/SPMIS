using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Spmis.Web.Data.Auth;

// Handles ONLY the 7 login tables. Business data stays in SpmisDbContext.
public class AppIdentityDbContext
    : IdentityDbContext<ApplicationUser, ApplicationRole, Guid>
{
    public AppIdentityDbContext(DbContextOptions<AppIdentityDbContext> options)
        : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        // Identity's defaults first (AspNetUsers etc.)...
        base.OnModelCreating(builder);

        // ...then we override them to match OUR tables and columns.
        builder.Entity<ApplicationUser>(e =>
        {
            e.ToTable("users");
            e.Property(x => x.Id).HasColumnName("id");
            e.Property(x => x.UserName).HasColumnName("user_name");
            e.Property(x => x.NormalizedUserName).HasColumnName("normalized_user_name");
            e.Property(x => x.Email).HasColumnName("email");
            e.Property(x => x.NormalizedEmail).HasColumnName("normalized_email");
            e.Property(x => x.EmailConfirmed).HasColumnName("email_confirmed");
            e.Property(x => x.PasswordHash).HasColumnName("password_hash");
            e.Property(x => x.SecurityStamp).HasColumnName("security_stamp");
            e.Property(x => x.ConcurrencyStamp).HasColumnName("concurrency_stamp");
            e.Property(x => x.PhoneNumber).HasColumnName("phone_number");
            e.Property(x => x.PhoneNumberConfirmed).HasColumnName("phone_number_confirmed");
            e.Property(x => x.TwoFactorEnabled).HasColumnName("two_factor_enabled");
            e.Property(x => x.LockoutEnd).HasColumnName("lockout_end").HasColumnType("datetime(6)");
            e.Property(x => x.LockoutEnabled).HasColumnName("lockout_enabled");
            e.Property(x => x.AccessFailedCount).HasColumnName("access_failed_count");
            e.Property(x => x.FirstName).HasColumnName("first_name");
            e.Property(x => x.MiddleName).HasColumnName("middle_name");
            e.Property(x => x.LastName).HasColumnName("last_name");
            e.Property(x => x.IsActive).HasColumnName("is_active");
        });

        builder.Entity<ApplicationRole>(e =>
        {
            e.ToTable("roles");
            e.Property(x => x.Id).HasColumnName("id");
            e.Property(x => x.Name).HasColumnName("name");
            e.Property(x => x.NormalizedName).HasColumnName("normalized_name");
            e.Property(x => x.ConcurrencyStamp).HasColumnName("concurrency_stamp");
        });

        builder.Entity<IdentityUserRole<Guid>>(e =>
        {
            e.ToTable("user_roles");
            e.Property(x => x.UserId).HasColumnName("user_id");
            e.Property(x => x.RoleId).HasColumnName("role_id");
        });

        builder.Entity<IdentityUserClaim<Guid>>(e =>
        {
            e.ToTable("user_claims");
            e.Property(x => x.Id).HasColumnName("id");
            e.Property(x => x.UserId).HasColumnName("user_id");
            e.Property(x => x.ClaimType).HasColumnName("claim_type");
            e.Property(x => x.ClaimValue).HasColumnName("claim_value");
        });

        builder.Entity<IdentityRoleClaim<Guid>>(e =>
        {
            e.ToTable("role_claims");
            e.Property(x => x.Id).HasColumnName("id");
            e.Property(x => x.RoleId).HasColumnName("role_id");
            e.Property(x => x.ClaimType).HasColumnName("claim_type");
            e.Property(x => x.ClaimValue).HasColumnName("claim_value");
        });

        builder.Entity<IdentityUserLogin<Guid>>(e =>
        {
            e.ToTable("user_logins");
            e.Property(x => x.LoginProvider).HasColumnName("login_provider");
            e.Property(x => x.ProviderKey).HasColumnName("provider_key");
            e.Property(x => x.ProviderDisplayName).HasColumnName("provider_display_name");
            e.Property(x => x.UserId).HasColumnName("user_id");
        });

        builder.Entity<IdentityUserToken<Guid>>(e =>
        {
            e.ToTable("user_tokens");
            e.Property(x => x.UserId).HasColumnName("user_id");
            e.Property(x => x.LoginProvider).HasColumnName("login_provider");
            e.Property(x => x.Name).HasColumnName("name");
            e.Property(x => x.Value).HasColumnName("value");
        });
    }
}