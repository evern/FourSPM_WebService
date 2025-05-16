using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace FourSPM_WebService.Controllers
{
    /// <summary>
    /// Base controller class for controllers that require authentication.
    /// Provides utility methods for extracting user claims from Azure AD tokens.
    /// </summary>
    [ApiController]
    [Authorize]
    public abstract class AuthenticatedControllerBase : ControllerBase
    {
        /// <summary>
        /// Gets the Azure AD Object ID (principal ID) of the current user
        /// </summary>
        /// <returns>The user's object ID from Azure AD</returns>
        protected string GetUserObjectId()
        {
            return User.FindFirst("http://schemas.microsoft.com/identity/claims/objectidentifier")?.Value ?? string.Empty;
        }

        /// <summary>
        /// Gets the email address of the current user
        /// </summary>
        /// <returns>The user's email address</returns>
        protected string GetUserEmail()
        {
            return User.FindFirst("preferred_username")?.Value ?? string.Empty;
        }

        /// <summary>
        /// Gets the display name of the current user
        /// </summary>
        /// <returns>The user's display name</returns>
        protected string GetUserDisplayName()
        {
            return User.FindFirst("name")?.Value ?? string.Empty;
        }

        /// <summary>
        /// Gets all roles assigned to the current user
        /// </summary>
        /// <returns>A collection of the user's roles</returns>
        protected IEnumerable<string> GetUserRoles()
        {
            return User.FindAll(ClaimTypes.Role).Select(c => c.Value);
        }

        /// <summary>
        /// Checks if the current user has a specific role
        /// </summary>
        /// <param name="roleName">The role name to check</param>
        /// <returns>True if the user has the role, false otherwise</returns>
        protected bool UserHasRole(string roleName)
        {
            return User.IsInRole(roleName);
        }
    }
}
