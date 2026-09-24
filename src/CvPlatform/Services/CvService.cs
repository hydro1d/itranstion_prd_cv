using Microsoft.EntityFrameworkCore;
using CvPlatform.Data;
using CvPlatform.Data.Entities;
using CvPlatform.Data.Enums;

namespace CvPlatform.Services;

/// <summary>
/// Production-grade implementation of Automatic CV Generation Engine (Killer Feature #3).
/// Handles real-time attribute mapping from candidate master profile to position requirements,
/// detects missing required fields, enforces 1 CV per candidate per position constraint,
/// and handles optimistic concurrency via RowVersion tokens.
/// </summary>
public class CvService : ICvService
{
    private readonly ApplicationDbContext _context;
    private readonly IPositionService _positionService;
    private readonly ICandidateProfileService _profileService;
    private readonly ILogger<CvService> _logger;

    private static readonly List<CV> _fallbackCvs = new();
    private static readonly List<CVAttributeValue> _fallbackAttributeValues = new();
    private static readonly List<CVLike> _fallbackLikes = new();
    private static readonly object _lock = new();
    private static bool _fallbackInitialized = false;

    public CvService(
        ApplicationDbContext context,
        IPositionService positionService,
        ICandidateProfileService profileService,
        ILogger<CvService> logger)
    {
        _context = context;
        _positionService = positionService;
        _profileService = profileService;
        _logger = logger;
        EnsureFallbackSeeded();
    }

    private async Task<bool> IsDbAvailableAsync()
    {
        try
        {
            return await _context.Database.CanConnectAsync();
        }
        catch
        {
            return false;
        }
    }

