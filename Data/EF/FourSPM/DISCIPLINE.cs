using System;

namespace FourSPM_WebService.Data.EF.FourSPM
{
    public class DISCIPLINE
    {
        public Guid GUID { get; set; }
        public required string CODE { get; set; }
        public string? NAME { get; set; }
        public DateTime CREATED { get; set; }
        public Guid CREATEDBY { get; set; }
        public DateTime? UPDATED { get; set; }
        public Guid? UPDATEDBY { get; set; }
        public DateTime? DELETED { get; set; }
        public Guid? DELETEDBY { get; set; }
        
        // Navigation properties could be added here if there are relationships
        // For example: public virtual ICollection<DELIVERABLE> Deliverables { get; set; }
    }
}
