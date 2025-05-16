using FourSPM_WebService.Models.Session;

namespace FourSPM_WebService.Services;

/// <summary>
/// Service interface for managing user permission checks
/// </summary>
public interface IUserPermissionService
{
    /// <summary>
    /// Check if the current user has the specified permission
    /// </summary>
    /// <param name="permissionName">The name of the permission to check</param>
    /// <returns>True if the user has the permission, false otherwise</returns>
    bool HasPermission(string permissionName);
    
    /// <summary>
    /// Check if the current user has read permission for the given resource
    /// </summary>
    /// <param name="resourceName">The name of the resource/entity</param>
    /// <returns>True if the user has read permission, false otherwise</returns>
    bool CanRead(string resourceName);
    
    /// <summary>
    /// Check if the current user has write permission for the given resource
    /// </summary>
    /// <param name="resourceName">The name of the resource/entity</param>
    /// <returns>True if the user has write permission, false otherwise</returns>
    bool CanWrite(string resourceName);

    /// <summary>
    /// Get all permissions for the current user
    /// </summary>
    /// <returns>Collection of permission models</returns>
    IReadOnlyCollection<RolePermissionModel> GetAllPermissions();
}
