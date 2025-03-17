using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using FourSPM_WebService.Models.Shared.Enums;

namespace FourSPM_WebService.Data.EF.FourSPM
{
    [Table("PROGRESS")]
    public class PROGRESS
    {
        [Key]
        public Guid GUID { get; set; }

        [Required]
        public Guid GUID_DELIVERABLE { get; set; }

        [Required]
        public int PERIOD { get; set; }

        public DeliverableGate? GATE_ID { get; set; }

        [Required]
        [Column(TypeName = "decimal(10,2)")]
        public decimal UNITS { get; set; }

        [Required]
        public DateTime CREATED { get; set; }

        [Required]
        public Guid CREATEDBY { get; set; }

        public DateTime? UPDATED { get; set; }

        public Guid? UPDATEDBY { get; set; }

        public DateTime? DELETED { get; set; }

        public Guid? DELETEDBY { get; set; }

        [ForeignKey(nameof(GUID_DELIVERABLE))]
        public virtual DELIVERABLE? Deliverable { get; set; }
    }
}
