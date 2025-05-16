using Microsoft.AspNetCore.Authorization;

namespace FourSPM_WebService.Authorization
{
    /// <summary>
    /// Authorization attribute that requires a specific permission
    /// </summary>
    public class RequirePermissionAttribute : AuthorizeAttribute
    {
        /// <summary>
        /// Creates a new authorization attribute requiring a specific permission
        /// </summary>
        /// <param name="permissionName">The permission name required to access the resource</param>
        public RequirePermissionAttribute(string permissionName)
        {
            Policy = $"Permission:{permissionName}";
        }
    }
}
