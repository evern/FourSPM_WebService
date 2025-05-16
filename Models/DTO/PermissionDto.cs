using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace FourSPM_WebService.Models.DTO
{
    /// <summary>
    /// Data transfer object for permission information
    /// </summary>
    public class PermissionDto
    {
        /// <summary>
        /// Gets or sets the name of the permission
        /// </summary>
        public string Name { get; set; } = null!;

        /// <summary>
        /// Gets or sets the category of the permission (e.g. "Roles", "Projects")
        /// </summary>
        public string Category { get; set; } = null!;

        /// <summary>
        /// Gets or sets a value indicating whether this permission is granted to a role
        /// </summary>
        public bool IsGranted { get; set; }
    }

    /// <summary>
    /// Data transfer object for updating role permissions
    /// </summary>
    public class UpdateRolePermissionsDto
    {
        /// <summary>
        /// Gets or sets the list of permissions to assign to the role
        /// </summary>
        [Required]
        public List<string> Permissions { get; set; } = new List<string>();
    }
}
