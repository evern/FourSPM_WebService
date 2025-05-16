namespace FourSPM_WebService.Config
{
    /// <summary>
    /// Constants for authorization and authentication
    /// </summary>
    public static class AuthConstants
    {
        /// <summary>
        /// Role definitions that match the Azure AD app roles
        /// </summary>
        public static class Roles
        {
            /// <summary>
            /// Administrator role with full system access
            /// </summary>
            public const string Admin = "Admin";
        }
        
        /// <summary>
        /// Permission constants used for role-based authorization in the application
        /// These names are stored in the ROLE_PERMISSION table
        /// </summary>
        public static class Permissions
        {
            // Permission prefixes for dynamic permission checking
            /// <summary>
            /// Prefix for all read permissions, used for dynamic permission checking
            /// </summary>
            public const string ReadPrefix = "Permissions.";
            
            /// <summary>
            /// Prefix for all write permissions, used for dynamic permission checking
            /// </summary>
            public const string WritePrefix = "Permissions.";
            
            /// <summary>
            /// Permission that grants access to all functions
            /// </summary>
            public const string AllAccess = "Permissions.System.All";
            
            // Project permissions
            /// <summary>
            /// Permission to view projects
            /// </summary>
            public const string ReadProjects = "Permissions.Projects.Read";
            
            /// <summary>
            /// Permission to modify projects (create, edit, delete)
            /// </summary>
            public const string WriteProjects = "Permissions.Projects.Write";
            
            // Areas permissions
            /// <summary>
            /// Permission to view areas
            /// </summary>
            public const string ReadAreas = "Permissions.Areas.Read";
            
            /// <summary>
            /// Permission to modify areas (create, edit, delete)
            /// </summary>
            public const string WriteAreas = "Permissions.Areas.Write";
            
            // Clients permissions
            /// <summary>
            /// Permission to view clients
            /// </summary>
            public const string ReadClients = "Permissions.Clients.Read";
            
            /// <summary>
            /// Permission to modify clients (create, edit, delete)
            /// </summary>
            public const string WriteClients = "Permissions.Clients.Write";
            
            // Deliverables permissions
            /// <summary>
            /// Permission to view deliverables
            /// </summary>
            public const string ReadDeliverables = "Permissions.Deliverables.Read";
            
            /// <summary>
            /// Permission to modify deliverables (create, edit, delete)
            /// </summary>
            public const string WriteDeliverables = "Permissions.Deliverables.Write";
            
            // Deliverable gates permissions
            /// <summary>
            /// Permission to view deliverable gates
            /// </summary>
            public const string ReadDeliverableGates = "Permissions.DeliverableGates.Read";
            
            /// <summary>
            /// Permission to modify deliverable gates (create, edit, delete)
            /// </summary>
            public const string WriteDeliverableGates = "Permissions.DeliverableGates.Write";
            
            // Deliverable progress permissions
            /// <summary>
            /// Permission to view deliverable progress
            /// </summary>
            public const string ReadDeliverableProgress = "Permissions.DeliverableProgress.Read";
            
            /// <summary>
            /// Permission to modify deliverable progress (create, edit, delete)
            /// </summary>
            public const string WriteDeliverableProgress = "Permissions.DeliverableProgress.Write";
            
            // Disciplines permissions
            /// <summary>
            /// Permission to view disciplines
            /// </summary>
            public const string ReadDisciplines = "Permissions.Disciplines.Read";
            
            /// <summary>
            /// Permission to modify disciplines (create, edit, delete)
            /// </summary>
            public const string WriteDisciplines = "Permissions.Disciplines.Write";
            
            // Document types permissions
            /// <summary>
            /// Permission to view document types
            /// </summary>
            public const string ReadDocumentTypes = "Permissions.DocumentTypes.Read";
            
            /// <summary>
            /// Permission to modify document types (create, edit, delete)
            /// </summary>
            public const string WriteDocumentTypes = "Permissions.DocumentTypes.Write";
            
            // Variations permissions
            /// <summary>
            /// Permission to view variations
            /// </summary>
            public const string ReadVariations = "Permissions.Variations.Read";
            
            /// <summary>
            /// Permission to modify variations (create, edit, delete)
            /// </summary>
            public const string WriteVariations = "Permissions.Variations.Write";
            
            // Variation deliverables permissions
            /// <summary>
            /// Permission to view variation deliverables
            /// </summary>
            public const string ReadVariationDeliverables = "Permissions.VariationDeliverables.Read";
            
            /// <summary>
            /// Permission to modify variation deliverables (create, edit, delete)
            /// </summary>
            public const string WriteVariationDeliverables = "Permissions.VariationDeliverables.Write";
            
            // User permissions
            /// <summary>
            /// Permission to view user profiles
            /// </summary>
            public const string ReadUsers = "Permissions.Users.Read";
            
            /// <summary>
            /// Permission to modify users (create, edit, delete)
            /// </summary>
            public const string WriteUsers = "Permissions.Users.Write";
            
            // Role permissions
            /// <summary>
            /// Permission to view roles and their assigned permissions
            /// </summary>
            public const string ReadRoles = "Permissions.Roles.Read";
            
            /// <summary>
            /// Permission to modify roles and their assigned permissions
            /// </summary>
            public const string WriteRoles = "Permissions.Roles.Write";
            
            // Role Permissions management
            /// <summary>
            /// Permission to view role permissions
            /// </summary>
            public const string ReadRolePermissions = "Permissions.RolePermissions.Read";
            
            /// <summary>
            /// Permission to modify role permissions (create, edit, delete)
            /// </summary>
            public const string WriteRolePermissions = "Permissions.RolePermissions.Write";
            
            // System permissions
            /// <summary>
            /// Permission for full administrative access
            /// </summary>
            public const string AdminAccess = "Permissions.System.Admin";
        }
    }
}
