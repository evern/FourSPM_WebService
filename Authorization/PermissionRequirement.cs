using Microsoft.AspNetCore.Authorization;

namespace FourSPM_WebService.Authorization
{
    /// <summary>
    /// Defines a requirement for a specific permission
    /// </summary>
    public class PermissionRequirement : IAuthorizationRequirement
    {
        /// <summary>
        /// Gets the permission name that is required
        /// </summary>
        public string Permission { get; }

        /// <summary>
        /// Initializes a new instance of <see cref="PermissionRequirement"/> with the specified permission.
        /// </summary>
        /// <param name="permission">The permission required for access</param>
        public PermissionRequirement(string permission)
        {
            Permission = permission;
        }
    }
}
