namespace CvPlatform.Data.Entities;

/// <summary>
/// Master Candidate Profile containing the 4 PRD sections (Me, Info, Projects, CVs).
/// </summary>
public class CandidateProfile
{
    public int Id { get; set; }
    public string UserId { get; set; } = string.Empty;
    
    // Section 1: "Me" (Personal details)
    public string FullName { get; set; } = string.Empty;
    public string? Headline { get; set; }
    public string? Summary { get; set; }
    public string? Phone { get; set; }
    public string? Location { get; set; }
    public string? ProfileImageUrl { get; set; }
    public string? SocialLinksJson { get; set; } // GitHub, LinkedIn, Website, etc.

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    // Navigation properties
    public virtual ApplicationUser User { get; set; } = null!;
    
    // Section 2: "Info" (Attribute values from reusable library)
    public virtual ICollection<ProfileAttributeValue> AttributeValues { get; set; } = new List<ProfileAttributeValue>();
    
    // Section 3: "Projects"
    public virtual ICollection<Project> Projects { get; set; } = new List<Project>();
    
    // Section 4: "CVs" (Generated CVs for positions)
    public virtual ICollection<CV> CVs { get; set; } = new List<CV>();
}
