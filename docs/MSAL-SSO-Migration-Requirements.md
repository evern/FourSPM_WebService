# MSAL SSO Authentication Migration Requirements

## Overview

This document outlines the requirements and implementation approach for migrating the FourSPM application's authentication system to Microsoft Authentication Library (MSAL) with Single Sign-On (SSO) capabilities. The migration will also implement a role-based permissions system that utilizes the newly created ROLE and ROLE_PERMISSION database tables.

## Prerequisites

- Azure Active Directory (Azure AD) application registration is complete
- Database schema has been updated to include ROLE and ROLE_PERMISSION tables
- Entity Framework scaffolding for new tables will be implemented

## Azure AD Configuration

The following Azure AD resources have been created and will be utilized:

- **Application (client) ID**: c67bf91d-8b6a-494a-8b99-c7a4592e08c1
- **Directory (tenant) ID**: 3c7fa9e9-64e7-443c-905a-d9134ca00da9

### Environment-Specific Azure AD Configuration

To facilitate seamless switching between development and production environments, implement a configuration system that:

1. **Uses Environment-Specific Configuration Files**:
   - Create separate `appsettings.Development.json` and `appsettings.Production.json` files
   - Store environment-specific Azure AD settings in these files

2. **Configuration Structure**:
   ```json
   {
     "AzureAd": {
       "Instance": "https://login.microsoftonline.com/",
       "Domain": "yourdomain.onmicrosoft.com",
       "TenantId": "3c7fa9e9-64e7-443c-905a-d9134ca00da9",
       "ClientId": "c67bf91d-8b6a-494a-8b99-c7a4592e08c1",
       "CallbackPath": "/signin-oidc",
       "SignedOutCallbackPath": "/signout-callback-oidc"
     }
   }
   ```

3. **Environment Variables Override**:
   - Allow environment variables to override configuration settings
   - Example: `AzureAd__TenantId` and `AzureAd__ClientId`
   - This enables easy switching in deployment pipelines without code changes

4. **Development Flag**:
   - Add a boolean flag in configuration to indicate development mode
   - `"AzureAd": { "IsDevelopment": true }`
   - Use this flag to enable local testing features

### Configured OAuth Scopes

The following scopes have been registered in Azure AD:
- `api://c67bf91d-8b6a-494a-8b99-c7a4592e08c1/Application.Admin`
- `api://c67bf91d-8b6a-494a-8b99-c7a4592e08c1/Application.User`

### Configuration Loading Code Example

```csharp
// In Startup.cs - ConfigureServices
public void ConfigureServices(IServiceCollection services)
{
    // Load Azure AD configuration from the appropriate environment settings
    services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
        .AddMicrosoftIdentityWebApi(Configuration.GetSection("AzureAd"));
        
    // Allow environment switching through environment variables
    // without code changes or redeployment
}
```

## Database Schema

The database schema has been updated to include the following tables:

1. **ROLE** - Defines application roles
   - Primary key: GUID
   - Unique constraint: ROLE_NAME
   - Contains system-level information about available roles

2. **ROLE_PERMISSION** - Maps permissions to roles
   - Primary key: GUID
   - Foreign key: ROLE_NAME references ROLE(ROLE_NAME)
   - Contains permission grants for each role

## Implementation Requirements

### 1. Entity Framework Model Implementation

#### ROLE Entity Class
- Create the EF model class for the ROLE table in `Data/EF/FourSPM/ROLE.cs`
- Implement navigation properties to ROLE_PERMISSION
- Configure entity in FourSPMContext.cs

#### ROLE_PERMISSION Entity Class
- Create the EF model class for the ROLE_PERMISSION table in `Data/EF/FourSPM/ROLE_PERMISSION.cs`
- Implement navigation property to ROLE
- Configure entity in FourSPMContext.cs

### 2. OData Entity Model Implementation

#### RoleEntity Class
- Create `RoleEntity.cs` in the `Data/OData/FourSPM` directory
- Map all properties from the ROLE database table
- Include a collection property for permissions

