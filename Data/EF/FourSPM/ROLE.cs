using System;

namespace FourSPM_WebService.Data.EF.FourSPM
{
    /// <summary>
    /// Represents a role in the system that can have permissions assigned to it
    /// </summary>
    public class ROLE
    {
        /// <summary>
        /// Primary key for the role
        /// </summary>
        public Guid GUID { get; set; }
        
        /// <summary>
        /// Unique name identifier for the role, used for authentication
        /// </summary>
        public string ROLE_NAME { get; set; } = string.Empty;
        
        /// <summary>
        /// User-friendly display name for the role
        /// </summary>
        public string? DISPLAY_NAME { get; set; }
        
        /// <summary>
        /// Detailed description of the role's purpose and intended use
        /// </summary>
        public string? DESCRIPTION { get; set; }
        
        /// <summary>
        /// Indicates if this is a system role that should not be modified by users
        /// </summary>
        public bool IS_SYSTEM_ROLE { get; set; }
        
        // Audit fields
        /// <summary>
        /// Date and time when the role was created
        /// </summary>
        public DateTime CREATED { get; set; }
        
        /// <summary>
        /// ID of the user who created the role
        /// </summary>
        public Guid CREATEDBY { get; set; }
        
        /// <summary>
        /// Date and time when the role was last updated
        /// </summary>
        public DateTime? UPDATED { get; set; }
        
        /// <summary>
        /// ID of the user who last updated the role
        /// </summary>
        public Guid? UPDATEDBY { get; set; }
        
        /// <summary>
        /// Date and time when the role was deleted (soft delete)
        /// </summary>
        public DateTime? DELETED { get; set; }
        
        /// <summary>
        /// ID of the user who deleted the role
        /// </summary>
        public Guid? DELETEDBY { get; set; }

        // Navigation properties
        /// <summary>
        /// Collection of role-permission mappings associated with this role
        /// </summary>
        public virtual ICollection<ROLE_PERMISSION>? RolePermissions { get; set; }
    }
}
