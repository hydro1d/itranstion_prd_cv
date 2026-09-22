namespace CvPlatform.Data.Entities;

/// <summary>
/// Individual message posted inside a Position discussion thread.
/// </summary>
public class DiscussionMessage
{
    public int Id { get; set; }
    public int DiscussionId { get; set; }
    public string UserId { get; set; } = string.Empty;
    public string AuthorName { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public DateTime SentAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public virtual Discussion Discussion { get; set; } = null!;
    public virtual ApplicationUser User { get; set; } = null!;
}
