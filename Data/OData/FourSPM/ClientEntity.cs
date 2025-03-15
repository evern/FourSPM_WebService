using System.ComponentModel.DataAnnotations;
using System.Collections.Generic;

namespace FourSPM_WebService.Data.OData.FourSPM
{
    public class ClientEntity
    {
        [Key]
        [Required]
        public Guid Guid { get; set; }

        [Required]
        [MaxLength(3)]
        public string Number { get; set; } = null!;

        [MaxLength(500)]
        public string? Description { get; set; }

        [MaxLength(500)]
        public string? ClientContactName { get; set; }

        [MaxLength(100)]
        public string? ClientContactNumber { get; set; }

        [MaxLength(100)]
        public string? ClientContactEmail { get; set; }

        public DateTime Created { get; set; }

        public Guid CreatedBy { get; set; }

        public DateTime? Updated { get; set; }

        public Guid? UpdatedBy { get; set; }

        public DateTime? Deleted { get; set; }

        public Guid? DeletedBy { get; set; }

        public virtual ICollection<ProjectEntity> Projects { get; set; } = new List<ProjectEntity>();
    }
}
