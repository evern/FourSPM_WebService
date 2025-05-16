using System;
using System.Collections.Generic;

namespace FourSPM_WebService.Data.OData.FourSPM
{
    /// <summary>
    /// OData entity representing a role in the system
    /// </summary>
    public class RoleEntity
    {
        /// <summary>
        /// Primary key for the role
        /// </summary>
        public Guid Guid { get; set; }
        
        /// <summary>
        /// Unique name identifier for the role, used for authentication
        /// </summary>
        public required string RoleName { get; set; }
        
        /// <summary>
        /// User-friendly display name for the role
        /// </summary>
        public string? DisplayName { get; set; }
        
        /// <summary>
        /// Detailed description of the role's purpose and intended use
        /// </summary>
        public string? Description { get; set; }
        
        /// <summary>
        /// Indicates if this is a system role that should not be modified by users
        /// </summary>
        public bool IsSystemRole { get; set; }
        
        // Audit fields
        /// <summary>
        /// Date and time when the role was created
        /// </summary>
        public DateTime Created { get; set; }
        
        /// <summary>
        /// ID of the user who created the role
        /// </summary>
        public Guid CreatedBy { get; set; }
        
        /// <summary>
        /// Date and time when the role was last updated
        /// </summary>
        public DateTime? Updated { get; set; }
        
        /// <summary>
        /// ID of the user who last updated the role
        /// </summary>
        public Guid? UpdatedBy { get; set; }
        
        /// <summary>
        /// Date and time when the role was deleted (soft delete)
        /// </summary>
        public DateTime? Deleted { get; set; }
        
        /// <summary>
        /// ID of the user who deleted the role
        /// </summary>
        public Guid? DeletedBy { get; set; }
    }
}
