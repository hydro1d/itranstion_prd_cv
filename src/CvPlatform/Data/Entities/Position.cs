using CvPlatform.Data.Enums;

namespace CvPlatform.Data.Entities;

/// <summary>
/// Job opening / position posted by a recruiter with customizable required/optional attributes (Killer Feature #2).
/// </summary>
public class Position
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Company { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    public EmploymentType EmploymentType { get; set; } = EmploymentType.FullTime;
    
    public DateTime? Deadline { get; set; }
    public string Tags { get; set; } = string.Empty; // Comma-delimited search tags
    public PositionVisibility Visibility { get; set; } = PositionVisibility.Public;
    
    public string RecruiterId { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    // Navigation properties
    public virtual ApplicationUser Recruiter { get; set; } = null!;
    public virtual ICollection<PositionAttribute> PositionAttributes { get; set; } = new List<PositionAttribute>();
    public virtual ICollection<CV> CVs { get; set; } = new List<CV>();
    public virtual ICollection<Discussion> Discussions { get; set; } = new List<Discussion>();
}
