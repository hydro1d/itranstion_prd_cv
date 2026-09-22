using System.ComponentModel.DataAnnotations;

namespace CvPlatform.Data.Entities;

/// <summary>
/// Position-specific tailored CV (Killer Feature #3).
/// Enforces strictly: 1 CV per candidate per position (CandidateProfileId + PositionId = UNIQUE).
/// Includes optimistic concurrency token for auto-saving.
/// </summary>
public class CV
{
    public int Id { get; set; }
    public int CandidateProfileId { get; set; }
    public int PositionId { get; set; }
    
    public string Title { get; set; } = string.Empty;
    public string? ProfessionalSummary { get; set; }
    public int CompletionPercentage { get; set; } = 0;
    
    /// <summary>
    /// Optimistic concurrency token updated on every auto-save to prevent data overwrites.
    /// </summary>
    [ConcurrencyCheck]
    public string RowVersion { get; set; } = Guid.NewGuid().ToString();

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    // Navigation properties
    public virtual CandidateProfile CandidateProfile { get; set; } = null!;
    public virtual Position Position { get; set; } = null!;
    public virtual ICollection<CVAttributeValue> AttributeValues { get; set; } = new List<CVAttributeValue>();
    public virtual ICollection<CVLike> Likes { get; set; } = new List<CVLike>();
}
