using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FourSPM_WebService.Data.EF.FourSPM
{
    public class ROLE_PERMISSION
    {
        [Key]
        public Guid GUID { get; set; }
        
        [Required]
        [ForeignKey("ROLE")]
        public Guid GUID_ROLE { get; set; }
        
        [Required]
        [StringLength(100)]
        public required string PERMISSION { get; set; }
        
        [Required]
        public DateTime CREATED { get; set; }
        
        [Required]
        public Guid CREATEDBY { get; set; }
        
        public DateTime? UPDATED { get; set; }
        
        public Guid? UPDATEDBY { get; set; }

        public DateTime? DELETED { get; set; }

        public Guid? DELETEDBY { get; set; }
        
        // Navigation property
        public virtual ROLE? ROLE { get; set; }
    }
}
