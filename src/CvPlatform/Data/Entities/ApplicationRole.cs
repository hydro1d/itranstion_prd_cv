using Microsoft.AspNetCore.Identity;

namespace CvPlatform.Data.Entities;

/// <summary>
/// Extended IdentityRole representing user roles (Candidate, Recruiter, Administrator).
/// </summary>
public class ApplicationRole : IdentityRole
{
    public string Description { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public const string Administrator = "Administrator";
    public const string Recruiter = "Recruiter";
    public const string Candidate = "Candidate";
}
