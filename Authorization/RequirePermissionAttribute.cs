using Microsoft.AspNetCore.Authorization;

namespace FourSPM_WebService.Authorization
{
    /// <summary>
    /// Authorization attribute that requires a specific permission for access
    /// </summary>
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = true)]
    public class RequirePermissionAttribute : AuthorizeAttribute
    {
        /// <summary>
        /// Initializes a new instance of <see cref="RequirePermissionAttribute"/> with the specified permission.
        /// </summary>
        /// <param name="permission">The permission required for access</param>
        public RequirePermissionAttribute(string permission) : base(policy: permission)
        {
        }
    }
}
