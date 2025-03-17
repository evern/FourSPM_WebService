using System;
using System.Collections.Generic;

namespace FourSPM_WebService.Data.OData.FourSPM
{
    public class DeliverableGateEntity
    {
        public Guid Guid { get; set; }
        public required string Name { get; set; }
        public decimal MaxPercentage { get; set; }
        public decimal? AutoPercentage { get; set; }
        
        // Audit fields
        public DateTime Created { get; set; }
        public Guid CreatedBy { get; set; }
        public DateTime? Updated { get; set; }
        public Guid? UpdatedBy { get; set; }
        public DateTime? Deleted { get; set; }
        public Guid? DeletedBy { get; set; }
    }
}
