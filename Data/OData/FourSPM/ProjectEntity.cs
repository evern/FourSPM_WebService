using FourSPM_WebService.Models.Shared.Enums;
using System.ComponentModel.DataAnnotations;
using System.Collections.Generic;

namespace FourSPM_WebService.Data.OData.FourSPM
{
    public class ProjectEntity
    {
        [Key]
        [Required]
        public Guid Guid { get; set; }

        [Required]
        [MaxLength(3)]
        public string ClientNumber { get; set; } = null!;

        [Required]
        [MaxLength(2)]
        public string ProjectNumber { get; set; } = null!;

        public string? Name { get; set; }

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

        public virtual ICollection<DeliverableEntity> Deliverables { get; set; } = new List<DeliverableEntity>();
    }
}
