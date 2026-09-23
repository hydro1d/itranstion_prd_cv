using CvPlatform.Data.Entities;
using CvPlatform.Data.Enums;

namespace CvPlatform.Services;

/// <summary>
/// Service contract for managing the Reusable Dynamic Attribute Library (Killer Feature #1).
/// </summary>
public interface IAttributeService
{
    Task<List<AttributeCategory>> GetCategoriesAsync();
    Task<AttributeCategory?> GetCategoryByIdAsync(int id);
    Task<AttributeCategory> SaveCategoryAsync(AttributeCategory category);
    Task<bool> DeleteCategoryAsync(int id);

    Task<List<CvAttribute>> GetAttributesAsync(
        int? categoryId = null,
        AttributeDataType? dataType = null,
        string? search = null,
        bool activeOnly = false);

    Task<CvAttribute?> GetAttributeByIdAsync(int id);
    Task<CvAttribute> SaveAttributeAsync(CvAttribute attribute, List<string>? options = null);
    Task<bool> ToggleAttributeStatusAsync(int id);
    Task<bool> DeleteAttributeAsync(int id);
}
