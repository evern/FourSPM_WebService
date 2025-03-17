using System;
using System.ComponentModel.DataAnnotations;

namespace FourSPM_WebService.Data.OData.FourSPM
{
    public class ProgressEntity
    {
        [Key]
        [Required]
        public Guid Guid { get; set; }

        [Required]
        public Guid DeliverableGuid { get; set; }

        [Required]
        public int Period { get; set; }

        public Guid? DeliverableGateGuid { get; set; }

        [Required]
        public decimal Units { get; set; }

        [Required]
        public DateTime Created { get; set; }

        [Required]
        public Guid CreatedBy { get; set; }

        public DateTime? Updated { get; set; }

        public Guid? UpdatedBy { get; set; }

        public DateTime? Deleted { get; set; }

        public Guid? DeletedBy { get; set; }

        public virtual DeliverableEntity? Deliverable { get; set; }
        
        public virtual DeliverableGateEntity? DeliverableGate { get; set; }
    }
}
