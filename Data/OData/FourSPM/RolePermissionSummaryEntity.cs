using System;

namespace FourSPM_WebService.Data.OData.FourSPM
{
    /// <summary>
    /// Represents a comprehensive permission summary for a role,
    /// combining static permission information with role-specific access level
    /// </summary>
    public class RolePermissionSummaryEntity
    {
        /// <summary>
        /// Unique identifier for the role permission assignment (view)
        /// </summary>
        public Guid? ViewPermissionGuid { get; set; }
        
        /// <summary>
        /// Unique identifier for the edit permission assignment
        /// </summary>
        public Guid? EditPermissionGuid { get; set; }
        
        /// <summary>
        /// Unique identifier for toggle-type permission assignment
        /// For toggle permissions (enabled/disabled) rather than access level permissions
        /// </summary>
        public Guid? TogglePermissionGuid { get; set; }

        /// <summary>
        /// Unique feature identifier (e.g., "deliverables")
        /// </summary>
        public string? FeatureKey { get; set; }
        
        /// <summary>
        /// User-friendly display name (e.g., "Deliverables")
        /// </summary>
        public string? DisplayName { get; set; }
        
        /// <summary>
        /// Detailed description of the feature
        /// </summary>
        public string? Description { get; set; }
        
        /// <summary>
        /// Grouping category (e.g., "Project Management")
        /// </summary>
        public string? FeatureGroup { get; set; }
        
        /// <summary>
        /// Current permission level for this role: 0=None, 1=ReadOnly, 2=FullAccess
        /// For access level permissions
        /// </summary>
        public int PermissionLevel { get; set; }

        /// <summary>
        /// String representation of the permission level based on permission type:
        /// For AccessLevel: 'No Access', 'Read-Only', 'Full Access'
        /// For Toggle: 'Disabled', 'Enabled'
        /// </summary>
        public string? PermissionLevelText { get; set; }

        /// <summary>
        /// Indicates the type of permission: 'AccessLevel' or 'Toggle'
        /// AccessLevel uses PermissionLevel property
        /// Toggle uses TogglePermissionGuid (present = enabled, null = disabled)
        /// </summary>
        public string PermissionType { get; set; } = "AccessLevel";
    }
}
