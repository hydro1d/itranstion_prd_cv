using Microsoft.AspNetCore.SignalR;

namespace CvPlatform.Hubs;

public class DiscussionMessageDto
{
    public int Id { get; set; }
    public int DiscussionId { get; set; }
    public int PositionId { get; set; }
    public string UserId { get; set; } = string.Empty;
    public string AuthorName { get; set; } = string.Empty;
    public string AuthorRole { get; set; } = "Candidate";
    public string Message { get; set; } = string.Empty;
    public DateTime SentAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// SignalR Hub for real-time discussion messages scoped by PositionId.
/// </summary>
public class DiscussionHub : Hub
{
    public async Task JoinPositionGroup(int positionId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, GetGroupName(positionId));
    }

    public async Task LeavePositionGroup(int positionId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, GetGroupName(positionId));
    }

    public static string GetGroupName(int positionId) => $"position_discussion_{positionId}";
}
