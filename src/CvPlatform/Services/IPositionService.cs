using CvPlatform.Data.Entities;
using CvPlatform.Data.Enums;

namespace CvPlatform.Services;

public class PositionAttributeInput
{
    public int AttributeId { get; set; }
    public bool IsRequired { get; set; }
    public int DisplayOrder { get; set; }
}

public class PlatformMetrics
{
    public int ActivePositions { get; set; }
    public int RegisteredCandidates { get; set; }
    public int PartnerRecruiters { get; set; }
    public int CvsGenerated { get; set; }
}

/// <summary>
/// Service contract for managing Customizable Position Templates & Job Openings (Killer Feature #2).
/// </summary>
public interface IPositionService
{
    Task<List<Position>> GetPositionsAsync(
        string? search = null,
        EmploymentType? employmentType = null,
        PositionVisibility? visibility = null,
        string? recruiterId = null);

    Task<Position?> GetPositionByIdAsync(int id);
    Task<Position> SavePositionAsync(Position position, List<PositionAttributeInput> attributes, string currentUserId);
    Task<bool> UpdateVisibilityAsync(int id, PositionVisibility visibility);
    Task<bool> DeletePositionAsync(int id);
    Task<PlatformMetrics> GetMetricsAsync();
}
