namespace CvPlatform.Data.Entities;

/// <summary>
/// Many-to-many join entity between Project and ProjectTag.
/// </summary>
public class ProjectTagLink
{
    public int ProjectId { get; set; }
    public int TagId { get; set; }

    // Navigation properties
    public virtual Project Project { get; set; } = null!;
    public virtual ProjectTag Tag { get; set; } = null!;
}
