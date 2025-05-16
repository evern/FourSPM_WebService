using System;

namespace FourSPM_WebService.Data.OData.FourSPM
{
    public class RolePermissionEntity
    {
        public int Guid { get; set; }
        public int RoleGuid { get; set; }
        public required string Permission { get; set; }
        
        // Audit fields
        public DateTime Created { get; set; }
        public required string CreatedBy { get; set; }
        public DateTime? Updated { get; set; }
        public string? UpdatedBy { get; set; }
        public DateTime? Deleted { get; set; }
        public string? DeletedBy { get; set; }
        
        // Navigation property
        public RoleEntity? Role { get; set; }
    }
}
