namespace CvPlatform.Data.Entities;

/// <summary>
/// Join entity connecting a Position template with reusable Attributes.
/// Defines whether the attribute is mandatory (IsRequired) and its display priority.
/// </summary>
public class PositionAttribute
{
    public int PositionId { get; set; }
    public int AttributeId { get; set; }
    
    public bool IsRequired { get; set; } = false;
    public int DisplayOrder { get; set; }

    // Navigation properties
    public virtual Position Position { get; set; } = null!;
    public virtual CvAttribute Attribute { get; set; } = null!;
}
