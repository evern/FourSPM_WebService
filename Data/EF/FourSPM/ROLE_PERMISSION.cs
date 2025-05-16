using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FourSPM_WebService.Data.EF.FourSPM
{
    public class ROLE_PERMISSION
    {
        [Key]
        public int GUID { get; set; }
        
        [Required]
        [ForeignKey("ROLE")]
        public int GUID_ROLE { get; set; }
        
        [Required]
        [StringLength(100)]
        public required string PERMISSION { get; set; }
        
        [Required]
        public DateTime CREATED { get; set; }
        
        [Required]
        public required string CREATEDBY { get; set; }
        
        public DateTime? UPDATED { get; set; }
        
        public string? UPDATEDBY { get; set; }
        
        public DateTime? DELETED { get; set; }
        
        public string? DELETEDBY { get; set; }
        
        // Navigation property
        public virtual ROLE? ROLE { get; set; }
    }
}
