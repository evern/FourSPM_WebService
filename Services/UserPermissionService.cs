using FourSPM_WebService.Config;
using FourSPM_WebService.Models.Session;

namespace FourSPM_WebService.Services;

/// <summary>
/// Service for managing user permission checks
/// </summary>
public class UserPermissionService : IUserPermissionService
{
    private readonly ApplicationUser _currentUser;
    private readonly ILogger<UserPermissionService> _logger;

    public UserPermissionService(ApplicationUser currentUser, ILogger<UserPermissionService> logger)
    {
        _currentUser = currentUser;
        _logger = logger;
    }

    /// <inheritdoc />
    public bool HasPermission(string permissionName)
    {
        if (_currentUser.UserId == null)
        {
            _logger.LogWarning("Permission check failed: No authenticated user");
            return false;
        }

        return _currentUser.Permissions.Any(p => 
            p.Name.Equals(permissionName, StringComparison.OrdinalIgnoreCase) &&
            (p.Permission == Permission.All || p.Permission == Permission.ReadOnly));
    }

    /// <inheritdoc />
    public bool CanRead(string resourceName)
    {
        if (_currentUser.UserId == null)
        {
            _logger.LogWarning("Read permission check failed: No authenticated user");
            return false;
        }

        // Check for resource-specific read permission (using the AuthConstants pattern)
        var readPermissionName = $"{AuthConstants.Permissions.ReadPrefix}{resourceName}";
        
        return _currentUser.Permissions.Any(p => 
            (p.Name.Equals(readPermissionName, StringComparison.OrdinalIgnoreCase) ||
             p.Name.Equals(AuthConstants.Permissions.AllAccess, StringComparison.OrdinalIgnoreCase)) &&
            (p.Permission == Permission.All || p.Permission == Permission.ReadOnly));
    }

    /// <inheritdoc />
    public bool CanWrite(string resourceName)
    {
        if (_currentUser.UserId == null)
        {
            _logger.LogWarning("Write permission check failed: No authenticated user");
            return false;
        }

        // Check for resource-specific write permission (using the AuthConstants pattern)
        var writePermissionName = $"{AuthConstants.Permissions.WritePrefix}{resourceName}";
        
        return _currentUser.Permissions.Any(p => 
            (p.Name.Equals(writePermissionName, StringComparison.OrdinalIgnoreCase) ||
             p.Name.Equals(AuthConstants.Permissions.AllAccess, StringComparison.OrdinalIgnoreCase)) &&
            p.Permission == Permission.All);
    }

    /// <inheritdoc />
    public IReadOnlyCollection<RolePermissionModel> GetAllPermissions()
    {
        return _currentUser.Permissions;
    }
}