#### RolePermissionEntity Class
- Create `RolePermissionEntity.cs` in the `Data/OData/FourSPM` directory
- Map all properties from the ROLE_PERMISSION database table
- Include a navigation property for the related role

#### EDM Model Registration
- Update `EdmModelBuilder.cs` to include the new entities in the OData model

### 3. Repository Implementation

#### Role Repository
- Create `IRoleRepository` interface and `RoleRepository` implementation
- Include CRUD methods for roles
- Include methods to retrieve roles with their permissions

#### Role Permission Repository
- Create `IRolePermissionRepository` interface and `RolePermissionRepository` implementation
- Include CRUD methods for role permissions
- Include methods to retrieve all permissions for a specific role

### 4. Controller Implementation

#### Roles Controller
- Create `RolesController.cs` with CRUD endpoints for role management
- Include endpoints for retrieving roles with their permissions
- Secure endpoints with appropriate authorization policies
- Add endpoint to enable navigation to role permission management screen:
  ```csharp
  [HttpGet("api/v1/roles/{roleName}/nav")]
  public async Task<IActionResult> GetRoleNavigationInfo(string roleName)
  {
      var role = await _roleRepository.GetByNameAsync(roleName);
      if (role == null)
          return NotFound();
          
      return Ok(new { RoleName = role.ROLE_NAME, DisplayName = role.DISPLAY_NAME });
  }
  ```

#### Role Permissions Controller
- Create `RolePermissionsController.cs` with CRUD endpoints for permission management
- Include endpoints for bulk permission updates
- Secure endpoints with appropriate authorization policies

#### Static Permissions Endpoint
- Create an endpoint to retrieve all available static permissions:
  ```csharp
  [HttpGet("api/v1/permissions/available")]
  public IActionResult GetAvailablePermissions()
  {
      var permissions = StaticPermissions.AllPermissions
          .Select(p => new {
              Name = p,
              DisplayName = FormatPermissionDisplayName(p),
              Category = GetPermissionCategory(p),
              IsEditPermission = p.EndsWith(".Edit")
          })
          .OrderBy(p => p.Category)
          .ThenBy(p => p.DisplayName)
          .ToList();
          
      return Ok(permissions);
  }
  
  private string FormatPermissionDisplayName(string permission)
  {
      // Convert format like "Projects.Edit" to "Projects - Edit"
      var parts = permission.Split('.');
      if (parts.Length != 2) return permission;
      
      return $"{parts[0]} - {parts[1]}";
  }
  
  private string GetPermissionCategory(string permission)
  {
      // Extract category from permission (e.g., "Projects.Edit" => "Projects")
      return permission.Split('.')[0];
  }
  ```
  
- Create endpoint to update role permissions with automatic View permission handling:
  ```csharp
  [HttpPost("api/v1/roles/{roleName}/permissions")]
  public async Task<IActionResult> UpdateRolePermissions(string roleName, [FromBody] UpdateRolePermissionsRequest request)
  {
      // Validate request
      if (request?.Permissions == null)
          return BadRequest("No permissions specified");
          
      var role = await _roleRepository.GetByNameAsync(roleName);
      if (role == null)
          return NotFound($"Role '{roleName}' not found");
          
      // Process the permissions and automatically add View permissions
      // when Edit permissions are granted
      var processedPermissions = new HashSet<string>(request.Permissions);
      
      // For each Edit permission, ensure the corresponding View permission is included
      foreach (var permission in request.Permissions.Where(p => p.EndsWith(".Edit")))
      {
          var viewPermission = permission.Replace(".Edit", ".View");
          processedPermissions.Add(viewPermission);
      }
      
      // Update the permissions in the database
      await _rolePermissionRepository.UpdateRolePermissionsAsync(
          roleName, 
          processedPermissions.ToList(), 
          _applicationUser.UserId);
          
      return Ok(new { Success = true });
  }
  
  public class UpdateRolePermissionsRequest
  {
      public List<string> Permissions { get; set; } = new List<string>();
  }
  ```

