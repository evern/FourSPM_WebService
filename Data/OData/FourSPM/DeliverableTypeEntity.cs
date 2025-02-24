using System;

namespace FourSPM_WebService.Data.OData.FourSPM
{
    public class DeliverableTypeEntity
    {
        public Guid ID { get; set; }
        public required string NAME { get; set; }
        public string? DESCRIPTION { get; set; }
        public DateTime CREATED { get; set; }
        public Guid CREATEDBY { get; set; }
        public DateTime? UPDATED { get; set; }
        public Guid? UPDATEDBY { get; set; }
        public DateTime? DELETED { get; set; }
        public Guid? DELETEDBY { get; set; }
    }
}
