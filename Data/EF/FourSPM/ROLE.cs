using System;
using System.ComponentModel.DataAnnotations;

namespace FourSPM_WebService.Data.EF.FourSPM
{
    public class ROLE
    {
        [Key]
        public int GUID { get; set; }
        
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
        public required string CREATEDBY { get; set; }
        
        public DateTime? UPDATED { get; set; }
        
        public string? UPDATEDBY { get; set; }
        
        public DateTime? DELETED { get; set; }
        
        public string? DELETEDBY { get; set; }
    }
}
