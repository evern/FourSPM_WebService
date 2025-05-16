using System;

namespace FourSPM_WebService.Data.OData.FourSPM
{
    public class RolePermissionEntity
    {
        public Guid Guid { get; set; }
        public Guid RoleGuid { get; set; }
        public required string Permission { get; set; }
        
        // Audit fields
        public DateTime Created { get; set; }
        public Guid CreatedBy { get; set; }
        public DateTime? Updated { get; set; }
        public Guid? UpdatedBy { get; set; }
        public DateTime? Deleted { get; set; }
        public Guid? DeletedBy { get; set; }
        
        // Navigation property
        public RoleEntity? Role { get; set; }
    }
}
