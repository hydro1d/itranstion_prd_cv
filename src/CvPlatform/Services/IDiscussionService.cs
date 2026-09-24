using CvPlatform.Data.Entities;
using CvPlatform.Hubs;

namespace CvPlatform.Services;

/// <summary>
/// Service contract for Real-Time Position Discussions.
/// </summary>
public interface IDiscussionService
{
    Task<Discussion> GetOrCreateDiscussionForPositionAsync(int positionId);
    Task<List<DiscussionMessageDto>> GetMessagesAsync(int positionId);
    Task<DiscussionMessageDto> PostMessageAsync(int positionId, string userId, string authorName, string authorRole, string message);
    Task<int> GetMessageCountAsync(int positionId);
    Task<Dictionary<int, int>> GetMessageCountsForPositionsAsync();
}
