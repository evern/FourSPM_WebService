using System.Security.Claims;

namespace FourSPM_WebService.Services
{
    /// <summary>
    /// Service for extracting and validating claims from Azure AD tokens
    /// </summary>
    public interface ITokenValidationService
    {
        /// <summary>
        /// Gets the unique identifier (object ID) of the authenticated user from the token
        /// </summary>
        /// <returns>The user's object ID in Azure AD</returns>
        string? GetUserObjectId();

        /// <summary>
        /// Gets the username/email of the authenticated user from the token
        /// </summary>
        /// <returns>The user's username or email</returns>
        string? GetUsername();

        /// <summary>
        /// Gets the user's display name from the token if available
        /// </summary>
        /// <returns>The user's display name</returns>
        string? GetUserDisplayName();

        /// <summary>
        /// Gets all roles assigned to the user from the token
        /// </summary>
        /// <returns>List of role names</returns>
        IEnumerable<string> GetUserRoles();

        /// <summary>
        /// Checks if the user has a specific role
        /// </summary>
        /// <param name="roleName">The role to check for</param>
        /// <returns>True if the user has the specified role</returns>
        bool IsInRole(string roleName);

        /// <summary>
        /// Gets all claims from the token
        /// </summary>
        /// <returns>The user's claims</returns>
        IEnumerable<Claim> GetAllClaims();

        /// <summary>
        /// Gets a specific claim value by type
        /// </summary>
        /// <param name="claimType">The type of claim to retrieve</param>
        /// <returns>The claim value if found, null otherwise</returns>
        string? GetClaimValue(string claimType);
    }
}
