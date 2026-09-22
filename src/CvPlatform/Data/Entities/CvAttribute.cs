using System.ComponentModel.DataAnnotations.Schema;
using CvPlatform.Data.Enums;

namespace CvPlatform.Data.Entities;

/// <summary>
/// Reusable Attribute definition configured by recruiters (Killer Feature #1).
/// Supports Text, Number, Date, Boolean, Dropdown, MultiSelect, and URL types.
/// </summary>
[Table("Attributes")]
public class CvAttribute
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int CategoryId { get; set; }
    public AttributeDataType DataType { get; set; } = AttributeDataType.Text;
    
    public bool IsRequiredDefault { get; set; } = false;
    public bool IsSearchable { get; set; } = true;
    public bool IsActive { get; set; } = true;
    public string? ValidationRules { get; set; }
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    // Navigation properties
    public virtual AttributeCategory Category { get; set; } = null!;
    public virtual ICollection<AttributeOption> Options { get; set; } = new List<AttributeOption>();
    public virtual ICollection<PositionAttribute> PositionAttributes { get; set; } = new List<PositionAttribute>();
    public virtual ICollection<ProfileAttributeValue> ProfileValues { get; set; } = new List<ProfileAttributeValue>();
    public virtual ICollection<CVAttributeValue> CVValues { get; set; } = new List<CVAttributeValue>();
}
