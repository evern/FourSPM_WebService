using System;

namespace FourSPM_WebService.Data.OData.FourSPM
{
    /// <summary>
    /// Represents a static permission in the system
    /// </summary>
    public class StaticPermissionEntity
    {
        /// <summary>
        /// Unique feature identifier (e.g., "deliverables")
        /// </summary>
        public string FeatureKey { get; set; }

        /// <summary>
        /// User-friendly display name (e.g., "Deliverables")
        /// </summary>
        public string DisplayName { get; set; }

        /// <summary>
        /// Detailed description of the feature
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// Grouping category (e.g., "Project Management")
        /// </summary>
        public string FeatureGroup { get; set; }
        
        /// <summary>
        /// Type of permission: 'AccessLevel' or 'Toggle'
        /// Default is 'AccessLevel' for backward compatibility
        /// </summary>
        public string PermissionType { get; set; } = "AccessLevel";
    }
}
