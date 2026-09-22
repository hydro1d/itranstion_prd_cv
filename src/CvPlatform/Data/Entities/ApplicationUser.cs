using Microsoft.AspNetCore.Identity;

namespace CvPlatform.Data.Entities;

/// <summary>
/// Extended IdentityUser representing system accounts (Candidate, Recruiter, Administrator).
/// </summary>
public class ApplicationUser : IdentityUser
{
    public string FullName { get; set; } = string.Empty;
    public string? AvatarUrl { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? LastLoginAt { get; set; }

    // Navigation properties
    public virtual CandidateProfile? CandidateProfile { get; set; }
    public virtual ICollection<Position> CreatedPositions { get; set; } = new List<Position>();
    public virtual ICollection<CVLike> GivenLikes { get; set; } = new List<CVLike>();
    public virtual ICollection<DiscussionMessage> DiscussionMessages { get; set; } = new List<DiscussionMessage>();
}
