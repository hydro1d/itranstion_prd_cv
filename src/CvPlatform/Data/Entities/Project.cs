namespace CvPlatform.Data.Entities;

/// <summary>
/// Portfolio project created by candidate with Markdown description and tags (Section 3: Projects).
/// </summary>
public class Project
{
    public int Id { get; set; }
    public int CandidateProfileId { get; set; }
    
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? MarkdownContent { get; set; }
    public string? Technologies { get; set; }
    public string? ProjectUrl { get; set; }
    public string? GitHubUrl { get; set; }
    public int DisplayOrder { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public virtual CandidateProfile CandidateProfile { get; set; } = null!;
    public virtual ICollection<ProjectTagLink> TagLinks { get; set; } = new List<ProjectTagLink>();
}
