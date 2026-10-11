using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Spmis.Web.Data.Auth;

// DEVELOPMENT ONLY. Gives every account that has no password yet
// one shared password taken from user secrets.
public static class DevPasswordSeeder
{
    public static async Task RunAsync(
        IServiceProvider services, IConfiguration config, ILogger logger)
    {
        var password = config["Seed:DevPassword"];
        if (string.IsNullOrWhiteSpace(password))
        {
            logger.LogWarning(
                "Seed:DevPassword is not set. Skipping dev password seeding.");
            return;
        }

        var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();

        // Only accounts with NO password. Safe to run on every startup.
        var users = await userManager.Users
            .Where(u => u.PasswordHash == null)
            .ToListAsync();

        var done = 0;
        foreach (var user in users)
        {
            // AddPasswordAsync hashes the password with Identity's hasher
            // (salted PBKDF2) and stores only the hash.
            var result = await userManager.AddPasswordAsync(user, password);
            if (result.Succeeded)
            {
                done++;
            }
            else
            {
                logger.LogError("Could not set password for {User}: {Errors}",
                    user.UserName,
                    string.Join("; ", result.Errors.Select(e => e.Description)));
            }
        }

        logger.LogInformation(
            "Dev password seeding: {Done} of {Total} accounts updated.",
            done, users.Count);

        // Proof that login lookup + hash check work end to end.
        var admin = await userManager.FindByNameAsync("admin@demo.lyceum.test");
        if (admin is not null)
        {
            var ok = await userManager.CheckPasswordAsync(admin, password);
            logger.LogInformation("Admin password check: {Ok}", ok);
        }
    }
}
