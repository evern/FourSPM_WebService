using FourSPM_WebService.Data.EF.FourSPM;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace FourSPM_WebService.Authorization
{
    /// <summary>
    /// Authorization handler that checks if a user has the required permission
    /// </summary>
    public class PermissionAuthorizationHandler : AuthorizationHandler<PermissionRequirement>
    {
        private readonly FourSPMContext _dbContext;
        private readonly IConfiguration _configuration;
        private readonly ILogger<PermissionAuthorizationHandler> _logger;

        /// <summary>
        /// Initializes a new instance of <see cref="PermissionAuthorizationHandler"/>
        /// </summary>
        public PermissionAuthorizationHandler(
            FourSPMContext dbContext,
            IConfiguration configuration,
            ILogger<PermissionAuthorizationHandler> logger)
        {
            _dbContext = dbContext;
            _configuration = configuration;
            _logger = logger;
        }

        /// <summary>
        /// Handles the authorization requirement
        /// </summary>
        protected override async Task HandleRequirementAsync(
            AuthorizationHandlerContext context,
            PermissionRequirement requirement)
        {
            // If the user is not authenticated, deny access
            if (!context.User.Identity?.IsAuthenticated ?? true)
            {
                _logger.LogDebug("User is not authenticated. Access denied.");
                return;
            }

            // Get user email/name from claims
            var userEmail = context.User.FindFirst("preferred_username")?.Value;
            if (string.IsNullOrEmpty(userEmail))
            {
                _logger.LogDebug("User email not found in claims. Access denied.");
                return;
            }

            try
            {
                // Check for development mode system role override
                bool isDevelopment = _configuration.GetValue<bool>("AzureAd:DevelopmentMode", false);
                string devDomain = _configuration.GetValue<string>("AzureAd:DevelopmentDomain", "yourdomain.com");
                
                if (isDevelopment && userEmail != null && userEmail.EndsWith($"@{devDomain}"))
                {
                    _logger.LogWarning($"Development mode is enabled. User {userEmail} granted system role access.");
                    context.Succeed(requirement);
                    return;
                }

                // Get user roles from claims
                var userRoles = context.User.FindAll(ClaimTypes.Role).Select(c => c.Value.ToLowerInvariant()).ToList();
                
                // Check if user has any system role
                bool hasSystemRole = await _dbContext.ROLEs
                    .Where(r => r.IS_SYSTEM_ROLE && 
                           r.DELETED == null && 
                           userRoles.Contains(r.NAME.ToLowerInvariant()))
                    .AnyAsync();

                if (hasSystemRole)
                {
                    _logger.LogInformation($"User {userEmail} has system role. Access granted for {requirement.Permission}.");
                    context.Succeed(requirement);
                    return;
                }

                // Check for the specific permission in user's roles
                bool hasPermission = await _dbContext.ROLE_PERMISSIONs
                    .Join(_dbContext.ROLEs,
                          rp => rp.GUID_ROLE,
                          r => r.GUID,
                          (rp, r) => new { RolePermission = rp, Role = r })
                    .Where(x => x.RolePermission.DELETED == null &&
                               x.Role.DELETED == null &&
                               userRoles.Contains(x.Role.NAME.ToLowerInvariant()) && 
                               x.RolePermission.PERMISSION == requirement.Permission)
                    .AnyAsync();

                if (hasPermission)
                {
                    _logger.LogInformation($"User {userEmail} has permission {requirement.Permission}. Access granted.");
                    context.Succeed(requirement);
                    return;
                }

                // Check for implied permissions (if user has Edit or Delete permission, they should also have View permission)
                if (requirement.Permission.EndsWith(".View"))
                {
                    string category = requirement.Permission.Split('.')[0];
                    string editPermission = $"{category}.Edit";
                    string deletePermission = $"{category}.Delete";
                    
                    bool hasImpliedPermission = await _dbContext.ROLE_PERMISSIONs
                        .Join(_dbContext.ROLEs,
                              rp => rp.GUID_ROLE,
                              r => r.GUID,
                              (rp, r) => new { RolePermission = rp, Role = r })
                        .Where(x => x.RolePermission.DELETED == null &&
                                  x.Role.DELETED == null &&
                                  userRoles.Contains(x.Role.NAME.ToLowerInvariant()) && 
                                  (x.RolePermission.PERMISSION == editPermission || 
                                   x.RolePermission.PERMISSION == deletePermission))
                        .AnyAsync();

                    if (hasImpliedPermission)
                    {
                        _logger.LogInformation($"User {userEmail} has implied permission {requirement.Permission} through Edit/Delete permission. Access granted.");
                        context.Succeed(requirement);
                        return;
                    }
                }

                _logger.LogInformation($"User {userEmail} does not have permission {requirement.Permission}. Access denied.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error during permission check for {requirement.Permission}");
            }

            // If we reach this point, the requirement has not been met
            return;
        }
    }
}
