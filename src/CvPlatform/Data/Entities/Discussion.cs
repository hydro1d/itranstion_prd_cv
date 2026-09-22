namespace CvPlatform.Data.Entities;

/// <summary>
/// Discussion thread associated with a Position for candidate and recruiter Q&A.
/// </summary>
public class Discussion
{
    public int Id { get; set; }
    public int PositionId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public virtual Position Position { get; set; } = null!;
    public virtual ICollection<DiscussionMessage> Messages { get; set; } = new List<DiscussionMessage>();
}
