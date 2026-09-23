using CvPlatform.Data.Entities;
using CvPlatform.Data.Enums;

namespace CvPlatform.Services;

public class CvMissingAttribute
{
    public int AttributeId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? CategoryName { get; set; }
    public AttributeDataType DataType { get; set; }
}

public class CvMappingStatus
{
    public int CvId { get; set; }
    public int TotalRequired { get; set; }
    public int FilledRequired { get; set; }
    public int TotalOptional { get; set; }
    public int FilledOptional { get; set; }
    public int CompletionPercentage { get; set; }
    public bool IsReadyForSubmission => TotalRequired == 0 || FilledRequired >= TotalRequired;
    public List<CvMissingAttribute> MissingRequiredAttributes { get; set; } = new();
}

public class CvUpdateResult
{
    public bool Success { get; set; }
    public bool ConcurrencyConflict { get; set; }
    public string? ErrorMessage { get; set; }
    public CV? UpdatedCv { get; set; }
}

/// <summary>
/// Service contract for Automatic CV Generation Engine & Tailored Document Management (Killer Feature #3).
/// Enforces strictly: 1 CV per candidate per position.
/// </summary>
public interface ICvService
{
    Task<CV> GetOrCreateCvForPositionAsync(int candidateProfileId, int positionId);
    Task<CV?> GetCvByIdAsync(int cvId);
    Task<List<CV>> GetCvsForCandidateAsync(int candidateProfileId);
    Task<List<CV>> GetCvsForPositionAsync(int positionId);
    Task<CvUpdateResult> UpdateCvAsync(int cvId, string? title, string? summary, Dictionary<int, string> attributeValues, string expectedRowVersion);
    Task<CvMappingStatus> GetCvMappingStatusAsync(int cvId);
    Task<bool> ToggleLikeAsync(int cvId, string recruiterId);
}
