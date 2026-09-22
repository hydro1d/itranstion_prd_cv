namespace CvPlatform.Data.Entities;

/// <summary>
/// Categories for grouping dynamic reusable attributes (e.g. Experience, Education, Skills, Preferences).
/// </summary>
public class AttributeCategory
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int DisplayOrder { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public virtual ICollection<CvAttribute> Attributes { get; set; } = new List<CvAttribute>();
}
