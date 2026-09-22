namespace CvPlatform.Data.Entities;

/// <summary>
/// Like endorsement given by a recruiter to a candidate's tailored CV.
/// Unique constraint: RecruiterId + CVId (one like per recruiter per CV).
/// </summary>
public class CVLike
{
    public int Id { get; set; }
    public int CVId { get; set; }
    public string RecruiterId { get; set; } = string.Empty;
    public DateTime LikedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public virtual CV CV { get; set; } = null!;
    public virtual ApplicationUser Recruiter { get; set; } = null!;
}
