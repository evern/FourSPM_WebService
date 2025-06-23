using System.Collections.Generic;
using FourSPM_WebService.Data.OData.FourSPM;

namespace FourSPM_WebService.Data.Constants
{
    /// <summary>
    /// Constants for system permissions
    /// </summary>
    public static class PermissionConstants
    {
        // Permission types
        public const string TypeAccessLevel = "AccessLevel";
        public const string TypeToggle = "Toggle";

        // Projects feature
        public const string ProjectsView = "projects.view";
        public const string ProjectsEdit = "projects.edit";
        
        // Deliverables feature
        public const string DeliverablesView = "deliverables.view";
        public const string DeliverablesEdit = "deliverables.edit";
        
        // Areas feature
        public const string AreasView = "areas.view";
        public const string AreasEdit = "areas.edit";
        
        // Clients feature
        public const string ClientsView = "clients.view";
        public const string ClientsEdit = "clients.edit";
        
        // Disciplines feature
        public const string DisciplinesView = "disciplines.view";
        public const string DisciplinesEdit = "disciplines.edit";
        
        // Document Types feature
        public const string DocumentTypesView = "document-types.view";
        public const string DocumentTypesEdit = "document-types.edit";
        
        // Deliverable Gates feature
        public const string DeliverableGatesView = "deliverable-gates.view";
        public const string DeliverableGatesEdit = "deliverable-gates.edit";
        
        // Variations feature
        public const string VariationsView = "variations.view";
        public const string VariationsEdit = "variations.edit";
        
        // Variation Deliverables feature
        public const string VariationDeliverablesView = "variation-deliverables.view";
        public const string VariationDeliverablesEdit = "variation-deliverables.edit";
        
        // Progress feature
        public const string ProgressView = "progress.view";
        public const string ProgressEdit = "progress.edit";
        
        // Roles feature
        public const string RolesView = "roles.view";
        public const string RolesEdit = "roles.edit";
        
        // Permission management feature (meta-permission)
        public const string PermissionsView = "permissions.view";
        public const string PermissionsEdit = "permissions.edit";
        
        // Toggle permission constants
        public const string CostInformationToggle = "cost-information.toggle";
        
        /// <summary>
        /// Gets all static permissions grouped by feature
        /// </summary>
        public static List<StaticPermissionEntity> GetStaticPermissions()
        {
            return new List<StaticPermissionEntity>
            {
                // Projects
                new StaticPermissionEntity
                {
                    FeatureKey = "projects",
                    DisplayName = "Projects",
                    Description = "Access to projects module",
                    FeatureGroup = "Project Management",
                    PermissionType = TypeAccessLevel
                },
                
                // Deliverables
                new StaticPermissionEntity
                {
                    FeatureKey = "deliverables",
                    DisplayName = "Deliverables",
                    Description = "Access to deliverables management",
                    FeatureGroup = "Project Management"
                },
                
                // Areas
                new StaticPermissionEntity
                {
                    FeatureKey = "areas",
                    DisplayName = "Areas",
                    Description = "Access to project areas",
                    FeatureGroup = "Project Management"
                },
                
                // Clients
                new StaticPermissionEntity
                {
                    FeatureKey = "clients",
                    DisplayName = "Clients",
                    Description = "Access to client records",
                    FeatureGroup = "Administration"
                },
                
                // Disciplines
                new StaticPermissionEntity
                {
                    FeatureKey = "disciplines",
                    DisplayName = "Disciplines",
                    Description = "Access to disciplines configuration",
                    FeatureGroup = "Administration"
                },
                
                // Document Types
                new StaticPermissionEntity
                {
                    FeatureKey = "document-types",
                    DisplayName = "Document Types",
                    Description = "Access to document types configuration",
                    FeatureGroup = "Administration"
                },
                
                // Deliverable Gates
                new StaticPermissionEntity
                {
                    FeatureKey = "deliverable-gates",
                    DisplayName = "Deliverable Gates",
                    Description = "Access to deliverable gates configuration",
                    FeatureGroup = "Administration"
                },
                
                // Variations
                new StaticPermissionEntity
                {
                    FeatureKey = "variations",
                    DisplayName = "Variations",
                    Description = "Access to variations management",
                    FeatureGroup = "Project Management"
                },
                
                // Variation Deliverables
                new StaticPermissionEntity
                {
                    FeatureKey = "variation-deliverables",
                    DisplayName = "Variation Deliverables",
                    Description = "Access to deliverables within variations",
                    FeatureGroup = "Project Management"
                },
                
                // Progress
                new StaticPermissionEntity
                {
                    FeatureKey = "progress",
                    DisplayName = "Deliverable Progress",
                    Description = "Access to progress tracking for deliverables",
                    FeatureGroup = "Project Management"
                },
                
                // Roles
                new StaticPermissionEntity
                {
                    FeatureKey = "roles",
                    DisplayName = "Roles",
                    Description = "Access to role management",
                    FeatureGroup = "Security"
                },
                
                // Permissions
                new StaticPermissionEntity
                {
                    FeatureKey = "permissions",
                    DisplayName = "Permissions",
                    Description = "Access to permission management",
                    FeatureGroup = "Security"
                },
                
                // Toggle Permission samples
                new StaticPermissionEntity
                {
                    FeatureKey = "cost-information",
                    DisplayName = "Cost Information",
                    Description = "Enable or disable hiding cost information",
                    FeatureGroup = "User Preferences",
                    PermissionType = TypeToggle
                }
            };
        }
    }
}
