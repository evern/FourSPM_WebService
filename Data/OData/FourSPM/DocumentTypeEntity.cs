using System;

namespace FourSPM_WebService.Data.OData.FourSPM
{
    public class DocumentTypeEntity
    {
        public Guid Guid { get; set; }
        public required string Code { get; set; }
        public string? Name { get; set; }
        // Audit fields
        public DateTime Created { get; set; }
        public Guid CreatedBy { get; set; }
        public DateTime? Updated { get; set; }
        public Guid? UpdatedBy { get; set; }
        public DateTime? Deleted { get; set; }
        public Guid? DeletedBy { get; set; }
    }
}
