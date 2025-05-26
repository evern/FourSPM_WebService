using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using FourSPM_WebService.Models.Session;

namespace FourSPM_WebService.Utilities;

public static class MsalClaimsUtility
{
    /// <summary>
    /// Populates an ApplicationUser object with claims from an MSAL token
    /// </summary>
    /// <param name="user">The ApplicationUser to populate</param>
    /// <param name="principal">The ClaimsPrincipal containing the claims</param>
    /// <returns>The populated ApplicationUser</returns>
    public static ApplicationUser PopulateFromClaims(ApplicationUser user, ClaimsPrincipal principal)
    {
        if (principal == null)
        {
            return user;
        }
        
        // Basic identity claims
        user.ObjectId = principal.FindFirst("oid")?.Value ?? principal.FindFirst("sub")?.Value;
        user.Upn = principal.FindFirst("upn")?.Value;
        user.TenantId = principal.FindFirst("tid")?.Value;
        user.PreferredUsername = principal.FindFirst("preferred_username")?.Value;
        user.Name = principal.FindFirst("name")?.Value ?? principal.Identity?.Name;
        user.Email = principal.FindFirst("email")?.Value ?? user.PreferredUsername;
        
        // Extract roles and groups
        // Extract role claim using the standard ClaimTypes.Role claim type
        var roleClaim = principal.FindFirst(System.Security.Claims.ClaimTypes.Role);
        var groups = principal.Claims.Where(c => c.Type == "groups").Select(c => c.Value).ToList();
        
        // Extract scopes
        var scopeClaim = principal.FindFirst("scp") ?? principal.FindFirst("http://schemas.microsoft.com/identity/claims/scope");
        var scopes = scopeClaim != null ? scopeClaim.Value.Split(' ').ToList() : new List<string>();
        
        user.Role = roleClaim?.Value;
        user.Groups = groups;
        user.Scopes = scopes;
        user.AuthenticationType = "MSAL";
        
        return user;
    }
    
    /// <summary>
    /// Populates an ApplicationUser object directly from a JWT token string
    /// </summary>
    /// <param name="user">The ApplicationUser to populate</param>
    /// <param name="token">The raw JWT token string</param>
    /// <returns>The populated ApplicationUser if successful, otherwise unchanged user</returns>
    public static ApplicationUser PopulateFromToken(ApplicationUser user, string token)
    {
        if (string.IsNullOrEmpty(token))
        {
            return user;
        }
        
        try
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            if (!tokenHandler.CanReadToken(token))
            {
                return user;
            }
            
            var jwtToken = tokenHandler.ReadJwtToken(token);
            
            // Extract claims
            user.ObjectId = GetClaimValue(jwtToken, "oid") ?? GetClaimValue(jwtToken, "sub");
            user.Upn = GetClaimValue(jwtToken, "upn");
            user.TenantId = GetClaimValue(jwtToken, "tid");
            user.PreferredUsername = GetClaimValue(jwtToken, "preferred_username");
            user.Name = GetClaimValue(jwtToken, "name") ?? user.PreferredUsername;
            user.Email = GetClaimValue(jwtToken, "email") ?? user.PreferredUsername;
            
            // Extract role and groups
            var roleClaim = jwtToken.Claims.FirstOrDefault(c => c.Type == System.Security.Claims.ClaimTypes.Role);
            var groups = jwtToken.Claims.Where(c => c.Type == "groups").Select(c => c.Value).ToList();
            
            // Extract scopes
            var scopeClaim = jwtToken.Claims.FirstOrDefault(c => c.Type == "scp") 
                          ?? jwtToken.Claims.FirstOrDefault(c => c.Type == "http://schemas.microsoft.com/identity/claims/scope");
                          
            var scopes = scopeClaim != null ? scopeClaim.Value.Split(' ').ToList() : new List<string>();
            
            user.Role = roleClaim?.Value;
            user.Groups = groups;
            user.Scopes = scopes;
            user.AuthenticationType = "MSAL";
        }
        catch
        {
            // If any error occurs during token parsing, return the user unchanged
            // This is a best-effort utility, should not throw exceptions
        }
        
        return user;
    }
    
    /// <summary>
    /// Helper method to safely extract a claim value from a JWT token
    /// </summary>
    private static string? GetClaimValue(JwtSecurityToken token, string claimType)
    {
        return token.Claims.FirstOrDefault(c => c.Type == claimType)?.Value;
    }
    
    /// <summary>
    /// Determines if a token is an MSAL token based on its issuer
    /// </summary>
    public static bool IsMsalToken(JwtSecurityToken token, string tenantId)
    {
        var msalIssuers = new[]
        {
            $"https://login.microsoftonline.com/{tenantId}/v2.0",
            $"https://sts.windows.net/{tenantId}/"
        };
        
        return msalIssuers.Any(issuer => token.Issuer?.StartsWith(issuer) == true);
    }
}
