using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using CvPlatform.Data.Entities;

namespace CvPlatform.Data;

/// <summary>
/// Seeds initial Identity roles, system administrator, and demo accounts for evaluation.
/// </summary>
public static class IdentityDataSeeder
{
    public static async Task SeedAsync(IServiceProvider serviceProvider, ILogger logger)
    {
        try
        {
            using var scope = serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<ApplicationRole>>();
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

            // Ensure database is created/migrated if database is reachable
            if (await context.Database.CanConnectAsync())
            {
                await context.Database.MigrateAsync();

                // 1. Seed Roles
                await SeedRoleAsync(roleManager, ApplicationRole.Administrator, "Full system governance and user administration");
                await SeedRoleAsync(roleManager, ApplicationRole.Recruiter, "Talent acquisition, position template creation, and CV review");
                await SeedRoleAsync(roleManager, ApplicationRole.Candidate, "Job candidate with master profile, portfolio, and tailored CVs");

                // 2. Seed Admin Demo Account
                await SeedUserAsync(
                    userManager,
                    context,
                    email: "admin@cvplatform.com",
                    password: "Admin@123",
                    fullName: "System Administrator",
                    role: ApplicationRole.Administrator
                );

                // 3. Seed Recruiter Demo Account
                await SeedUserAsync(
                    userManager,
                    context,
                    email: "recruiter@cvplatform.com",
                    password: "Recruiter@123",
                    fullName: "Sarah Connor (HR Lead)",
                    role: ApplicationRole.Recruiter
                );

                // 4. Seed Candidate Demo Account
                await SeedUserAsync(
                    userManager,
                    context,
                    email: "candidate@cvplatform.com",
                    password: "Candidate@123",
                    fullName: "Alex Rivera (Senior Engineer)",
                    role: ApplicationRole.Candidate,
                    createProfile: true
                );

                logger.LogInformation("Database seeded successfully with default roles and demo accounts.");
            }
            else
            {
                logger.LogWarning("PostgreSQL database is currently unreachable. Seeding skipped for now.");
            }
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Seeding encountered an issue (PostgreSQL might be offline). App will continue running.");
        }
    }

    private static async Task SeedRoleAsync(RoleManager<ApplicationRole> roleManager, string roleName, string description)
    {
        if (!await roleManager.RoleExistsAsync(roleName))
        {
            var role = new ApplicationRole
            {
                Name = roleName,
                NormalizedName = roleName.ToUpperInvariant(),
                Description = description,
                CreatedAt = DateTime.UtcNow
            };
            await roleManager.CreateAsync(role);
        }
    }

    private static async Task SeedUserAsync(
        UserManager<ApplicationUser> userManager,
        ApplicationDbContext context,
        string email,
        string password,
        string fullName,
        string role,
        bool createProfile = false)
    {
        var existingUser = await userManager.FindByEmailAsync(email);
        if (existingUser == null)
        {
            var user = new ApplicationUser
            {
                UserName = email,
                Email = email,
                FullName = fullName,
                EmailConfirmed = true,
                CreatedAt = DateTime.UtcNow
            };

            var createResult = await userManager.CreateAsync(user, password);
            if (createResult.Succeeded)
            {
                await userManager.AddToRoleAsync(user, role);

                if (createProfile)
                {
                    var existingProfile = await context.CandidateProfiles.FirstOrDefaultAsync(p => p.UserId == user.Id);
                    if (existingProfile == null)
                    {
                        var profile = new CandidateProfile
                        {
                            UserId = user.Id,
                            FullName = fullName,
                            Headline = "Senior Full-Stack & Cloud Systems Architect",
                            Summary = "Passionate software engineer with 8+ years experience in distributed systems, .NET Core, cloud architectures, and scalable web solutions.",
                            Phone = "+1 (555) 349-2810",
                            Location = "Stockholm, Sweden / Remote",
                            SocialLinksJson = "{\"github\":\"https://github.com\",\"linkedin\":\"https://linkedin.com\"}",
                            CreatedAt = DateTime.UtcNow
                        };
                        context.CandidateProfiles.Add(profile);
                        await context.SaveChangesAsync();
                    }
                }
            }
        }
    }
}
