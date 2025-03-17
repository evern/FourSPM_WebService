using System;
using System.Collections.Generic;

namespace FourSPM_WebService.Data.EF.FourSPM
{
    public class DELIVERABLE_GATE
    {
        public Guid GUID { get; set; }
        public required string NAME { get; set; }
        public decimal MAX_PERCENTAGE { get; set; }
        public decimal? AUTO_PERCENTAGE { get; set; }
        
        // Audit fields
        public DateTime CREATED { get; set; }
        public Guid CREATEDBY { get; set; }
        public DateTime? UPDATED { get; set; }
        public Guid? UPDATEDBY { get; set; }
        public DateTime? DELETED { get; set; }
        public Guid? DELETEDBY { get; set; }
        
        // Navigation properties
        public virtual ICollection<PROGRESS> ProgressItems { get; set; } = new List<PROGRESS>();
    }
}
