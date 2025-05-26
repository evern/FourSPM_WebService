using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using System.Collections.Generic;
using FourSPM_WebService.Models.Session;
using FourSPM_WebService.Data.Interfaces;

namespace FourSPM_WebService.Models.Session
{
    /// <summary>
    /// Provides functionality to map MSAL claims to ApplicationUser objects
    /// </summary>
    public static class MsalClaimsMapper
    {
        /// <summary>
        /// Creates an ApplicationUser from a ClaimsPrincipal containing MSAL claims
        /// </summary>
        /// <param name="claimsPrincipal">The claims principal from authentication</param>
        /// <returns>An ApplicationUser populated with data from claims</returns>
        public static ApplicationUser CreateFromClaims(ClaimsPrincipal claimsPrincipal)
        {
            if (claimsPrincipal == null || claimsPrincipal.Identity == null || !claimsPrincipal.Identity.IsAuthenticated)
                return new ApplicationUser(); // Return empty user if not authenticated

            var applicationUser = new ApplicationUser
            {
                AuthenticationType = "MSAL",
                // Map standard claims
                ObjectId = claimsPrincipal.FindFirst("oid")?.Value,
                TenantId = claimsPrincipal.FindFirst("tid")?.Value,
                PreferredUsername = claimsPrincipal.FindFirst("preferred_username")?.Value,
                Name = claimsPrincipal.FindFirst("name")?.Value,
                Email = claimsPrincipal.FindFirst("email")?.Value,
                Upn = claimsPrincipal.FindFirst("upn")?.Value,
                
                // Use PreferredUsername for UserName if available, otherwise fall back to Name
                UserName = claimsPrincipal.FindFirst("preferred_username")?.Value ?? 
                           claimsPrincipal.FindFirst("name")?.Value
            };

            // Convert ObjectId to UserId if possible
            if (Guid.TryParse(applicationUser.ObjectId, out Guid userId))
            {
                applicationUser.UserId = userId;
            }

            // Extract role claims (could be multiple)
            var roleClaims = claimsPrincipal.FindAll("roles").Select(c => c.Value);
            if (roleClaims.Any())
            {
                applicationUser.Roles = roleClaims.ToList();
            }

            // Extract group claims (could be multiple)
            var groupClaims = claimsPrincipal.FindAll("groups").Select(c => c.Value);
            if (groupClaims.Any())
            {
                applicationUser.Groups = groupClaims.ToList();
            }

            // Extract scope claims (typically a space-delimited string)
            var scopeClaim = claimsPrincipal.FindFirst("scp")?.Value;
            if (!string.IsNullOrEmpty(scopeClaim))
            {
                applicationUser.Scopes = scopeClaim.Split(' ').ToList();
            }

            return applicationUser;
        }

        /// <summary>
        /// Loads permissions for a user based on their roles
        /// </summary>
        /// <param name="user">The user to load permissions for</param>
        /// <returns>Updated user with permissions</returns>
        public static ApplicationUser LoadUserPermissions(ApplicationUser user)
        {
            if (user == null)
                return new ApplicationUser(); // Return empty user rather than null

            // Create a list to hold the user's permissions
            var permissions = new List<RolePermissionModel>();
            
            // Get roles from the roles collection
            foreach (string role in user.Roles)
            {
                // In a real implementation, this would query your database
                // For now, we're setting basic permissions based on role names
                permissions.Add(new RolePermissionModel { 
                    Name = role, 
                    Permission = role.ToLower().Contains("admin") ? Permission.All : Permission.ReadOnly 
                });
            }
            
            // Set permissions on user
            user.Permissions = permissions;
            
            return user;
        }
    }
}
