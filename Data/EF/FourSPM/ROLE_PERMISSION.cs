using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FourSPM_WebService.Data.EF.FourSPM
{
    /// <summary>
    /// Represents a role permission mapping in the system
    /// </summary>
    public class ROLE_PERMISSION
    {
        /// <summary>
        /// Primary key
        /// </summary>
        [Key]
        public Guid GUID { get; set; }
        
        /// <summary>
        /// Name of the role (matches Azure AD role name)
        /// </summary>
        [Required]
        [StringLength(100)]
        public required string ROLE_NAME { get; set; }
        
        /// <summary>
        /// Name of the permission
        /// </summary>
        [Required]
        [StringLength(200)]
        public required string PERMISSION_NAME { get; set; }
        
        /// <summary>
        /// Whether the permission is granted to the role
        /// </summary>
        [Required]
        public bool IS_GRANTED { get; set; } = true;
        
        /// <summary>
        /// Optional description of this role-permission mapping
        /// </summary>
        [StringLength(500)]
        public string? DESCRIPTION { get; set; }
        
        /// <summary>
        /// Date and time when this record was created
        /// </summary>
        [Required]
        public DateTime CREATED { get; set; }
        
        /// <summary>
        /// ID of the user who created this record
        /// </summary>
        [Required]
        public Guid CREATEDBY { get; set; }
        
        /// <summary>
        /// Date and time when this record was last updated
        /// </summary>
        public DateTime? UPDATED { get; set; }
        
        /// <summary>
        /// ID of the user who last updated this record
        /// </summary>
        public Guid? UPDATEDBY { get; set; }
        
        /// <summary>
        /// Date and time when this record was deleted (null if not deleted)
        /// </summary>
        public DateTime? DELETED { get; set; }
        
        /// <summary>
        /// ID of the user who deleted this record
        /// </summary>
        public Guid? DELETEDBY { get; set; }
        
        /// <summary>
        /// Navigation property to the role
        /// </summary>
        public virtual ROLE? Role { get; set; }
    }
}