### 5. MSAL Authentication Integration

#### Backend Configuration
1. **Update Startup.cs**:
   - Replace current authentication with MSAL authentication
   - Configure JWT bearer authentication to validate Azure AD tokens
   - Configure authorization policies based on roles and permissions
   - Register appropriate CORS settings for token validation

2. **Update AuthorizationExtensions**:
   - Implement permission-based authorization handlers
   - Create permission requirement classes
   - Register authorization policies in DI container

3. **Create PermissionManager Service**:
   - Implement a service to check if a user has specific permissions
   - Cache permission data for performance optimization
   - Provide helper methods for permission checks

#### Custom Authorization Attributes

1. **Create HasPermissionAttribute**:
   - Custom attribute to decorate controller methods with required permissions
   - Implementation should check the user's role and corresponding permissions
   - Support for multiple required permissions (AND/OR logic)

### 6. Permission Authorization System

#### Static Permission Definitions
- Generate static permission constants based on page access in `D:\Source\FourSPM_Web\fourspm_web\src\pages`
- Define two levels of access for each page:
  - Read-only access
  - Full editing capability
- Store permissions in a centralized location for easy reference

##### Generated Permissions
Based on the current frontend structure, the following permissions should be implemented:

```csharp
public static class StaticPermissions
{
    // Areas module permissions
    public const string AreasView = "Areas.View";
    public const string AreasEdit = "Areas.Edit";
    
    // Clients module permissions
    public const string ClientsView = "Clients.View";
    public const string ClientsEdit = "Clients.Edit";
    
    // Deliverable Gates module permissions
    public const string DeliverableGatesView = "DeliverableGates.View";
    public const string DeliverableGatesEdit = "DeliverableGates.Edit";
    
    // Deliverable Progress module permissions
    public const string DeliverableProgressView = "DeliverableProgress.View";
    public const string DeliverableProgressEdit = "DeliverableProgress.Edit";
    
    // Deliverables module permissions
    public const string DeliverablesView = "Deliverables.View";
    public const string DeliverablesEdit = "Deliverables.Edit";
    
    // Disciplines module permissions
    public const string DisciplinesView = "Disciplines.View";
    public const string DisciplinesEdit = "Disciplines.Edit";
    
    // Document Types module permissions
    public const string DocumentTypesView = "DocumentTypes.View";
    public const string DocumentTypesEdit = "DocumentTypes.Edit";
    
    // Home module permissions
    public const string HomeView = "Home.View";
    
    // Profile module permissions
    public const string ProfileView = "Profile.View";
    public const string ProfileEdit = "Profile.Edit";
    
    // Project module permissions
    public const string ProjectView = "Project.View";
    public const string ProjectEdit = "Project.Edit";
    
    // Projects module permissions
    public const string ProjectsView = "Projects.View";
    public const string ProjectsEdit = "Projects.Edit";
    
    // Variation Deliverables module permissions
    public const string VariationDeliverablesView = "VariationDeliverables.View";
    public const string VariationDeliverablesEdit = "VariationDeliverables.Edit";
    
    // Variations module permissions
    public const string VariationsView = "Variations.View";
    public const string VariationsEdit = "Variations.Edit";
    
    // Roles module permissions
    public const string RolesView = "Roles.View";
    public const string RolesEdit = "Roles.Edit";
    
    // System permissions
    public const string SystemAdmin = "System.Admin";
    
    // Helper method to get all permissions
    public static IEnumerable<string> AllPermissions
    {
        get
        {
            return typeof(StaticPermissions)
                .GetFields(BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy)
                .Where(fi => fi.IsLiteral && !fi.IsInitOnly && fi.FieldType == typeof(string))
                .Select(x => (string)x.GetRawConstantValue()!);
        }
    }
}
```

##### Permission Scanner Script

