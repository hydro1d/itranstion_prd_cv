using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using CvPlatform.Data;
using CvPlatform.Data.Entities;

namespace CvPlatform.Endpoints;

public static class AccountEndpoints
{
    public static IEndpointRouteBuilder MapAccountEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/account");

        // 1. Password Login
        group.MapPost("/login", async (
            HttpContext context,
            SignInManager<ApplicationUser> signInManager,
            UserManager<ApplicationUser> userManager) =>
        {
            var form = await context.Request.ReadFormAsync();
            var email = form["email"].ToString();
            var password = form["password"].ToString();
            var rememberMe = form["rememberMe"].ToString() == "true" || form["rememberMe"].ToString() == "on";
            var returnUrl = form["returnUrl"].ToString();

            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
            {
                return Results.LocalRedirect("/account/login?error=MissingCredentials");
            }

            var user = await userManager.FindByEmailAsync(email);
            if (user == null)
            {
                return Results.LocalRedirect("/account/login?error=InvalidCredentials");
            }

            var result = await signInManager.PasswordSignInAsync(user, password, rememberMe, lockoutOnFailure: false);
            if (result.Succeeded)
            {
                user.LastLoginAt = DateTime.UtcNow;
                await userManager.UpdateAsync(user);

                if (!string.IsNullOrWhiteSpace(returnUrl) && returnUrl.StartsWith('/') && !returnUrl.StartsWith("//"))
                {
                    return Results.LocalRedirect(returnUrl);
                }

                // Default redirect by role
                var roles = await userManager.GetRolesAsync(user);
                if (roles.Contains(ApplicationRole.Administrator)) return Results.LocalRedirect("/admin/dashboard");
                if (roles.Contains(ApplicationRole.Recruiter)) return Results.LocalRedirect("/recruiter/dashboard");
                return Results.LocalRedirect("/candidate/dashboard");
            }

            return Results.LocalRedirect("/account/login?error=InvalidCredentials");
        }).DisableAntiforgery();

        // 2. Demo One-Click Sign-In (For evaluators and rapid testing)
        group.MapPost("/demo-login", async (
            HttpContext context,
            SignInManager<ApplicationUser> signInManager,
            UserManager<ApplicationUser> userManager) =>
        {
            var form = await context.Request.ReadFormAsync();
            var roleParam = form["role"].ToString().ToLowerInvariant();

            string targetEmail = roleParam switch
            {
                "admin" => "admin@cvplatform.com",
                "recruiter" => "recruiter@cvplatform.com",
                _ => "candidate@cvplatform.com"
            };

            var user = await userManager.FindByEmailAsync(targetEmail);
            if (user == null)
            {
                return Results.LocalRedirect($"/account/login?error=DemoAccountNotSeeded");
            }

            await signInManager.SignInAsync(user, isPersistent: true);
            user.LastLoginAt = DateTime.UtcNow;
            await userManager.UpdateAsync(user);

            return roleParam switch
            {
                "admin" => Results.LocalRedirect("/admin/dashboard"),
                "recruiter" => Results.LocalRedirect("/recruiter/dashboard"),
                _ => Results.LocalRedirect("/candidate/dashboard")
            };
        }).DisableAntiforgery();

        // 3. User Registration
        group.MapPost("/register", async (
            HttpContext context,
            SignInManager<ApplicationUser> signInManager,
            UserManager<ApplicationUser> userManager,
            ApplicationDbContext dbContext) =>
        {
            var form = await context.Request.ReadFormAsync();
            var fullName = form["fullName"].ToString().Trim();
            var email = form["email"].ToString().Trim();
            var password = form["password"].ToString();
            var confirmPassword = form["confirmPassword"].ToString();
            var selectedRole = form["role"].ToString();

            if (string.IsNullOrWhiteSpace(fullName) || string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
            {
                return Results.LocalRedirect("/account/register?error=MissingFields");
            }

            if (password != confirmPassword)
            {
                return Results.LocalRedirect("/account/register?error=PasswordMismatch");
            }

            var existingUser = await userManager.FindByEmailAsync(email);
            if (existingUser != null)
            {
                return Results.LocalRedirect("/account/register?error=EmailTaken");
            }

            var roleToAssign = selectedRole == ApplicationRole.Recruiter 
                ? ApplicationRole.Recruiter 
                : ApplicationRole.Candidate;

            var user = new ApplicationUser
            {
                UserName = email,
                Email = email,
                FullName = fullName,
                EmailConfirmed = true,
                CreatedAt = DateTime.UtcNow
            };

            var result = await userManager.CreateAsync(user, password);
            if (!result.Succeeded)
            {
                var firstError = result.Errors.FirstOrDefault()?.Description ?? "RegistrationFailed";
                return Results.LocalRedirect($"/account/register?error={Uri.EscapeDataString(firstError)}");
            }

            await userManager.AddToRoleAsync(user, roleToAssign);

            // Auto-create CandidateProfile if candidate
            if (roleToAssign == ApplicationRole.Candidate)
            {
                var profile = new CandidateProfile
                {
                    UserId = user.Id,
                    FullName = fullName,
                    Headline = "Candidate Profile",
                    CreatedAt = DateTime.UtcNow
                };
                dbContext.CandidateProfiles.Add(profile);
                await dbContext.SaveChangesAsync();
            }

            await signInManager.SignInAsync(user, isPersistent: true);

            return roleToAssign == ApplicationRole.Recruiter
                ? Results.LocalRedirect("/recruiter/dashboard")
                : Results.LocalRedirect("/candidate/dashboard");
        }).DisableAntiforgery();

        // 4. Logout
        group.MapPost("/logout", async (
            HttpContext context,
            SignInManager<ApplicationUser> signInManager) =>
        {
            await signInManager.SignOutAsync();
            return Results.LocalRedirect("/");
        }).DisableAntiforgery();

        // 5. External / Social Login Simulation (For defense evaluation)
        group.MapGet("/external-login", async (
            string provider,
            SignInManager<ApplicationUser> signInManager,
            UserManager<ApplicationUser> userManager,
            ApplicationDbContext dbContext) =>
        {
            var providerName = provider.ToLowerInvariant() == "google" ? "Google" : "GitHub";
            var mockEmail = $"demo.{providerName.ToLowerInvariant()}@cvplatform.com";
            var user = await userManager.FindByEmailAsync(mockEmail);

            if (user == null)
            {
                user = new ApplicationUser
                {
                    UserName = mockEmail,
                    Email = mockEmail,
                    FullName = $"{providerName} Demo Candidate",
                    EmailConfirmed = true,
                    CreatedAt = DateTime.UtcNow
                };
                await userManager.CreateAsync(user, "DemoSocial@123");
                await userManager.AddToRoleAsync(user, ApplicationRole.Candidate);

                var profile = new CandidateProfile
                {
                    UserId = user.Id,
                    FullName = user.FullName,
                    Headline = $"Verified {providerName} Developer",
                    Summary = $"Signed in via {providerName} social authentication provider simulation.",
                    CreatedAt = DateTime.UtcNow
                };
                dbContext.CandidateProfiles.Add(profile);
                await dbContext.SaveChangesAsync();
            }

            await signInManager.SignInAsync(user, isPersistent: true);
            return Results.LocalRedirect("/candidate/dashboard");
        });

        return app;
    }
}
