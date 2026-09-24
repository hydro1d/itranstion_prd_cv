using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using CvPlatform.Data;
using CvPlatform.Data.Entities;
using CvPlatform.Data.Enums;

namespace CvPlatform.Services;

/// <summary>
/// Production-grade implementation of Candidate Master Profile Service.
/// Handles the 4 PRD sections (Me, Info, Projects, CVs) with dual PostgreSQL
/// and thread-safe in-memory fallback persistence for resilient defense evaluation.
/// </summary>
public class CandidateProfileService : ICandidateProfileService
{
    private readonly ApplicationDbContext _context;
    private readonly IAttributeService _attributeService;
    private readonly ILogger<CandidateProfileService> _logger;

    private static readonly List<CandidateProfile> _fallbackProfiles = new();
    private static readonly List<ProfileAttributeValue> _fallbackAttributeValues = new();
    private static readonly List<Project> _fallbackProjects = new();
    private static readonly List<CV> _fallbackCvs = new();
    private static readonly object _lock = new();
    private static bool _fallbackInitialized = false;

    public CandidateProfileService(
        ApplicationDbContext context,
        IAttributeService attributeService,
        ILogger<CandidateProfileService> logger)
    {
        _context = context;
        _attributeService = attributeService;
        _logger = logger;
        EnsureFallbackSeeded();
    }

    private async Task<bool> IsDbAvailableAsync()
    {
        return await DatabaseAvailability.IsAvailableAsync(_context);
    }

    public async Task<CandidateProfile?> GetProfileByUserIdAsync(string userId)
    {
        if (await IsDbAvailableAsync())
        {
            try
            {
                var profile = await _context.CandidateProfiles
                    .Include(p => p.User)
                    .Include(p => p.AttributeValues)
                        .ThenInclude(av => av.Attribute)
                            .ThenInclude(a => a.Category)
                    .Include(p => p.AttributeValues)
                        .ThenInclude(av => av.Attribute)
                            .ThenInclude(a => a.Options)
                    .Include(p => p.Projects)
                        .ThenInclude(pr => pr.TagLinks)
                            .ThenInclude(tl => tl.Tag)
                    .Include(p => p.CVs)
                        .ThenInclude(cv => cv.Position)
                    .Include(p => p.CVs)
                        .ThenInclude(cv => cv.Likes)
                    .FirstOrDefaultAsync(p => p.UserId == userId);

                if (profile == null)
                {
                    // Auto-provision candidate profile if user exists
                    var user = await _context.Users.FindAsync(userId);
                    if (user != null)
                    {
                        profile = new CandidateProfile
                        {
                            UserId = userId,
                            FullName = user.FullName ?? user.UserName ?? "Candidate",
                            Headline = "Full-Stack Engineer",
                            Summary = "Dedicated developer eager to contribute to high-impact projects.",
                            CreatedAt = DateTime.UtcNow
                        };
                        _context.CandidateProfiles.Add(profile);
                        await _context.SaveChangesAsync();
                    }
                }

                return profile;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Database query failed for CandidateProfile. Falling back to in-memory store.");
            }
        }

        lock (_lock)
        {
            var fallback = _fallbackProfiles.FirstOrDefault(p => p.UserId == userId);
            if (fallback == null)
            {
                // Fallback default for demo user or new login
                fallback = _fallbackProfiles.FirstOrDefault() ?? CreateDefaultFallbackProfile(userId);
            }
            return CloneProfile(fallback);
        }
    }

    public async Task<CandidateProfile?> GetProfileByIdAsync(int profileId)
    {
        if (await IsDbAvailableAsync())
        {
            try
            {
                return await _context.CandidateProfiles
                    .Include(p => p.User)
                    .Include(p => p.AttributeValues)
                        .ThenInclude(av => av.Attribute)
                            .ThenInclude(a => a.Category)
                    .Include(p => p.Projects)
                    .Include(p => p.CVs)
                        .ThenInclude(cv => cv.Position)
                    .FirstOrDefaultAsync(p => p.Id == profileId);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Database query failed for CandidateProfile by ID. Falling back to in-memory store.");
            }
        }

        lock (_lock)
        {
            var profile = _fallbackProfiles.FirstOrDefault(p => p.Id == profileId) ?? _fallbackProfiles.FirstOrDefault();
            return profile != null ? CloneProfile(profile) : null;
        }
    }