```csharp
public static class PermissionScanner
{
    public static List<string> ScanDirectoryForPermissions(string directoryPath)
    {
        List<string> permissions = new List<string>();
        
        if (!Directory.Exists(directoryPath))
            return permissions;
            
        var directories = Directory.GetDirectories(directoryPath);
        
        foreach (var dir in directories)
        {
            var dirName = new DirectoryInfo(dir).Name;
            if (dirName.StartsWith(".")) // Skip hidden directories
                continue;
                
            // Convert directory name to permission name (e.g., "deliverable-gates" to "DeliverableGates")
            var permissionBase = ToPascalCase(dirName);
            
            // Add View permission
            permissions.Add($"{permissionBase}.View");
            
            // Add Edit permission (skip for pages that are view-only)
            if (permissionBase != "Home")
                permissions.Add($"{permissionBase}.Edit");
        }
        
        // Add Roles permissions
        permissions.Add("Roles.View");
        permissions.Add("Roles.Edit");
        
        // Add system permissions
        permissions.Add("System.Admin");
        
        return permissions;
    }
    
    private static string ToPascalCase(string input)
    {
        if (string.IsNullOrEmpty(input))
            return input;
            
        // Replace hyphens with spaces for splitting
        var words = input.Replace("-", " ").Split(' ');
        var result = string.Empty;
        
        foreach (var word in words)
        {
            if (string.IsNullOrEmpty(word))
                continue;
                
            // Capitalize first letter of each word
            result += char.ToUpperInvariant(word[0]) + word.Substring(1);
        }
        
        return result;
    }
}
```

#### System Roles
- Automatically grant all permissions to roles with `IS_SYSTEM_ROLE` set to true
- Implement this bypass check before the regular permission validation
- System roles should have access to all functionality regardless of their entries in the ROLE_PERMISSION table
- Implement a temporary development flag that treats all users as having system role privileges
  - This will simplify initial development and testing
  - Include a clearly marked code section that can be easily disabled when ready for production testing

#### Authorization Middleware
- Implement middleware to validate permissions for each request
- Extract user role claims from the JWT token
- Check if the user has a system role (IS_SYSTEM_ROLE=true) and grant full access if true
- Otherwise, query database for permissions associated with the user's role
- Authorize access based on endpoint requirements

#### Endpoint Security
- Secure API endpoints with permission requirements using the HasPermission attribute
- The following should remain accessible without authentication:
  - Home page and related endpoints
  - Authentication endpoints (login, token validation, etc.)
- All other endpoints should be protected with appropriate permissions
- Consider implementing automatic audit logging for authorization events

### 7. Azure AD Claims and Role Mapping

#### Role Claim Mapping
- Map Azure AD role claims to database roles using case-insensitive string comparison
- Extract role name from the appropriate claim in the token (typically "roles" claim)
- Compare this value with ROLE_NAME in the database using case-insensitive comparison
- If a match is found, use the corresponding role's permissions
- If no match is found, treat the user as having no permissions

#### Unknown User Handling
- For users who exist in Azure AD but not in the local database:
  - Allow authentication (valid token)
  - Grant no permissions by default
  - Log access attempts for auditing purposes
  - Optionally, redirect to an "access request" page

### 8. Logout Implementation

#### Backend Logout Endpoints
- Implement `/api/auth/logout` endpoint to handle server-side logout
- Clear any server-side sessions or cached user information
- Invalidate any non-token-based resources associated with the user
- Return appropriate 200 OK response with cache control headers

#### Token Revocation Considerations
- Document that JWT tokens cannot be truly "revoked" until they expire
- Consider implementing a token blacklist for high-security scenarios
- Use short-lived access tokens with refresh token pattern for better security

## Migration Strategy

### Phase 1: Entity Framework Implementation
- Scaffold the new database tables
- Implement repositories and controllers
- Complete backend CRUD operations

### Phase 2: MSAL Authentication Integration
- Implement MSAL authentication on backend
- Configure token validation
- Update existing controllers to use the new authentication

### Phase 3: Permission System Implementation
- Define static permissions
- Implement permission-based authorization
- Secure endpoints with permission requirements



## Appendix: Code Examples

### Entity Framework Configuration Example

