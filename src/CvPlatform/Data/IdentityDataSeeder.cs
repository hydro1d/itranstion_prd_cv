using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using CvPlatform.Data.Entities;
using CvPlatform.Data.Enums;

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

                // 5. Seed Dynamic Attribute Library Categories & Attributes
                await SeedAttributesAsync(context);

                // 6. Seed Position Openings & Requirements Templates (Killer Feature #2)
                await SeedPositionsAsync(context, userManager);

                logger.LogInformation("Database seeded successfully with default roles, demo accounts, attribute library, and positions.");
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

    private static async Task SeedAttributesAsync(ApplicationDbContext context)
    {
        if (await context.AttributeCategories.AnyAsync()) return;

        // Categories
        var catExp = new AttributeCategory { Name = "Experience & Background", Description = "Work history, seniority, and education level", DisplayOrder = 1 };
        var catTech = new AttributeCategory { Name = "Technical Skills", Description = "Programming languages, frameworks, cloud, and databases", DisplayOrder = 2 };
        var catLang = new AttributeCategory { Name = "Languages & Communication", Description = "Spoken and written language proficiencies", DisplayOrder = 3 };
        var catLinks = new AttributeCategory { Name = "Certifications & Links", Description = "Professional links, portfolio, and credentials", DisplayOrder = 4 };
        var catPref = new AttributeCategory { Name = "Work Preferences", Description = "Work model, relocation, and salary expectations", DisplayOrder = 5 };

        context.AttributeCategories.AddRange(catExp, catTech, catLang, catLinks, catPref);
        await context.SaveChangesAsync();

        // Attributes
        var attributes = new List<CvAttribute>
        {
            new() { Name = "Years of Experience", Description = "Total professional software engineering experience", CategoryId = catExp.Id, DataType = AttributeDataType.Number, IsRequiredDefault = true, IsSearchable = true, IsActive = true },
            new() { Name = "Current Job Title", Description = "Current or most recent professional role title", CategoryId = catExp.Id, DataType = AttributeDataType.Text, IsRequiredDefault = true, IsSearchable = true, IsActive = true },
            new() { Name = "Available Start Date", Description = "Earliest potential start date for joining the team", CategoryId = catLinks.Id, DataType = AttributeDataType.Date, IsRequiredDefault = true, IsSearchable = true, IsActive = true },
            new() { Name = "GitHub Profile URL", Description = "Direct link to public GitHub portfolio and repositories", CategoryId = catLinks.Id, DataType = AttributeDataType.Url, IsRequiredDefault = false, IsSearchable = false, IsActive = true },
            new() { Name = "LinkedIn Profile URL", Description = "Direct link to verified LinkedIn professional profile", CategoryId = catLinks.Id, DataType = AttributeDataType.Url, IsRequiredDefault = false, IsSearchable = false, IsActive = true },
            new() { Name = "Willing to Relocate", Description = "Open to geographic relocation for on-site or hybrid roles", CategoryId = catPref.Id, DataType = AttributeDataType.Boolean, IsRequiredDefault = false, IsSearchable = true, IsActive = true }
        };

        // Dropdown: Education
        var attrEdu = new CvAttribute { Name = "Highest Education Level", Description = "Highest achieved academic degree", CategoryId = catExp.Id, DataType = AttributeDataType.Dropdown, IsRequiredDefault = false, IsSearchable = true, IsActive = true };
        attrEdu.Options = new List<AttributeOption>
        {
            new() { Value = "High School Diploma", DisplayOrder = 1 },
            new() { Value = "Bachelor's Degree", DisplayOrder = 2 },
            new() { Value = "Master's Degree", DisplayOrder = 3 },
            new() { Value = "Doctorate / PhD", DisplayOrder = 4 }
        };
        attributes.Add(attrEdu);

        // Dropdown: Primary Language
        var attrLang = new CvAttribute { Name = "Primary Programming Language", Description = "Core language utilized for principal software development", CategoryId = catTech.Id, DataType = AttributeDataType.Dropdown, IsRequiredDefault = true, IsSearchable = true, IsActive = true };
        attrLang.Options = new List<AttributeOption>
        {
            new() { Value = "C# / .NET", DisplayOrder = 1 },
            new() { Value = "TypeScript / JavaScript", DisplayOrder = 2 },
            new() { Value = "Python", DisplayOrder = 3 },
            new() { Value = "Java", DisplayOrder = 4 },
            new() { Value = "Go (Golang)", DisplayOrder = 5 },
            new() { Value = "Rust", DisplayOrder = 6 }
        };
        attributes.Add(attrLang);

        // MultiSelect: Frameworks
        var attrFrameworks = new CvAttribute { Name = "Frameworks & Libraries", Description = "Key engineering frameworks and technologies mastered", CategoryId = catTech.Id, DataType = AttributeDataType.MultiSelect, IsRequiredDefault = true, IsSearchable = true, IsActive = true };
        attrFrameworks.Options = new List<AttributeOption>
        {
            new() { Value = "ASP.NET Core", DisplayOrder = 1 },
            new() { Value = "Blazor", DisplayOrder = 2 },
            new() { Value = "React", DisplayOrder = 3 },
            new() { Value = "Angular", DisplayOrder = 4 },
            new() { Value = "Node.js", DisplayOrder = 5 },
            new() { Value = "Spring Boot", DisplayOrder = 6 }
        };
        attributes.Add(attrFrameworks);

        // MultiSelect: Cloud
        var attrCloud = new CvAttribute { Name = "Cloud & Infrastructure Platforms", Description = "Cloud hosting, containerization, and orchestration platforms", CategoryId = catTech.Id, DataType = AttributeDataType.MultiSelect, IsRequiredDefault = false, IsSearchable = true, IsActive = true };
        attrCloud.Options = new List<AttributeOption>
        {
            new() { Value = "AWS", DisplayOrder = 1 },
            new() { Value = "Microsoft Azure", DisplayOrder = 2 },
            new() { Value = "Google Cloud Platform", DisplayOrder = 3 },
            new() { Value = "Docker & Containers", DisplayOrder = 4 },
            new() { Value = "Kubernetes", DisplayOrder = 5 }
        };
        attributes.Add(attrCloud);

        // Dropdown: English
        var attrEnglish = new CvAttribute { Name = "English Proficiency", Description = "Standard CEFR proficiency level for English communication", CategoryId = catLang.Id, DataType = AttributeDataType.Dropdown, IsRequiredDefault = true, IsSearchable = true, IsActive = true };
        attrEnglish.Options = new List<AttributeOption>
        {
            new() { Value = "Native / Bilingual", DisplayOrder = 1 },
            new() { Value = "Fluent (C1/C2)", DisplayOrder = 2 },
            new() { Value = "Professional Working (B2)", DisplayOrder = 3 },
            new() { Value = "Conversational (B1)", DisplayOrder = 4 }
        };
        attributes.Add(attrEnglish);

        // Dropdown: Work Model
        var attrWorkModel = new CvAttribute { Name = "Preferred Work Model", Description = "Preferred workplace location structure", CategoryId = catPref.Id, DataType = AttributeDataType.Dropdown, IsRequiredDefault = true, IsSearchable = true, IsActive = true };
        attrWorkModel.Options = new List<AttributeOption>
        {
            new() { Value = "Remote", DisplayOrder = 1 },
            new() { Value = "Hybrid", DisplayOrder = 2 },
            new() { Value = "On-site", DisplayOrder = 3 }
        };
        attributes.Add(attrWorkModel);

        context.Attributes.AddRange(attributes);
        await context.SaveChangesAsync();
    }

    private static async Task SeedPositionsAsync(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
    {
        if (await context.Positions.AnyAsync()) return;

        var recruiter = await userManager.FindByEmailAsync("recruiter@cvplatform.com");
        if (recruiter == null) return;

        var allAttributes = await context.Attributes.ToListAsync();
        var attrMap = allAttributes.ToDictionary(a => a.Name, a => a.Id);

        // Helper to add attribute requirement
        void AddReq(Position p, string attrName, bool isRequired, ref int order)
        {
            if (attrMap.TryGetValue(attrName, out var id))
            {
                p.PositionAttributes.Add(new PositionAttribute
                {
                    AttributeId = id,
                    IsRequired = isRequired,
                    DisplayOrder = order++
                });
            }
        }

        // Position 1: Senior .NET Core Cloud Architect
        var p1 = new Position
        {
            Title = "Senior .NET Core Cloud Architect",
            Company = "FinTech Global Solutions",
            Location = "Stockholm, Sweden / Remote",
            EmploymentType = EmploymentType.FullTime,
            Visibility = PositionVisibility.Public,
            Deadline = DateTime.UtcNow.AddDays(30),
            Tags = "C#, .NET 9, Cloud, Microservices, Architecture",
            Description = "We are seeking a seasoned .NET Cloud Architect to spearhead high-throughput distributed transaction systems. You will lead cloud design patterns, microservices decomposition, and ensure sub-millisecond database queries across distributed clusters.",
            RecruiterId = recruiter.Id,
            CreatedAt = DateTime.UtcNow.AddDays(-5),
            UpdatedAt = DateTime.UtcNow.AddDays(-1)
        };
        int o1 = 1;
        AddReq(p1, "Years of Experience", true, ref o1);
        AddReq(p1, "Current Job Title", true, ref o1);
        AddReq(p1, "Primary Programming Language", true, ref o1);
        AddReq(p1, "Frameworks & Libraries", true, ref o1);
        AddReq(p1, "English Proficiency", true, ref o1);
        AddReq(p1, "Highest Education Level", false, ref o1);
        AddReq(p1, "Cloud & Infrastructure Platforms", false, ref o1);
        AddReq(p1, "GitHub Profile URL", false, ref o1);
        AddReq(p1, "LinkedIn Profile URL", false, ref o1);
        AddReq(p1, "Willing to Relocate", false, ref o1);

        // Position 2: Full-Stack TypeScript & React Engineer
        var p2 = new Position
        {
            Title = "Full-Stack TypeScript & React Engineer",
            Company = "Nordic Tech Labs",
            Location = "Gothenburg, Sweden (Hybrid)",
            EmploymentType = EmploymentType.FullTime,
            Visibility = PositionVisibility.Public,
            Deadline = DateTime.UtcNow.AddDays(20),
            Tags = "TypeScript, React, Node.js, Web, UI/UX",
            Description = "Join our product engineering squad building rich, interactive web portals. You will craft accessible, high-performance web applications using modern TypeScript, React, and server-side component architectures.",
            RecruiterId = recruiter.Id,
            CreatedAt = DateTime.UtcNow.AddDays(-3),
            UpdatedAt = DateTime.UtcNow.AddDays(-1)
        };
        int o2 = 1;
        AddReq(p2, "Years of Experience", true, ref o2);
        AddReq(p2, "Current Job Title", true, ref o2);
        AddReq(p2, "Frameworks & Libraries", true, ref o2);
        AddReq(p2, "English Proficiency", true, ref o2);
        AddReq(p2, "Primary Programming Language", false, ref o2);
        AddReq(p2, "GitHub Profile URL", false, ref o2);
        AddReq(p2, "Preferred Work Model", false, ref o2);

        // Position 3: Cloud Infrastructure & DevOps Lead
        var p3 = new Position
        {
            Title = "Cloud Infrastructure & DevOps Lead",
            Company = "CloudScale Systems",
            Location = "Remote (Europe)",
            EmploymentType = EmploymentType.Contract,
            Visibility = PositionVisibility.Public,
            Deadline = DateTime.UtcNow.AddDays(15),
            Tags = "DevOps, Kubernetes, Docker, AWS, Terraform",
            Description = "Seeking an infrastructure automation expert to design and maintain self-healing Kubernetes clusters, CI/CD pipelines, and multi-region infrastructure as code.",
            RecruiterId = recruiter.Id,
            CreatedAt = DateTime.UtcNow.AddDays(-2),
            UpdatedAt = DateTime.UtcNow.AddDays(-1)
        };
        int o3 = 1;
        AddReq(p3, "Years of Experience", true, ref o3);
        AddReq(p3, "Cloud & Infrastructure Platforms", true, ref o3);
        AddReq(p3, "English Proficiency", true, ref o3);
        AddReq(p3, "Primary Programming Language", false, ref o3);
        AddReq(p3, "GitHub Profile URL", false, ref o3);
        AddReq(p3, "Available Start Date", false, ref o3);
        AddReq(p3, "Willing to Relocate", false, ref o3);

        // Position 4: Junior Backend Software Engineer
        var p4 = new Position
        {
            Title = "Junior Backend Software Engineer",
            Company = "InnovateIQ Labs",
            Location = "Malmö, Sweden (On-site)",
            EmploymentType = EmploymentType.Internship,
            Visibility = PositionVisibility.Public,
            Deadline = DateTime.UtcNow.AddDays(45),
            Tags = "Backend, C#, SQL, Mentorship, Graduate",
            Description = "An outstanding graduate or early-career role offering close mentorship with senior engineers. You will contribute to API endpoints, database migrations, and unit test suites.",
            RecruiterId = recruiter.Id,
            CreatedAt = DateTime.UtcNow.AddDays(-1),
            UpdatedAt = DateTime.UtcNow
        };
        int o4 = 1;
        AddReq(p4, "Highest Education Level", true, ref o4);
        AddReq(p4, "Primary Programming Language", true, ref o4);
        AddReq(p4, "English Proficiency", true, ref o4);
        AddReq(p4, "GitHub Profile URL", false, ref o4);
        AddReq(p4, "Available Start Date", false, ref o4);

        context.Positions.AddRange(p1, p2, p3, p4);
        await context.SaveChangesAsync();
    }
}
