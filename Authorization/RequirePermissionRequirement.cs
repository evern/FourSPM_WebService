using Microsoft.AspNetCore.Authorization;

namespace FourSPM_WebService.Authorization
{
    /// <summary>
    /// Authorization requirement that checks if the user has a specific permission
    /// </summary>
    public class RequirePermissionRequirement : IAuthorizationRequirement
    {
        /// <summary>
        /// The permission name required
        /// </summary>
        public string PermissionName { get; }
        
        /// <summary>
        /// Creates a new requirement for the specified permission
        /// </summary>
        /// <param name="permissionName">Permission name required</param>
        public RequirePermissionRequirement(string permissionName)
        {
            PermissionName = permissionName;
        }
    }
}