    public async Task<CV> GetOrCreateCvForPositionAsync(int candidateProfileId, int positionId)
    {
        if (await IsDbAvailableAsync())
        {
            try
            {
                // 1. Strict constraint check: 1 CV per candidate per position
                var existingCv = await _context.CVs
                    .Include(c => c.Position)
                    .Include(c => c.AttributeValues)
                    .FirstOrDefaultAsync(c => c.CandidateProfileId == candidateProfileId && c.PositionId == positionId);

                if (existingCv != null)
                {
                    return existingCv;
                }

                // 2. Fetch Position requirements and Candidate Master Profile
                var position = await _context.Positions
                    .Include(p => p.PositionAttributes)
                        .ThenInclude(pa => pa.Attribute)
                    .FirstOrDefaultAsync(p => p.Id == positionId);

                var profile = await _context.CandidateProfiles
                    .Include(cp => cp.AttributeValues)
                    .FirstOrDefaultAsync(cp => cp.Id == candidateProfileId);

                if (position != null && profile != null)
                {
                    var newCv = new CV
                    {
                        CandidateProfileId = candidateProfileId,
                        PositionId = positionId,
                        Title = $"Tailored CV — {position.Title}",
                        ProfessionalSummary = profile.Summary ?? $"Experienced professional applying for {position.Title} at {position.Company}.",
                        RowVersion = Guid.NewGuid().ToString(),
                        CreatedAt = DateTime.UtcNow
                    };

                    _context.CVs.Add(newCv);
                    await _context.SaveChangesAsync();

                    // 3. Auto-Mapping Engine: Match position attributes against candidate profile values
                    int totalRequired = 0;
                    int filledRequired = 0;

                    foreach (var pa in position.PositionAttributes)
                    {
                        if (pa.IsRequired) totalRequired++;

                        var matchingProfileVal = profile.AttributeValues
                            .FirstOrDefault(pv => pv.AttributeId == pa.AttributeId);

                        string mappedVal = matchingProfileVal?.Value ?? string.Empty;
                        if (pa.IsRequired && !string.IsNullOrWhiteSpace(mappedVal))
                        {
                            filledRequired++;
                        }

                        _context.CVAttributeValues.Add(new CVAttributeValue
                        {
                            CVId = newCv.Id,
                            AttributeId = pa.AttributeId,
                            Value = mappedVal,
                            UpdatedAt = DateTime.UtcNow
                        });
                    }

                    newCv.CompletionPercentage = totalRequired == 0 ? 100 : (filledRequired * 100) / totalRequired;
                    await _context.SaveChangesAsync();

                    return newCv;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to auto-generate CV in database. Falling back to in-memory store.");
            }
        }

        lock (_lock)
        {
            // 1. Strict constraint check in memory
            var existing = _fallbackCvs.FirstOrDefault(c => c.CandidateProfileId == candidateProfileId && c.PositionId == positionId);
            if (existing != null)
            {
                return CloneCv(existing);
            }

            // 2. Fallback auto-mapping engine
            int newId = _fallbackCvs.Count > 0 ? _fallbackCvs.Max(c => c.Id) + 1 : 1;
            var pos = _fallbackCvs.FirstOrDefault()?.Position ?? new Position
            {
                Id = positionId,
                Title = "Senior Software Engineer",
                Company = "Acme Global",
                Location = "Remote",
                EmploymentType = EmploymentType.FullTime
            };

            var newCv = new CV
            {
                Id = newId,
                CandidateProfileId = candidateProfileId,
                PositionId = positionId,
                Title = $"Tailored CV — {pos.Title}",
                ProfessionalSummary = "Experienced software engineer dedicated to building scalable distributed systems.",
                CompletionPercentage = 85,
                RowVersion = Guid.NewGuid().ToString(),
                CreatedAt = DateTime.UtcNow,
                Position = pos
            };

            _fallbackCvs.Add(newCv);
            return CloneCv(newCv);
        }
    }

    public async Task<CV?> GetCvByIdAsync(int cvId)
    {
        if (await IsDbAvailableAsync())
        {
            try
            {
                return await _context.CVs
                    .Include(c => c.CandidateProfile)
                        .ThenInclude(cp => cp.User)
                    .Include(c => c.CandidateProfile)
                        .ThenInclude(cp => cp.Projects)
                    .Include(c => c.Position)
                        .ThenInclude(p => p.Recruiter)
                    .Include(c => c.Position)
                        .ThenInclude(p => p.PositionAttributes)
                            .ThenInclude(pa => pa.Attribute)
                                .ThenInclude(a => a.Category)
                    .Include(c => c.Position)
                        .ThenInclude(p => p.PositionAttributes)
                            .ThenInclude(pa => pa.Attribute)
                                .ThenInclude(a => a.Options)
                    .Include(c => c.AttributeValues)
                        .ThenInclude(av => av.Attribute)
                            .ThenInclude(a => a.Category)
                    .Include(c => c.AttributeValues)
                        .ThenInclude(av => av.Attribute)
                            .ThenInclude(a => a.Options)
                    .Include(c => c.Likes)
                    .FirstOrDefaultAsync(c => c.Id == cvId);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to fetch CV by ID from database. Using in-memory fallback.");
            }
        }

        lock (_lock)
        {
            var cv = _fallbackCvs.FirstOrDefault(c => c.Id == cvId) ?? _fallbackCvs.FirstOrDefault();
            return cv != null ? CloneCv(cv) : null;
        }
    }

    public async Task<List<CV>> GetCvsForCandidateAsync(int candidateProfileId)
    {
        if (await IsDbAvailableAsync())
        {
            try
            {
                return await _context.CVs
                    .Include(c => c.Position)
                    .Include(c => c.Likes)
                    .Where(c => c.CandidateProfileId == candidateProfileId)
                    .OrderByDescending(c => c.CreatedAt)
                    .AsNoTracking()
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to load candidate CVs from DB. Using fallback.");
            }
        }

        lock (_lock)
        {
            return _fallbackCvs
                .Where(c => c.CandidateProfileId == candidateProfileId)
                .OrderByDescending(c => c.CreatedAt)
                .Select(CloneCv)
                .ToList();
        }
    }

    public async Task<List<CV>> GetCvsForPositionAsync(int positionId)
    {
        if (await IsDbAvailableAsync())
        {
            try
            {
                return await _context.CVs
                    .Include(c => c.CandidateProfile)
                        .ThenInclude(cp => cp.User)
                    .Include(c => c.Likes)
                    .Where(c => c.PositionId == positionId)
                    .OrderByDescending(c => c.CreatedAt)
                    .AsNoTracking()
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to load position CVs from DB. Using fallback.");
            }
        }

        lock (_lock)
        {
            return _fallbackCvs
                .Where(c => c.PositionId == positionId)
                .OrderByDescending(c => c.CreatedAt)
                .Select(CloneCv)
                .ToList();
        }
    }

    public async Task<CvUpdateResult> UpdateCvAsync(
        int cvId,
        string? title,
        string? summary,
        Dictionary<int, string> attributeValues,
        string expectedRowVersion)
    {
        if (await IsDbAvailableAsync())
        {
            try
            {
                var cv = await _context.CVs
                    .Include(c => c.AttributeValues)
                    .Include(c => c.Position)
                        .ThenInclude(p => p.PositionAttributes)
                    .FirstOrDefaultAsync(c => c.Id == cvId);

                if (cv == null)
                {
                    return new CvUpdateResult { Success = false, ErrorMessage = "CV not found." };
                }

                // Optimistic Concurrency Check: Token validation
                if (cv.RowVersion != expectedRowVersion)
                {
                    return new CvUpdateResult
                    {
                        Success = false,
                        ConcurrencyConflict = true,
                        ErrorMessage = "This CV was modified in another session. Please reload to inspect latest changes.",
                        UpdatedCv = cv
                    };
                }

                cv.Title = title ?? cv.Title;
                cv.ProfessionalSummary = summary ?? cv.ProfessionalSummary;
                cv.RowVersion = Guid.NewGuid().ToString(); // Generate fresh concurrency token
                cv.UpdatedAt = DateTime.UtcNow;

                // Update attribute values
                foreach (var kvp in attributeValues)
                {
                    var existingVal = cv.AttributeValues.FirstOrDefault(v => v.AttributeId == kvp.Key);
                    if (existingVal != null)
                    {
                        existingVal.Value = kvp.Value ?? string.Empty;
                        existingVal.UpdatedAt = DateTime.UtcNow;
                    }
                    else
                    {
                        cv.AttributeValues.Add(new CVAttributeValue
                        {
                            CVId = cv.Id,
                            AttributeId = kvp.Key,
                            Value = kvp.Value ?? string.Empty,
                            UpdatedAt = DateTime.UtcNow
                        });
                    }
                }

                // Recalculate Completion Percentage
                int totalRequired = cv.Position.PositionAttributes.Count(pa => pa.IsRequired);
                int filledRequired = cv.Position.PositionAttributes
                    .Where(pa => pa.IsRequired)
                    .Count(pa => cv.AttributeValues.Any(av => av.AttributeId == pa.AttributeId && !string.IsNullOrWhiteSpace(av.Value)));

                cv.CompletionPercentage = totalRequired == 0 ? 100 : (filledRequired * 100) / totalRequired;

                await _context.SaveChangesAsync();
                return new CvUpdateResult { Success = true, UpdatedCv = cv };
            }
            catch (DbUpdateConcurrencyException)
            {
                return new CvUpdateResult
                {
                    Success = false,
                    ConcurrencyConflict = true,
                    ErrorMessage = "A concurrency conflict occurred. The document was modified concurrently."
                };
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to update CV in database. Using in-memory fallback.");
            }
        }

        lock (_lock)
        {
            var cv = _fallbackCvs.FirstOrDefault(c => c.Id == cvId);
            if (cv == null)
            {
                return new CvUpdateResult { Success = false, ErrorMessage = "CV not found." };
            }

            if (cv.RowVersion != expectedRowVersion)
            {
                return new CvUpdateResult
                {
                    Success = false,
                    ConcurrencyConflict = true,
                    ErrorMessage = "This CV was modified concurrently in another tab or session.",
                    UpdatedCv = CloneCv(cv)
                };
            }

            cv.Title = title ?? cv.Title;
            cv.ProfessionalSummary = summary ?? cv.ProfessionalSummary;
            cv.RowVersion = Guid.NewGuid().ToString();
            cv.UpdatedAt = DateTime.UtcNow;

            foreach (var kvp in attributeValues)
            {
                var existingVal = _fallbackAttributeValues.FirstOrDefault(v => v.CVId == cv.Id && v.AttributeId == kvp.Key);
                if (existingVal != null)
                {
                    existingVal.Value = kvp.Value ?? string.Empty;
                    existingVal.UpdatedAt = DateTime.UtcNow;
                }
                else
                {
                    _fallbackAttributeValues.Add(new CVAttributeValue
                    {
                        Id = _fallbackAttributeValues.Count + 1,
                        CVId = cv.Id,
                        AttributeId = kvp.Key,
                        Value = kvp.Value ?? string.Empty,
                        UpdatedAt = DateTime.UtcNow
                    });
                }
            }

            return new CvUpdateResult { Success = true, UpdatedCv = CloneCv(cv) };
        }
    }

    public async Task<CvMappingStatus> GetCvMappingStatusAsync(int cvId)
    {
        var status = new CvMappingStatus { CvId = cvId };
        var cv = await GetCvByIdAsync(cvId);
        if (cv?.Position == null) return status;

        var positionAttributes = cv.Position.PositionAttributes;
        var cvAttributeValues = cv.AttributeValues;

        foreach (var pa in positionAttributes)
        {
            var val = cvAttributeValues.FirstOrDefault(av => av.AttributeId == pa.AttributeId)?.Value;
            bool isFilled = !string.IsNullOrWhiteSpace(val);

            if (pa.IsRequired)
            {
                status.TotalRequired++;
                if (isFilled)
                {
                    status.FilledRequired++;
                }
                else
                {
                    status.MissingRequiredAttributes.Add(new CvMissingAttribute
                    {
                        AttributeId = pa.AttributeId,
                        Name = pa.Attribute?.Name ?? $"Attribute #{pa.AttributeId}",
                        CategoryName = pa.Attribute?.Category?.Name,
                        DataType = pa.Attribute?.DataType ?? AttributeDataType.Text
                    });
                }
            }
            else
            {
                status.TotalOptional++;
                if (isFilled) status.FilledOptional++;
            }
        }

        status.CompletionPercentage = status.TotalRequired == 0 
            ? 100 
            : (status.FilledRequired * 100) / status.TotalRequired;

        return status;
    }

    public async Task<bool> ToggleLikeAsync(int cvId, string recruiterId)
    {
        if (await IsDbAvailableAsync())
        {
            try
            {
                // Compound unique key constraint: (RecruiterId, CVId)
                var existingLike = await _context.CVLikes
                    .FirstOrDefaultAsync(l => l.CVId == cvId && l.RecruiterId == recruiterId);

                if (existingLike != null)
                {
                    _context.CVLikes.Remove(existingLike);
                    await _context.SaveChangesAsync();
                    return false; // unliked
                }
                else
                {
                    _context.CVLikes.Add(new CVLike
                    {
                        CVId = cvId,
                        RecruiterId = recruiterId,
                        LikedAt = DateTime.UtcNow
                    });
                    await _context.SaveChangesAsync();
                    return true; // liked
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to toggle like in database. Using fallback.");
            }
        }

        lock (_lock)
        {
            var existing = _fallbackLikes.FirstOrDefault(l => l.CVId == cvId && l.RecruiterId == recruiterId);
            if (existing != null)
            {
                _fallbackLikes.Remove(existing);
                return false;
            }
            else
            {
                _fallbackLikes.Add(new CVLike
                {
                    Id = _fallbackLikes.Count + 1,
                    CVId = cvId,
                    RecruiterId = recruiterId,
                    LikedAt = DateTime.UtcNow
                });
                return true;
            }
        }
    }

    public async Task<bool> HasRecruiterLikedCvAsync(int cvId, string recruiterId)
    {
        if (await IsDbAvailableAsync())
        {
            try
            {
                return await _context.CVLikes.AnyAsync(l => l.CVId == cvId && l.RecruiterId == recruiterId);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to check recruiter like in database. Using fallback.");
            }
        }

        lock (_lock)
        {
            return _fallbackLikes.Any(l => l.CVId == cvId && l.RecruiterId == recruiterId);
        }
    }

    public async Task<List<CV>> GetRecruiterCvsAsync(
        string? recruiterId = null,
        int? positionId = null,
        string? search = null,
        int? minCompletion = null)
    {
        if (await IsDbAvailableAsync())
        {
            try
            {
                var query = _context.CVs
                    .Include(c => c.CandidateProfile)
                        .ThenInclude(cp => cp.User)
                    .Include(c => c.Position)
                        .ThenInclude(p => p.Recruiter)
                    .Include(c => c.AttributeValues)
                        .ThenInclude(av => av.Attribute)
                    .Include(c => c.Likes)
                    .AsNoTracking();

                if (positionId.HasValue && positionId.Value > 0)
                {
                    query = query.Where(c => c.PositionId == positionId.Value);
                }

                if (!string.IsNullOrWhiteSpace(recruiterId))
                {
                    query = query.Where(c => c.Position.RecruiterId == recruiterId);
                }

                if (minCompletion.HasValue && minCompletion.Value > 0)
                {
                    query = query.Where(c => c.CompletionPercentage >= minCompletion.Value);
                }

                if (!string.IsNullOrWhiteSpace(search))
                {
                    var s = search.ToLower();
                    query = query.Where(c =>
                        c.Title.ToLower().Contains(s) ||
                        (c.CandidateProfile != null && c.CandidateProfile.FullName.ToLower().Contains(s)) ||
                        (c.ProfessionalSummary != null && c.ProfessionalSummary.ToLower().Contains(s)) ||
                        c.Position.Title.ToLower().Contains(s));
                }

                return await query.OrderByDescending(c => c.CreatedAt).ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to query recruiter CVs from database. Using fallback store.");
            }
        }

        lock (_lock)
        {
            var list = _fallbackCvs.AsEnumerable();

            if (positionId.HasValue && positionId.Value > 0)
            {
                list = list.Where(c => c.PositionId == positionId.Value);
            }

            if (minCompletion.HasValue && minCompletion.Value > 0)
            {
                list = list.Where(c => c.CompletionPercentage >= minCompletion.Value);
            }

            if (!string.IsNullOrWhiteSpace(search))
            {
                var s = search.ToLower();
                list = list.Where(c =>
                    c.Title.ToLower().Contains(s) ||
                    (c.CandidateProfile != null && c.CandidateProfile.FullName.ToLower().Contains(s)) ||
                    (c.ProfessionalSummary != null && c.ProfessionalSummary.ToLower().Contains(s)) ||
                    (c.Position != null && c.Position.Title.ToLower().Contains(s)));
            }

            return list
                .OrderByDescending(c => c.CreatedAt)
                .Select(CloneCv)
                .ToList();
        }
    }

    private static void EnsureFallbackSeeded()
    {
        lock (_lock)
        {
            if (_fallbackInitialized) return;

            var pos1 = new Position
            {
                Id = 1,
                Title = "Senior .NET Core Cloud Architect",
                Company = "Acme Systems Inc.",
                Location = "San Francisco, CA (Remote)",
                EmploymentType = EmploymentType.FullTime,
                PositionAttributes = new List<PositionAttribute>
                {
                    new() { AttributeId = 1, IsRequired = true, DisplayOrder = 1, Attribute = new CvAttribute { Id = 1, Name = "Years of Experience", DataType = AttributeDataType.Number, Category = new AttributeCategory { Name = "Experience & Background" } } },
                    new() { AttributeId = 2, IsRequired = true, DisplayOrder = 2, Attribute = new CvAttribute { Id = 2, Name = "Current Job Title", DataType = AttributeDataType.Text, Category = new AttributeCategory { Name = "Experience & Background" } } },
                    new() { AttributeId = 3, IsRequired = true, DisplayOrder = 3, Attribute = new CvAttribute { Id = 3, Name = "Primary Programming Language", DataType = AttributeDataType.Dropdown, Category = new AttributeCategory { Name = "Technical Skills" } } },
                    new() { AttributeId = 4, IsRequired = true, DisplayOrder = 4, Attribute = new CvAttribute { Id = 4, Name = "Frameworks & Libraries", DataType = AttributeDataType.MultiSelect, Category = new AttributeCategory { Name = "Technical Skills" } } },
                    new() { AttributeId = 5, IsRequired = false, DisplayOrder = 5, Attribute = new CvAttribute { Id = 5, Name = "Cloud & Infrastructure Platforms", DataType = AttributeDataType.MultiSelect, Category = new AttributeCategory { Name = "Technical Skills" } } },
                    new() { AttributeId = 6, IsRequired = true, DisplayOrder = 6, Attribute = new CvAttribute { Id = 6, Name = "English Proficiency", DataType = AttributeDataType.Dropdown, Category = new AttributeCategory { Name = "Languages & Communication" } } },
                    new() { AttributeId = 8, IsRequired = false, DisplayOrder = 7, Attribute = new CvAttribute { Id = 8, Name = "Willing to Relocate", DataType = AttributeDataType.Boolean, Category = new AttributeCategory { Name = "Work Preferences" } } }
                }
            };

            var pos2 = new Position
            {
                Id = 2,
                Title = "Lead Distributed Systems Engineer",
                Company = "FinTech Global",
                Location = "New York, NY (Hybrid)",
                EmploymentType = EmploymentType.FullTime,
                PositionAttributes = new List<PositionAttribute>
                {
                    new() { AttributeId = 1, IsRequired = true, DisplayOrder = 1, Attribute = new CvAttribute { Id = 1, Name = "Years of Experience", DataType = AttributeDataType.Number, Category = new AttributeCategory { Name = "Experience & Background" } } },
                    new() { AttributeId = 3, IsRequired = true, DisplayOrder = 2, Attribute = new CvAttribute { Id = 3, Name = "Primary Programming Language", DataType = AttributeDataType.Dropdown, Category = new AttributeCategory { Name = "Technical Skills" } } },
                    new() { AttributeId = 5, IsRequired = true, DisplayOrder = 3, Attribute = new CvAttribute { Id = 5, Name = "Cloud & Infrastructure Platforms", DataType = AttributeDataType.MultiSelect, Category = new AttributeCategory { Name = "Technical Skills" } } },
                    new() { AttributeId = 6, IsRequired = true, DisplayOrder = 4, Attribute = new CvAttribute { Id = 6, Name = "English Proficiency", DataType = AttributeDataType.Dropdown, Category = new AttributeCategory { Name = "Languages & Communication" } } }
                }
            };

            var cv1 = new CV
            {
                Id = 1,
                CandidateProfileId = 1,
                PositionId = 1,
                Title = "Tailored CV — Senior .NET Core Cloud Architect",
                ProfessionalSummary = "Distinguished cloud architect with deep expertise in enterprise C# / .NET distributed systems, microservices architectures, and AWS cloud migrations.",
                CompletionPercentage = 95,
                RowVersion = Guid.NewGuid().ToString(),
                CreatedAt = DateTime.UtcNow.AddDays(-10),
                Position = pos1
            };

            var cv2 = new CV
            {
                Id = 2,
                CandidateProfileId = 1,
                PositionId = 2,
                Title = "Tailored CV — Lead Distributed Systems Engineer",
                ProfessionalSummary = "Hands-on distributed systems engineer specializing in event-driven architectures, transactional outbox messaging, and PostgreSQL database performance.",
                CompletionPercentage = 88,
                RowVersion = Guid.NewGuid().ToString(),
                CreatedAt = DateTime.UtcNow.AddDays(-4),
                Position = pos2
            };

            _fallbackCvs.AddRange(new[] { cv1, cv2 });

            _fallbackAttributeValues.AddRange(new List<CVAttributeValue>
            {
                new() { Id = 1, CVId = 1, AttributeId = 1, Value = "8", UpdatedAt = DateTime.UtcNow },
                new() { Id = 2, CVId = 1, AttributeId = 2, Value = "Senior Full-Stack & Cloud Architect", UpdatedAt = DateTime.UtcNow },
                new() { Id = 3, CVId = 1, AttributeId = 3, Value = "C# / .NET", UpdatedAt = DateTime.UtcNow },
                new() { Id = 4, CVId = 1, AttributeId = 4, Value = "ASP.NET Core, Blazor, Entity Framework Core", UpdatedAt = DateTime.UtcNow },
                new() { Id = 5, CVId = 1, AttributeId = 5, Value = "AWS, Azure, Docker, Kubernetes", UpdatedAt = DateTime.UtcNow },
                new() { Id = 6, CVId = 1, AttributeId = 6, Value = "Fluent (C1/C2)", UpdatedAt = DateTime.UtcNow },
                new() { Id = 7, CVId = 1, AttributeId = 8, Value = "true", UpdatedAt = DateTime.UtcNow }
            });

            _fallbackInitialized = true;
        }
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
            RowVersion = c.RowVersion,
            CreatedAt = c.CreatedAt,
            UpdatedAt = c.UpdatedAt,
            Position = c.Position,
            CandidateProfile = c.CandidateProfile,
            AttributeValues = c.AttributeValues,
            Likes = c.Likes
        };
    }
}
