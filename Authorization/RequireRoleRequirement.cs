using Microsoft.AspNetCore.Authorization;

namespace FourSPM_WebService.Authorization
{
    /// <summary>
    /// Authorization requirement that checks if the user is in a specific role
    /// </summary>
    public class RequireRoleRequirement : IAuthorizationRequirement
    {
        /// <summary>
        /// The primary role name required
        /// </summary>
        public string RoleName { get; }
        
        /// <summary>
        /// Alternative roles that also satisfy this requirement
        /// </summary>
        public string[] AlternativeRoles { get; }

        /// <summary>
        /// Creates a new requirement for the specified role
        /// </summary>
        /// <param name="roleName">Primary role name required</param>
        /// <param name="alternativeRoles">Other roles that also satisfy this requirement</param>
        public RequireRoleRequirement(string roleName, params string[] alternativeRoles)
        {
            RoleName = roleName;
            AlternativeRoles = alternativeRoles;
        }
    }
}
