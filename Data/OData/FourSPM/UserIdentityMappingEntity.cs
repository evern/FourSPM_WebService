using System;

namespace FourSPM_WebService.Data.OData.FourSPM
{
    public class UserIdentityMappingEntity
    {
        public Guid Guid { get; set; }
        public required string Username { get; set; }
        public required string Email { get; set; }
        public DateTime Created { get; set; }
        public DateTime? LastLogin { get; set; }
        public DateTime? Deleted { get; set; }
        public Guid? DeletedBy { get; set; }
    }
}
