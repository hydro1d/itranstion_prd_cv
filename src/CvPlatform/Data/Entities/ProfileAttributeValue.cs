namespace CvPlatform.Data.Entities;

/// <summary>
/// Candidate's value for a reusable library attribute (Section 2: Info).
/// </summary>
public class ProfileAttributeValue
{
    public int Id { get; set; }
    public int CandidateProfileId { get; set; }
    public int AttributeId { get; set; }
    
    public string Value { get; set; } = string.Empty;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public virtual CandidateProfile CandidateProfile { get; set; } = null!;
    public virtual CvAttribute Attribute { get; set; } = null!;
}
