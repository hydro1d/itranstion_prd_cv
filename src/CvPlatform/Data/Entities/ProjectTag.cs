namespace CvPlatform.Data.Entities;

/// <summary>
/// Reusable tag for projects and technologies.
/// </summary>
public class ProjectTag
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;

    // Navigation property
    public virtual ICollection<ProjectTagLink> ProjectLinks { get; set; } = new List<ProjectTagLink>();
}
