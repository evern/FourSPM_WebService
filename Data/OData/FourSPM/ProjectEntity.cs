using System.ComponentModel.DataAnnotations;

namespace FourSPM_WebService.Data.OData.FourSPM
{
    public class ProjectEntity
    {
        [Key] // Required for OData
        public Guid Guid { get; set; }

        [Required]
        public string Number { get; set; } = null!;

        public string? Name { get; set; }

        public string? Client { get; set; }

        public DateTime Created { get; set; }

        public Guid CreatedBy { get; set; }

        public DateTime? Updated { get; set; }

        public Guid? UpdatedBy { get; set; }

        public DateTime? Deleted { get; set; }

        public Guid? DeletedBy { get; set; }
    }
}
