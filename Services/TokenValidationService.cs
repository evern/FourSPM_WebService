using Microsoft.AspNetCore.Http;
using System.Security.Claims;

namespace FourSPM_WebService.Services
{
    /// <summary>
    /// Service for working with Azure AD tokens and claims
    /// </summary>
    public class TokenValidationService : ITokenValidationService
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ILogger<TokenValidationService> _logger;

        public TokenValidationService(IHttpContextAccessor httpContextAccessor, ILogger<TokenValidationService> logger)
        {
            _httpContextAccessor = httpContextAccessor;
            _logger = logger;
        }

        /// <summary>
        /// Gets the current ClaimsPrincipal from the HttpContext
        /// </summary>
        private ClaimsPrincipal? CurrentUser => _httpContextAccessor.HttpContext?.User;

        /// <summary>
        /// Gets the unique identifier (object ID) of the authenticated user from the token
        /// </summary>
        /// <returns>The user's object ID in Azure AD</returns>
        public string? GetUserObjectId()
        {
            // Try to get the Azure AD Object ID claim
            var objectId = GetClaimValue("http://schemas.microsoft.com/identity/claims/objectidentifier");
            
            if (string.IsNullOrEmpty(objectId))
            {
                // Fall back to name identifier if object identifier is not present
                objectId = GetClaimValue(ClaimTypes.NameIdentifier);
            }

            return objectId;
        }

        /// <summary>
        /// Gets the username/email of the authenticated user from the token
        /// </summary>
        /// <returns>The user's username or email</returns>
        public string? GetUsername()
        {
            // Try preferred_username first (typically email in Azure AD)
            var username = GetClaimValue("preferred_username");
            
            if (string.IsNullOrEmpty(username))
            {
                // Fall back to standard claim types
                username = GetClaimValue(ClaimTypes.Upn) ?? 
                          GetClaimValue(ClaimTypes.Email) ?? 
                          GetClaimValue(ClaimTypes.Name);
            }

            return username;
        }

        /// <summary>
        /// Gets the user's display name from the token if available
        /// </summary>
        /// <returns>The user's display name</returns>
        public string? GetUserDisplayName()
        {
            // Try name claim first (typically full name in Azure AD)
            var displayName = GetClaimValue("name");
            
            if (string.IsNullOrEmpty(displayName))
            {
                // Fall back to given_name + family_name
                var firstName = GetClaimValue("given_name");
                var lastName = GetClaimValue("family_name");
                
                if (!string.IsNullOrEmpty(firstName) && !string.IsNullOrEmpty(lastName))
                {
                    displayName = $"{firstName} {lastName}".Trim();
                }
                else if (!string.IsNullOrEmpty(firstName))
                {
                    displayName = firstName;
                }
                else if (!string.IsNullOrEmpty(lastName))
                {
                    displayName = lastName;
                }
                else
                {
                    // Last resort: use the username
                    displayName = GetUsername();
                }
            }

            return displayName;
        }

        /// <summary>
        /// Gets all roles assigned to the user from the token
        /// </summary>
        /// <returns>List of role names</returns>
        public IEnumerable<string> GetUserRoles()
        {
            if (CurrentUser == null)
            {
                return Enumerable.Empty<string>();
            }

            // Collect roles from both standard role claim and Azure AD roles claim
            var roles = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            
            // Get standard role claims
            var roleClaims = CurrentUser.FindAll(ClaimTypes.Role).ToList();
            foreach (var claim in roleClaims)
            {
                roles.Add(claim.Value);
            }
            
            // Get Azure AD role claims (may be in a different claim type)
            var azureRoleClaims = CurrentUser.FindAll("roles").ToList();
            foreach (var claim in azureRoleClaims)
            {
                roles.Add(claim.Value);
            }

            _logger.LogDebug("Found {RoleCount} roles for user: {Roles}", 
                            roles.Count, 
                            string.Join(", ", roles));
            
            return roles;
        }

        /// <summary>
        /// Checks if the user has a specific role
        /// </summary>
        /// <param name="roleName">The role to check for</param>
        /// <returns>True if the user has the specified role</returns>
        public bool IsInRole(string roleName)
        {
            var roles = GetUserRoles();
            return roles.Any(r => string.Equals(r, roleName, StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// Gets all claims from the token
        /// </summary>
        /// <returns>The user's claims</returns>
        public IEnumerable<Claim> GetAllClaims()
        {
            if (CurrentUser == null)
            {
                return Enumerable.Empty<Claim>();
            }

            return CurrentUser.Claims;
        }

        /// <summary>
        /// Gets a specific claim value by type
        /// </summary>
        /// <param name="claimType">The type of claim to retrieve</param>
        /// <returns>The claim value if found, null otherwise</returns>
        public string? GetClaimValue(string claimType)
        {
            if (CurrentUser == null)
            {
                return null;
            }

            return CurrentUser.FindFirst(claimType)?.Value;
        }
    }
}