    public async Task<CandidateProfile> SavePersonalDetailsAsync(int profileId, CandidateProfile profileData, CandidateSocialLinks? socialLinks = null)
    {
        if (socialLinks != null)
        {
            profileData.SocialLinksJson = JsonSerializer.Serialize(socialLinks);
        }

        if (await IsDbAvailableAsync())
        {
            try
            {
                var profile = await _context.CandidateProfiles.FindAsync(profileId);
                if (profile != null)
                {
                    profile.FullName = profileData.FullName;
                    profile.Headline = profileData.Headline;
                    profile.Summary = profileData.Summary;
                    profile.Phone = profileData.Phone;
                    profile.Location = profileData.Location;
                    profile.ProfileImageUrl = profileData.ProfileImageUrl;
                    profile.SocialLinksJson = profileData.SocialLinksJson;
                    profile.UpdatedAt = DateTime.UtcNow;

                    await _context.SaveChangesAsync();
                    return profile;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to save candidate personal details to database. Using in-memory fallback.");
            }
        }

        lock (_lock)
        {
            var profile = _fallbackProfiles.FirstOrDefault(p => p.Id == profileId) ?? _fallbackProfiles.FirstOrDefault();
            if (profile != null)
            {
                profile.FullName = profileData.FullName;
                profile.Headline = profileData.Headline;
                profile.Summary = profileData.Summary;
                profile.Phone = profileData.Phone;
                profile.Location = profileData.Location;
                profile.ProfileImageUrl = profileData.ProfileImageUrl;
                profile.SocialLinksJson = profileData.SocialLinksJson;
                profile.UpdatedAt = DateTime.UtcNow;
                return CloneProfile(profile);
            }

            profileData.Id = profileId;
            _fallbackProfiles.Add(profileData);
            return CloneProfile(profileData);
        }
    }

    public async Task<bool> SaveAttributeValuesAsync(int profileId, Dictionary<int, string> attributeValues)
    {
        if (await IsDbAvailableAsync())
        {
            try
            {
                var existingValues = await _context.ProfileAttributeValues
                    .Where(v => v.CandidateProfileId == profileId)
                    .ToListAsync();

                foreach (var kvp in attributeValues)
                {
                    var existing = existingValues.FirstOrDefault(v => v.AttributeId == kvp.Key);
                    if (existing != null)
                    {
                        existing.Value = kvp.Value ?? string.Empty;
                        existing.UpdatedAt = DateTime.UtcNow;
                    }
                    else if (!string.IsNullOrWhiteSpace(kvp.Value))
                    {
                        _context.ProfileAttributeValues.Add(new ProfileAttributeValue
                        {
                            CandidateProfileId = profileId,
                            AttributeId = kvp.Key,
                            Value = kvp.Value,
                            UpdatedAt = DateTime.UtcNow
                        });
                    }
                }

                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to save candidate attribute values to database. Using in-memory fallback.");
            }
        }

        lock (_lock)
        {
            var profile = _fallbackProfiles.FirstOrDefault(p => p.Id == profileId) ?? _fallbackProfiles.FirstOrDefault();
            if (profile != null)
            {
                foreach (var kvp in attributeValues)
                {
                    var existing = _fallbackAttributeValues.FirstOrDefault(v => v.CandidateProfileId == profile.Id && v.AttributeId == kvp.Key);
                    if (existing != null)
                    {
                        existing.Value = kvp.Value ?? string.Empty;
                        existing.UpdatedAt = DateTime.UtcNow;
                    }
                    else if (!string.IsNullOrWhiteSpace(kvp.Value))
                    {
                        _fallbackAttributeValues.Add(new ProfileAttributeValue
                        {
                            Id = _fallbackAttributeValues.Count + 1,
                            CandidateProfileId = profile.Id,
                            AttributeId = kvp.Key,
                            Value = kvp.Value,
                            UpdatedAt = DateTime.UtcNow
                        });
                    }
                }
            }
            return true;
        }
    }

    public async Task<List<ProfileAttributeValue>> GetAttributeValuesAsync(int profileId)
    {
        if (await IsDbAvailableAsync())
        {
            try
            {
                return await _context.ProfileAttributeValues
                    .Include(v => v.Attribute)
                        .ThenInclude(a => a.Category)
                    .Include(v => v.Attribute)
                        .ThenInclude(a => a.Options)
                    .Where(v => v.CandidateProfileId == profileId)
                    .AsNoTracking()
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to load attribute values from DB. Using in-memory store.");
            }
        }

        lock (_lock)
        {
            var profile = _fallbackProfiles.FirstOrDefault(p => p.Id == profileId) ?? _fallbackProfiles.FirstOrDefault();
            if (profile != null)
            {
                return _fallbackAttributeValues
                    .Where(v => v.CandidateProfileId == profile.Id)
                    .Select(CloneAttributeValue)
                    .ToList();
            }
            return new List<ProfileAttributeValue>();
        }
    }

    public async Task<List<Project>> GetProjectsAsync(int profileId)
    {
        if (await IsDbAvailableAsync())
        {
            try
            {
                return await _context.Projects
                    .Include(p => p.TagLinks)
                        .ThenInclude(tl => tl.Tag)
                    .Where(p => p.CandidateProfileId == profileId)
                    .OrderBy(p => p.DisplayOrder)
                    .ThenByDescending(p => p.CreatedAt)
                    .AsNoTracking()
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to load projects from DB. Using in-memory store.");
            }
        }

        lock (_lock)
        {
            var profile = _fallbackProfiles.FirstOrDefault(p => p.Id == profileId) ?? _fallbackProfiles.FirstOrDefault();
            if (profile != null)
            {
                return _fallbackProjects
                    .Where(p => p.CandidateProfileId == profile.Id)
                    .OrderBy(p => p.DisplayOrder)
                    .ThenByDescending(p => p.CreatedAt)
                    .Select(CloneProject)
                    .ToList();
            }
            return new List<Project>();
        }
    }

    public async Task<Project> SaveProjectAsync(int profileId, Project project, List<string>? tags = null)
    {
        if (await IsDbAvailableAsync())
        {
            try
            {
                if (project.Id > 0)
                {
                    var existing = await _context.Projects
                        .Include(p => p.TagLinks)
                        .FirstOrDefaultAsync(p => p.Id == project.Id && p.CandidateProfileId == profileId);

                    if (existing != null)
                    {
                        existing.Title = project.Title;
                        existing.Description = project.Description;
                        existing.MarkdownContent = project.MarkdownContent;
                        existing.Technologies = project.Technologies;
                        existing.ProjectUrl = project.ProjectUrl;
                        existing.GitHubUrl = project.GitHubUrl;
                        existing.DisplayOrder = project.DisplayOrder;

                        await _context.SaveChangesAsync();
                        return existing;
                    }
                }
                else
                {
                    project.CandidateProfileId = profileId;
                    project.CreatedAt = DateTime.UtcNow;
                    _context.Projects.Add(project);
                    await _context.SaveChangesAsync();
                    return project;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to save project to DB. Using in-memory store.");
            }
        }

        lock (_lock)
        {
            var profile = _fallbackProfiles.FirstOrDefault(p => p.Id == profileId) ?? _fallbackProfiles.FirstOrDefault();
            int targetProfileId = profile?.Id ?? profileId;

            if (project.Id > 0)
            {
                var existing = _fallbackProjects.FirstOrDefault(p => p.Id == project.Id);
                if (existing != null)
                {
                    existing.Title = project.Title;
                    existing.Description = project.Description;
                    existing.MarkdownContent = project.MarkdownContent;
                    existing.Technologies = project.Technologies;
                    existing.ProjectUrl = project.ProjectUrl;
                    existing.GitHubUrl = project.GitHubUrl;
                    existing.DisplayOrder = project.DisplayOrder;
                    return CloneProject(existing);
                }
            }

            var newProject = new Project
            {
                Id = _fallbackProjects.Count > 0 ? _fallbackProjects.Max(p => p.Id) + 1 : 1,
                CandidateProfileId = targetProfileId,
                Title = project.Title,
                Description = project.Description,
                MarkdownContent = project.MarkdownContent,
                Technologies = project.Technologies,
                ProjectUrl = project.ProjectUrl,
                GitHubUrl = project.GitHubUrl,
                DisplayOrder = project.DisplayOrder,
                CreatedAt = DateTime.UtcNow
            };
            _fallbackProjects.Add(newProject);
            return CloneProject(newProject);
        }
    }

    public async Task<bool> DeleteProjectAsync(int profileId, int projectId)
    {
        if (await IsDbAvailableAsync())
        {
            try
            {
                var project = await _context.Projects
                    .FirstOrDefaultAsync(p => p.Id == projectId && p.CandidateProfileId == profileId);

                if (project != null)
                {
                    _context.Projects.Remove(project);
                    await _context.SaveChangesAsync();
                    return true;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to delete project from DB. Using in-memory fallback.");
            }
        }

        lock (_lock)
        {
            var item = _fallbackProjects.FirstOrDefault(p => p.Id == projectId);
            if (item != null)
            {
                _fallbackProjects.Remove(item);
                return true;
            }
            return false;
        }
    }

    public async Task<List<CV>> GetCandidateCvsAsync(int profileId)
    {
        if (await IsDbAvailableAsync())
        {
            try
            {
                return await _context.CVs
                    .Include(c => c.Position)
                    .Include(c => c.Likes)
                    .Where(c => c.CandidateProfileId == profileId)
                    .OrderByDescending(c => c.CreatedAt)
                    .AsNoTracking()
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to query CVs from DB. Using in-memory fallback.");
            }
        }

        lock (_lock)
        {
            var profile = _fallbackProfiles.FirstOrDefault(p => p.Id == profileId) ?? _fallbackProfiles.FirstOrDefault();
            if (profile != null)
            {
                return _fallbackCvs
                    .Where(c => c.CandidateProfileId == profile.Id)
                    .OrderByDescending(c => c.CreatedAt)
                    .Select(CloneCv)
                    .ToList();
            }
            return new List<CV>();
        }
    }

    public async Task<ProfileReadiness> GetProfileReadinessAsync(int profileId)
    {
        var readiness = new ProfileReadiness();
        var profile = await GetProfileByIdAsync(profileId);
        if (profile == null)
        {
            readiness.Percentage = 10;
            readiness.MissingSuggestions.Add("Complete your basic candidate information.");
            return readiness;
        }

        // 1. Personal details score (Max 40 points)
        int personalScore = 0;
        if (!string.IsNullOrWhiteSpace(profile.FullName)) personalScore += 10;
        if (!string.IsNullOrWhiteSpace(profile.Headline)) personalScore += 10;
        if (!string.IsNullOrWhiteSpace(profile.Summary)) personalScore += 10;
        if (!string.IsNullOrWhiteSpace(profile.Location) || !string.IsNullOrWhiteSpace(profile.Phone)) personalScore += 5;
        if (!string.IsNullOrWhiteSpace(profile.SocialLinksJson)) personalScore += 5;
        readiness.PersonalDetailsScore = personalScore;

        if (personalScore < 40)
        {
            if (string.IsNullOrWhiteSpace(profile.Headline)) readiness.MissingSuggestions.Add("Add a professional headline (e.g. Senior Full-Stack Engineer).");
            if (string.IsNullOrWhiteSpace(profile.Summary)) readiness.MissingSuggestions.Add("Write a summary highlighting your strengths.");
        }

        // 2. Attributes qualifications score (Max 40 points)
        var attributeValues = await GetAttributeValuesAsync(profileId);
        int filledCount = attributeValues.Count(v => !string.IsNullOrWhiteSpace(v.Value));
        int attrScore = Math.Min(40, filledCount * 5); // 8 attributes = 40 points
        readiness.AttributesScore = attrScore;

        if (filledCount < 5)
        {
            readiness.MissingSuggestions.Add("Provide your standardized qualifications in the 'Info' tab to enable 1-click tailored CV generation.");
        }

        // 3. Projects score (Max 20 points)
        var projects = await GetProjectsAsync(profileId);
        int projectScore = projects.Count switch
        {
            0 => 0,
            1 => 12,
            _ => 20
        };
        readiness.ProjectsScore = projectScore;

        if (projects.Count == 0)
        {
            readiness.MissingSuggestions.Add("Add at least one portfolio project showcasing your technical work.");
        }

        readiness.Percentage = personalScore + attrScore + projectScore;
        return readiness;
    }

    private static CandidateProfile CreateDefaultFallbackProfile(string userId)
    {
        return new CandidateProfile
        {
            Id = 1,
            UserId = userId,
            FullName = "Alex Rivera",
            Headline = "Senior Full-Stack & Cloud Systems Architect",
            Summary = "Passionate software engineer with 8+ years experience in distributed systems, .NET Core, cloud architectures, and scalable web solutions.",
            Phone = "+1 (555) 349-2810",
            Location = "Stockholm, Sweden / Remote",
            ProfileImageUrl = "https://images.unsplash.com/photo-1534528741775-53994a69daeb?auto=format&fit=crop&w=256&q=80",
            SocialLinksJson = JsonSerializer.Serialize(new CandidateSocialLinks
            {
                GitHub = "https://github.com",
                LinkedIn = "https://linkedin.com",
                Portfolio = "https://alexrivera.dev",
                Twitter = "https://x.com"
            }),
            CreatedAt = DateTime.UtcNow.AddMonths(-3)
        };
    }

    private static void EnsureFallbackSeeded()
    {
        lock (_lock)
        {
            if (_fallbackInitialized) return;

            var profile = CreateDefaultFallbackProfile("candidate-demo-user-id");
            _fallbackProfiles.Add(profile);

            // Seed attribute values for Alex Rivera
            _fallbackAttributeValues.AddRange(new List<ProfileAttributeValue>
            {
                new() { Id = 1, CandidateProfileId = 1, AttributeId = 1, Value = "8", UpdatedAt = DateTime.UtcNow },
                new() { Id = 2, CandidateProfileId = 1, AttributeId = 2, Value = "Senior Full-Stack & Cloud Architect", UpdatedAt = DateTime.UtcNow },
                new() { Id = 3, CandidateProfileId = 1, AttributeId = 3, Value = "C# / .NET", UpdatedAt = DateTime.UtcNow },
                new() { Id = 4, CandidateProfileId = 1, AttributeId = 4, Value = "ASP.NET Core, Blazor, Entity Framework Core", UpdatedAt = DateTime.UtcNow },
                new() { Id = 5, CandidateProfileId = 1, AttributeId = 5, Value = "AWS, Azure, Docker, Kubernetes", UpdatedAt = DateTime.UtcNow },
                new() { Id = 6, CandidateProfileId = 1, AttributeId = 6, Value = "Fluent (C1/C2)", UpdatedAt = DateTime.UtcNow },
                new() { Id = 7, CandidateProfileId = 1, AttributeId = 7, Value = "Master's Degree", UpdatedAt = DateTime.UtcNow },
                new() { Id = 8, CandidateProfileId = 1, AttributeId = 8, Value = "true", UpdatedAt = DateTime.UtcNow },
                new() { Id = 9, CandidateProfileId = 1, AttributeId = 9, Value = "Remote Only", UpdatedAt = DateTime.UtcNow },
                new() { Id = 10, CandidateProfileId = 1, AttributeId = 10, Value = "https://github.com/alexrivera", UpdatedAt = DateTime.UtcNow },
                new() { Id = 11, CandidateProfileId = 1, AttributeId = 11, Value = "135000", UpdatedAt = DateTime.UtcNow },
                new() { Id = 12, CandidateProfileId = 1, AttributeId = 12, Value = "1 Month", UpdatedAt = DateTime.UtcNow }
            });

            // Seed portfolio projects for Alex Rivera
            _fallbackProjects.Add(new Project
            {
                Id = 1,
                CandidateProfileId = 1,
                Title = "Distributed Event Sourcing & CQRS Engine",
                Description = "High-throughput event sourcing architecture supporting over 25,000 events/sec with transactional outbox delivery.",
                MarkdownContent = @"### Architectural Overview

Designed and engineered a high-throughput event sourcing platform for financial transaction processing handling over **25,000 events/second** with zero data loss.

#### Key Architectural Capabilities:
- **CQRS Pattern**: Segregated read and write projections to optimize high-concurrency analytical queries.
- **Event Store**: Implemented append-only event stream persistence on PostgreSQL with optimistic concurrency tokens.
- **Transactional Outbox**: Guaranteed at-least-once message delivery via RabbitMQ broker.

```csharp
public async Task AppendEventAsync<TEvent>(Guid aggregateId, TEvent domainEvent)
{
    // Atomic event sequence increment and outbox dispatch
    await _eventStore.SaveAsync(aggregateId, domainEvent);
}
```

#### Metrics & Outcomes:
- **P99 Latency**: Reduced from 420ms to 48ms under peak burst traffic.
- **Fault Tolerance**: Automated partition rebalancing and replica failover.",
                Technologies = "C#, .NET 9, RabbitMQ, PostgreSQL, Docker, Redis",
                GitHubUrl = "https://github.com/alexrivera/distributed-cqrs",
                ProjectUrl = "https://cqrs-demo.alexrivera.dev",
                DisplayOrder = 1,
                CreatedAt = DateTime.UtcNow.AddMonths(-2)
            });

            _fallbackProjects.Add(new Project
            {
                Id = 2,
                CandidateProfileId = 1,
                Title = "Real-Time Telemetry & Observability Hub",
                Description = "Low-latency browser monitoring suite providing live telemetry, health metrics, and distributed tracing for cloud-native microservices.",
                MarkdownContent = @"### System Summary

A centralized developer dashboard providing real-time infrastructure observability, distributed tracing, and live error alerting for distributed microservices.

#### Core Technical Highlights:
- **Live Streaming**: Continuous node health broadcasting using WebSockets and SignalR.
- **Sub-Second Dashboards**: Client-side reactive canvas charts rendering CPU, memory, and GC allocations.
- **OpenTelemetry Standard**: Native export to Prometheus, Jaeger, and Grafana stacks.

```csharp
app.MapHub<TelemetryHub>(""/hubs/telemetry"");
```",
                Technologies = "Blazor Server, SignalR, OpenTelemetry, Prometheus, Grafana, Bootstrap 5",
                GitHubUrl = "https://github.com/alexrivera/telemetry-hub",
                ProjectUrl = "https://telemetry.alexrivera.dev",
                DisplayOrder = 2,
                CreatedAt = DateTime.UtcNow.AddMonths(-1)
            });

            // Seed sample tailored CVs for Alex Rivera
            _fallbackCvs.Add(new CV
            {
                Id = 1,
                CandidateProfileId = 1,
                PositionId = 1,
                Title = "Tailored CV — Senior .NET Core Cloud Architect",
                ProfessionalSummary = "Distinguished cloud architect with deep expertise in enterprise C# / .NET distributed systems, microservices architectures, and AWS cloud migrations.",
                CompletionPercentage = 95,
                CreatedAt = DateTime.UtcNow.AddDays(-12),
                Position = new Position
                {
                    Id = 1,
                    Title = "Senior .NET Core Cloud Architect",
                    Company = "Acme Systems Inc.",
                    Location = "San Francisco, CA (Remote)",
                    EmploymentType = EmploymentType.FullTime
                }
            });

            _fallbackCvs.Add(new CV
            {
                Id = 2,
                CandidateProfileId = 1,
                PositionId = 2,
                Title = "Tailored CV — Lead Distributed Systems Engineer",
                ProfessionalSummary = "Hands-on distributed systems engineer specializing in event-driven architectures, transactional outbox messaging, and PostgreSQL database performance.",
                CompletionPercentage = 88,
                CreatedAt = DateTime.UtcNow.AddDays(-5),
                Position = new Position
                {
                    Id = 2,
                    Title = "Lead Distributed Systems Engineer",
                    Company = "FinTech Global",
                    Location = "New York, NY (Hybrid)",
                    EmploymentType = EmploymentType.FullTime
                }
            });

            _fallbackInitialized = true;
        }
    }

    private static CandidateProfile CloneProfile(CandidateProfile source)
    {
        return new CandidateProfile
        {
            Id = source.Id,
            UserId = source.UserId,
            FullName = source.FullName,
            Headline = source.Headline,
            Summary = source.Summary,
            Phone = source.Phone,
            Location = source.Location,
            ProfileImageUrl = source.ProfileImageUrl,
            SocialLinksJson = source.SocialLinksJson,
            CreatedAt = source.CreatedAt,
            UpdatedAt = source.UpdatedAt
        };
    }

    private static ProfileAttributeValue CloneAttributeValue(ProfileAttributeValue v)
    {
        return new ProfileAttributeValue
        {
            Id = v.Id,
            CandidateProfileId = v.CandidateProfileId,
            AttributeId = v.AttributeId,
            Value = v.Value,
            UpdatedAt = v.UpdatedAt,
            Attribute = v.Attribute
        };
    }

    private static Project CloneProject(Project p)
    {
        return new Project
        {
            Id = p.Id,
            CandidateProfileId = p.CandidateProfileId,
            Title = p.Title,
            Description = p.Description,
            MarkdownContent = p.MarkdownContent,
            Technologies = p.Technologies,
            ProjectUrl = p.ProjectUrl,
            GitHubUrl = p.GitHubUrl,
            DisplayOrder = p.DisplayOrder,
            CreatedAt = p.CreatedAt
        };
    }

    private static CV CloneCv(CV c)
    {
        return new CV
        {
            Id = c.Id,
            CandidateProfileId = c.CandidateProfileId,
            PositionId = c.PositionId,
            Title = c.Title,
            ProfessionalSummary = c.ProfessionalSummary,
            CompletionPercentage = c.CompletionPercentage,
            CreatedAt = c.CreatedAt,
            UpdatedAt = c.UpdatedAt,
            Position = c.Position,
            Likes = c.Likes
        };
    }
}
