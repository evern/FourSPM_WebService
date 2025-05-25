using System;

namespace FourSPM_WebService.Models.Request
{
    /// <summary>
    /// Request model for setting permission levels
    /// </summary>
    public class SetPermissionLevelRequest
    {
        /// <summary>
        /// The GUID of the role to modify permissions for
        /// </summary>
        public Guid RoleId { get; set; }
        
        /// <summary>
        /// The feature key identifier (e.g., "projects", "deliverables")
        /// </summary>
        public required string FeatureKey { get; set; }
        
        /// <summary>
        /// The action to perform: "NoAccess", "ReadOnly", "FullAccess", "Enable", or "Disable"
        /// </summary>
        public required string Action { get; set; }
    }
}
