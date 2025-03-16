using System;
using System.ComponentModel.DataAnnotations;

namespace FourSPM_WebService.Data.EF.FourSPM
{
    public class DOCUMENT_TYPE
    {
        [Key]
        public Guid GUID { get; set; }
        [StringLength(3)]
        public required string CODE { get; set; }
        public string? NAME { get; set; }
        public DateTime CREATED { get; set; }
        public Guid CREATEDBY { get; set; }
        public DateTime? UPDATED { get; set; }
        public Guid? UPDATEDBY { get; set; }
        public DateTime? DELETED { get; set; }
        public Guid? DELETEDBY { get; set; }
    }
}
