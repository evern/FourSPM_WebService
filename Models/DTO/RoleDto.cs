using System;
using System.Collections.Generic;

namespace FourSPM_WebService.Models.DTO
{
    /// <summary>
    /// Data transfer object for role information
    /// </summary>
    public class RoleDto
    {
        /// <summary>
        /// Gets or sets the unique identifier (GUID) of the role
        /// </summary>
        public int Guid { get; set; }

        /// <summary>
        /// Gets or sets the name of the role
        /// </summary>
        public string Name { get; set; } = null!;

        /// <summary>
        /// Gets or sets the display name of the role
        /// </summary>
        public string DisplayName { get; set; } = null!;

        /// <summary>
        /// Gets or sets the description of the role
        /// </summary>
        public string? Description { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this is a system role
        /// </summary>
        public bool IsSystemRole { get; set; }

        /// <summary>
        /// Gets or sets the permissions associated with this role
        /// </summary>
        public List<string>? Permissions { get; set; }
    }

    /// <summary>
    /// Data transfer object for creating a new role
    /// </summary>
    public class CreateRoleDto
    {
        /// <summary>
        /// Gets or sets the name of the role
        /// </summary>
        public string Name { get; set; } = null!;

        /// <summary>
        /// Gets or sets the display name of the role
        /// </summary>
        public string DisplayName { get; set; } = null!;

        /// <summary>
        /// Gets or sets the description of the role
        /// </summary>
        public string? Description { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this is a system role
        /// </summary>
        public bool IsSystemRole { get; set; }

        /// <summary>
        /// Gets or sets the initial permissions to assign to the role
        /// </summary>
        public List<string>? Permissions { get; set; }
    }

    /// <summary>
    /// Data transfer object for updating an existing role
    /// </summary>
    public class UpdateRoleDto
    {
        /// <summary>
        /// Gets or sets the name of the role
        /// </summary>
        public string? Name { get; set; }

        /// <summary>
        /// Gets or sets the display name of the role
        /// </summary>
        public string? DisplayName { get; set; }

        /// <summary>
        /// Gets or sets the description of the role
        /// </summary>
        public string? Description { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this is a system role
        /// </summary>
        public bool? IsSystemRole { get; set; }
    }

    /// <summary>
    /// Data transfer object for role permissions operations
    /// </summary>
    public class RolePermissionsDto
    {
        /// <summary>
        /// Gets or sets the unique identifier (GUID) of the role
        /// </summary>
        public int RoleGuid { get; set; }

        /// <summary>
        /// Gets or sets the name of the role
        /// </summary>
        public string RoleName { get; set; } = null!;

        /// <summary>
        /// Gets or sets a value indicating whether this is a system role
        /// </summary>
        public bool IsSystemRole { get; set; }

        /// <summary>
        /// Gets or sets the list of permissions for the role
        /// </summary>
        public List<string> Permissions { get; set; } = new List<string>();
    }
}
