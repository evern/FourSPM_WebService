using System;

namespace FourSPM_WebService.Data.OData.FourSPM
{
    public class RoleEntity
    {
        public int Guid { get; set; }
        public required string Name { get; set; }
        public required string DisplayName { get; set; }
        public string? Description { get; set; }
        public bool IsSystemRole { get; set; }
        
        // Audit fields
        public DateTime Created { get; set; }
        public required string CreatedBy { get; set; }
        public DateTime? Updated { get; set; }
        public string? UpdatedBy { get; set; }
        public DateTime? Deleted { get; set; }
        public string? DeletedBy { get; set; }
    }
}
