namespace CvPlatform.Data.Entities;

/// <summary>
/// Position-specific attribute value in a tailored CV, mapped or customized by candidate.
/// </summary>
public class CVAttributeValue
{
    public int Id { get; set; }
    public int CVId { get; set; }
    public int AttributeId { get; set; }
    
    public string Value { get; set; } = string.Empty;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public virtual CV CV { get; set; } = null!;
    public virtual CvAttribute Attribute { get; set; } = null!;
}
