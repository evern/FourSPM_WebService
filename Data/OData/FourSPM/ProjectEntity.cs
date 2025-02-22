using FourSPM_WebService.Models.Shared.Enums;
using System.ComponentModel.DataAnnotations;

namespace FourSPM_WebService.Data.OData.FourSPM
{
    public class ProjectEntity
    {
        [Key]
        [Required]
        public string ClientNumber { get; set; } = null!;

        [Key]
        [Required]
        public string ProjectNumber { get; set; } = null!;

        public string? ClientContact { get; set; }

        public string? PurchaseOrderNumber { get; set; }

        [Required]
        public ProjectStatus ProjectStatus { get; set; }

        public DateTime Created { get; set; }

        public Guid CreatedBy { get; set; }

        public DateTime? Updated { get; set; }

        public Guid? UpdatedBy { get; set; }

        public DateTime? Deleted { get; set; }

        public Guid? DeletedBy { get; set; }
    }
}
