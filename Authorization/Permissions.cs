using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace FourSPM_WebService.Authorization
{
    /// <summary>
    /// Static class defining all permission constants used for authorization
    /// </summary>
    public static class Permissions
    {
        #region Roles
        public const string ViewRoles = "Roles.View";
        public const string EditRoles = "Roles.Edit";
        #endregion

        #region Projects
        public const string ViewProjects = "Projects.View";
        public const string EditProjects = "Projects.Edit";
        public const string DeleteProjects = "Projects.Delete";
        #endregion

        #region Deliverables
        public const string ViewDeliverables = "Deliverables.View";
        public const string EditDeliverables = "Deliverables.Edit";
        public const string DeleteDeliverables = "Deliverables.Delete";
        #endregion

        #region Areas
        public const string ViewAreas = "Areas.View";
        public const string EditAreas = "Areas.Edit";
        public const string DeleteAreas = "Areas.Delete";
        #endregion

        #region Clients
        public const string ViewClients = "Clients.View";
        public const string EditClients = "Clients.Edit";
        public const string DeleteClients = "Clients.Delete";
        #endregion

        #region Disciplines
        public const string ViewDisciplines = "Disciplines.View";
        public const string EditDisciplines = "Disciplines.Edit";
        public const string DeleteDisciplines = "Disciplines.Delete";
        #endregion

        #region Document Types
        public const string ViewDocumentTypes = "DocumentTypes.View";
        public const string EditDocumentTypes = "DocumentTypes.Edit";
        public const string DeleteDocumentTypes = "DocumentTypes.Delete";
        #endregion

        #region Progress
        public const string ViewProgress = "Progress.View";
        public const string EditProgress = "Progress.Edit";
        #endregion

        #region Variations
        public const string ViewVariations = "Variations.View";
        public const string EditVariations = "Variations.Edit";
        public const string DeleteVariations = "Variations.Delete";
        public const string ApproveVariations = "Variations.Approve";
        #endregion

        #region Helper Methods
        /// <summary>
        /// Gets all permission constants defined in this class
        /// </summary>
        /// <returns>A collection of all permission strings</returns>
        public static IEnumerable<string> GetAllPermissions()
        {
            return typeof(Permissions)
                .GetFields(BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy)
                .Where(fi => fi.IsLiteral && !fi.IsInitOnly && fi.FieldType == typeof(string))
                .Select(x => (string?)x.GetRawConstantValue())
                .Where(x => x != null)
                .Select(x => x!)
                .ToList();
        }

        /// <summary>
        /// Gets all permissions with their categories based on the naming convention
        /// </summary>
        /// <returns>A collection of tuples containing the permission and its category</returns>
        public static IEnumerable<(string Permission, string Category)> GetPermissionsWithCategories()
        {
            return GetAllPermissions()
                .Select(p => {
                    var parts = p.Split('.');
                    return (Permission: p, Category: parts[0]);
                });
        }

        /// <summary>
        /// Check if a view permission should be automatically granted when an edit permission is assigned
        /// </summary>
        /// <param name="permission">The permission to check</param>
        /// <returns>The implied view permission if applicable, null otherwise</returns>
        public static string? GetImpliedViewPermission(string permission)
        {
            if (permission.EndsWith(".Edit") || permission.EndsWith(".Delete") || permission.EndsWith(".Approve"))
            {
                var category = permission.Split('.')[0];
                return $"{category}.View";
            }
            
            return null;
        }
        #endregion
    }
}
