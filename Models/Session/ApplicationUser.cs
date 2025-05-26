using Microsoft.OData.ModelBuilder.Core.V1;

namespace FourSPM_WebService.Models.Session
{
    public class ApplicationUser
    {
        // Basic user identity properties
        public string? UserName { get; set; }
        public Guid? UserId { get; set; }
        public string? Email { get; set; }
        
        // MSAL-specific properties
        public string? Upn { get; set; } // User Principal Name from Azure AD
        public string? ObjectId { get; set; } // Azure AD Object ID (oid claim)
        public string? TenantId { get; set; } // Azure AD Tenant ID (tid claim)
        public string? PreferredUsername { get; set; } // preferred_username claim
        public string? Name { get; set; } // name claim
        public string? AuthenticationType { get; set; } // MSAL or Legacy
        public string? Role { get; set; } // http://schemas.microsoft.com/ws/2008/06/identity/claims/role claim
        public List<string> Groups { get; set; } = new List<string>(); // groups claim
        public List<string> Scopes { get; set; } = new List<string>(); // scp claim
        
        // Application-specific permissions
        public IReadOnlyCollection<RolePermissionModel> Permissions { get; set; } = new List<RolePermissionModel>();
    }
}
