using System.ComponentModel.DataAnnotations;

namespace FourSPM_WebService.Data.OData.FourSPM
{
    /// <summary>
    /// Represents a role permission mapping in the OData model
    /// </summary>
    public class RolePermissionEntity
    {
        /// <summary>
        /// Primary key
        /// </summary>
        [Key]
        [Required]
        public Guid Guid { get; set; }

        /// <summary>
        /// Name of the role (matches Azure AD role name)
        /// </summary>
        [Required]
        [MaxLength(100)]
        public string RoleName { get; set; } = null!;

        /// <summary>
        /// Name of the permission
        /// </summary>
        [Required]
        [MaxLength(200)]
        public string PermissionName { get; set; } = null!;

        /// <summary>
        /// Whether the permission is granted to the role
        /// </summary>
        [Required]
        public bool IsGranted { get; set; } = true;

        /// <summary>
        /// Optional description of this role-permission mapping
        /// </summary>
        [MaxLength(500)]
        public string? Description { get; set; }

        /// <summary>
        /// Date and time when this record was created
        /// </summary>
        public DateTime Created { get; set; }

        /// <summary>
        /// ID of the user who created this record
        /// </summary>
        public Guid CreatedBy { get; set; }

        /// <summary>
        /// Date and time when this record was last updated
        /// </summary>
        public DateTime? Updated { get; set; }

        /// <summary>
        /// ID of the user who last updated this record
        /// </summary>
        public Guid? UpdatedBy { get; set; }

        /// <summary>
        /// Date and time when this record was deleted (null if not deleted)
        /// </summary>
        public DateTime? Deleted { get; set; }

        /// <summary>
        /// ID of the user who deleted this record
        /// </summary>
        public Guid? DeletedBy { get; set; }
    }
}
