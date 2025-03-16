using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FourSPM_WebService.Data.EF.FourSPM
{
    public class DEPARTMENT
    {
        [Key]
        public Guid GUID { get; set; }

        [Required]
        [StringLength(50)]
        public required string NAME { get; set; }

        [StringLength(500)]
        public string? DESCRIPTION { get; set; }

        [Required]
        public DateTime CREATED { get; set; }

        [Required]
        public Guid CREATEDBY { get; set; }

        public DateTime? UPDATED { get; set; }

        public Guid? UPDATEDBY { get; set; }

        public DateTime? DELETED { get; set; }

        public Guid? DELETEDBY { get; set; }
    }
}
