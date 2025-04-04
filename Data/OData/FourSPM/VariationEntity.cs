using System;
using System.Text.Json.Serialization;

namespace FourSPM_WebService.Data.OData.FourSPM
{
    public class VariationEntity
    {
        public Guid Guid { get; set; }

        public Guid ProjectGuid { get; set; }

        public string Name { get; set; } = string.Empty;

        public string? Comments { get; set; }

        public DateTime? Submitted { get; set; }

        public Guid? SubmittedBy { get; set; }

        public DateTime? ClientApproved { get; set; }

        public Guid? ClientApprovedBy { get; set; }

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
