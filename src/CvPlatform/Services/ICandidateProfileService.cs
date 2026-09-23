using CvPlatform.Data.Entities;

namespace CvPlatform.Services;

public class CandidateSocialLinks
{
    public string? GitHub { get; set; }
    public string? LinkedIn { get; set; }
    public string? Portfolio { get; set; }
    public string? Twitter { get; set; }
}

public class ProfileReadiness
{
    public int Percentage { get; set; }
    public int PersonalDetailsScore { get; set; }
    public int AttributesScore { get; set; }
    public int ProjectsScore { get; set; }
    public List<string> MissingSuggestions { get; set; } = new();
}

/// <summary>
/// Service contract for Candidate Master Profile (The 4 PRD sections: Me, Info, Projects, CVs).
/// </summary>
public interface ICandidateProfileService
{
    Task<CandidateProfile?> GetProfileByUserIdAsync(string userId);
    Task<CandidateProfile?> GetProfileByIdAsync(int profileId);
    Task<CandidateProfile> SavePersonalDetailsAsync(int profileId, CandidateProfile profileData, CandidateSocialLinks? socialLinks = null);
    Task<bool> SaveAttributeValuesAsync(int profileId, Dictionary<int, string> attributeValues);
    Task<List<ProfileAttributeValue>> GetAttributeValuesAsync(int profileId);
    Task<List<Project>> GetProjectsAsync(int profileId);
    Task<Project> SaveProjectAsync(int profileId, Project project, List<string>? tags = null);
    Task<bool> DeleteProjectAsync(int profileId, int projectId);
    Task<List<CV>> GetCandidateCvsAsync(int profileId);
    Task<ProfileReadiness> GetProfileReadinessAsync(int profileId);
}
