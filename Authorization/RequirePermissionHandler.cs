using FourSPM_WebService.Data.Interfaces;
using Microsoft.AspNetCore.Authorization;
using FourSPM_WebService.Models.Session;

namespace FourSPM_WebService.Authorization
{
    /// <summary>
    /// Authorization handler that validates if a user has the required permission
    /// based on their roles and the ROLE_PERMISSION mappings in the database
    /// </summary>
    public class RequirePermissionHandler : AuthorizationHandler<RequirePermissionRequirement>
    {
        private readonly IRolePermissionRepository _rolePermissionRepository;
        private readonly ApplicationUser _user;
        private readonly ILogger<RequirePermissionHandler> _logger;

        public RequirePermissionHandler(
            IRolePermissionRepository rolePermissionRepository,
            ApplicationUser user,
            ILogger<RequirePermissionHandler> logger)
        {
            _rolePermissionRepository = rolePermissionRepository;
            _user = user;
            _logger = logger;
        }

        /// <summary>
        /// Handles the requirement by checking if any of the user's roles has the required permission
        /// </summary>
        protected override async Task HandleRequirementAsync(
            AuthorizationHandlerContext context, 
            RequirePermissionRequirement requirement)
        {
            // Skip check if user isn't authenticated
            if (!context.User.Identity?.IsAuthenticated ?? true)
            {
                _logger.LogDebug("User not authenticated. Permission check skipped.");
                return;
            }

            try
            {
                // Get user's roles from the claims
                var userRoles = context.User.Claims
                    .Where(c => c.Type == "http://schemas.microsoft.com/ws/2008/06/identity/claims/role" ||
                                c.Type == "roles")
                    .Select(c => c.Value)
                    .ToList();

                if (userRoles.Count == 0)
                {
                    _logger.LogWarning("User has no roles assigned");
                    return;
                }

                // Check for the Admin role as a special case - admins have all permissions
                if (userRoles.Contains(Config.AuthConstants.Roles.Admin))
                {
                    _logger.LogDebug("User is an Admin - automatically granting permission {Permission}", requirement.PermissionName);
                    context.Succeed(requirement);
                    return;
                }

                // First, check if the permission is in the ApplicationUser's permission collection
                // This is much more efficient than querying the database for each check
                var directPermissionCheck = _user.Permissions.Any(p => 
                    p.Name.Equals(requirement.PermissionName, StringComparison.OrdinalIgnoreCase) &&
                    (p.Permission == Permission.All || p.Permission == Permission.ReadOnly));
                    
                if (directPermissionCheck)
                {
                    _logger.LogDebug("Permission {Permission} granted via ApplicationUser direct check", requirement.PermissionName);
                    context.Succeed(requirement);
                    return;
                }
                
                // Fall back to checking role permissions in the database if not found
                foreach (var role in userRoles)
                {
                    var hasPermission = await _rolePermissionRepository.CheckPermissionAsync(role, requirement.PermissionName);
                    if (hasPermission)
                    {
                        _logger.LogDebug("Permission {Permission} granted via role {Role}", requirement.PermissionName, role);
                        context.Succeed(requirement);
                        return;
                    }
                }

                _logger.LogWarning("Permission {Permission} denied - no matching role-permission mapping found", 
                    requirement.PermissionName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking permission {Permission}", requirement.PermissionName);
            }
        }
    }
}
