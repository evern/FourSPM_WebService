using System;
using System.Text.Json.Serialization;

namespace FourSPM_WebService.Data.OData.FourSPM
{
    public class AreaEntity
    {
        public Guid Guid { get; set; }

        public Guid ProjectGuid { get; set; }

        public string Number { get; set; } = string.Empty;

        public string Description { get; set; } = string.Empty;

        // Audit fields
        public DateTime Created { get; set; }

        public Guid CreatedBy { get; set; }

        public DateTime? Updated { get; set; }

        public Guid? UpdatedBy { get; set; }

        public DateTime? Deleted { get; set; }

        public Guid? DeletedBy { get; set; }

        // Navigation properties
        [JsonIgnore]
        public ProjectEntity? Project { get; set; }
    }
}
