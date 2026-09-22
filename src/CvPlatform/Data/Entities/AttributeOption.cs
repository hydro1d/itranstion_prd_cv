namespace CvPlatform.Data.Entities;

/// <summary>
/// Predefined selection options for Dropdown or Multi-select attribute types.
/// </summary>
public class AttributeOption
{
    public int Id { get; set; }
    public int AttributeId { get; set; }
    public string Value { get; set; } = string.Empty;
    public int DisplayOrder { get; set; }

    // Navigation property
    public virtual CvAttribute Attribute { get; set; } = null!;
}