```csharp
// In FourSPMContext.cs OnModelCreating method
modelBuilder.Entity<ROLE>(entity =>
{
    entity.HasKey(e => e.GUID);
    
    entity.Property(e => e.ROLE_NAME)
        .IsRequired()
        .HasMaxLength(100);
        
    entity.Property(e => e.DISPLAY_NAME)
        .HasMaxLength(200);
        
    entity.Property(e => e.DESCRIPTION)
        .HasMaxLength(500);
        
    entity.HasIndex(e => e.ROLE_NAME)
        .IsUnique();
        
    entity.HasMany(e => e.ROLE_PERMISSIONS)
        .WithOne()
        .HasForeignKey(e => e.ROLE_NAME)
        .OnDelete(DeleteBehavior.Cascade);
});

modelBuilder.Entity<ROLE_PERMISSION>(entity =>
{
    entity.HasKey(e => e.GUID);
    
    entity.Property(e => e.ROLE_NAME)
        .IsRequired()
        .HasMaxLength(100);
        
    entity.Property(e => e.PERMISSION_NAME)
        .IsRequired()
        .HasMaxLength(200);
        
    entity.Property(e => e.DESCRIPTION)
        .HasMaxLength(500);
});
```

### Permission-Based Authorization Example

```csharp
// Custom authorization attribute
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = true)]
public class HasPermissionAttribute : AuthorizeAttribute
{
    public HasPermissionAttribute(string permission)
        : base(policy: $"Permission:{permission}")
    {
    }
}

// Usage in controller
[HasPermission("Projects.Edit")]
[HttpPost]
public async Task<IActionResult> CreateProject([FromBody] ProjectEntity projectEntity)
{
    // Implementation
}
```

### System Role Authorization Handler Example

```csharp
public class PermissionAuthorizationHandler : AuthorizationHandler<PermissionRequirement>
{
    private readonly IRoleRepository _roleRepository;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IConfiguration _configuration;

    // Development settings to easily control permissions during initial development
    private bool AllUsersHaveSystemRole => _configuration.GetValue<bool>("Authentication:AllUsersHaveSystemRole");

    public PermissionAuthorizationHandler(
        IRoleRepository roleRepository, 
        IHttpContextAccessor httpContextAccessor,
        IConfiguration configuration)
    {
        _roleRepository = roleRepository;
        _httpContextAccessor = httpContextAccessor;
        _configuration = configuration;
    }

    protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context, PermissionRequirement requirement)
    {
        // ====================================================================
        // DEVELOPMENT MODE: Grant all permissions to all users during development
        // REMOVE OR DISABLE THIS SECTION FOR PRODUCTION DEPLOYMENT
        // ====================================================================
        if (AllUsersHaveSystemRole)
        {
            context.Succeed(requirement);
            return;
        }
        // ====================================================================
        
        // Get user role name from claims
        var roleName = context.User.FindFirst("role")?.Value;
        if (string.IsNullOrEmpty(roleName))
        {
            return; // No role claim found
        }

        // Check if the user has a system role
        var role = await _roleRepository.GetByNameAsync(roleName);
        if (role != null && role.IS_SYSTEM_ROLE)
        {
            // System roles bypass permission checks and are granted all permissions
            context.Succeed(requirement);
            return;
        }

        // For non-system roles, check specific permissions
        var hasPermission = await _roleRepository.HasPermissionAsync(roleName, requirement.Permission);
        if (hasPermission)
        {
            context.Succeed(requirement);
        }
    }
}
```

### MSAL Authentication Configuration Example

```csharp
// In Startup.cs ConfigureServices method
services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddMicrosoftIdentityWebApi(Configuration.GetSection("AzureAd"));

services.AddAuthorization(options =>
{
    // Add policies for permissions
    foreach (var permission in StaticPermissions.AllPermissions)
    {
        options.AddPolicy($"Permission:{permission}", policy =>
            policy.Requirements.Add(new PermissionRequirement(permission)));
    }
});

services.AddSingleton<IAuthorizationHandler, PermissionAuthorizationHandler>();
```
