using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace FourSPM_WebService.Data.EF.FourSPM
{
    public class ROLE
    {
        [Key]
        public Guid GUID { get; set; }
        
        [Required]
        [StringLength(50)]
        public required string NAME { get; set; }
        
        [Required]
        [StringLength(100)]
        public required string DISPLAY_NAME { get; set; }
        
        [StringLength(500)]
        public string? DESCRIPTION { get; set; }
        
        [Required]
        public bool IS_SYSTEM_ROLE { get; set; }
        
        [Required]
        public DateTime CREATED { get; set; }
        
        [Required]
        public Guid CREATEDBY { get; set; }
        
        public DateTime? UPDATED { get; set; }
        
        public Guid? UPDATEDBY { get; set; }

        public DateTime? DELETED { get; set; }

        public Guid? DELETEDBY { get; set; }
        
        // Navigation property for role permissions
        public virtual ICollection<ROLE_PERMISSION> ROLE_PERMISSIONS { get; set; } = new List<ROLE_PERMISSION>();
    }
}
