using System;
using System.ComponentModel.DataAnnotations;

namespace FourSPM_WebService.Data.OData.FourSPM
{
    public class DeliverableTypeEntity
    {
        [Key]
        [Required]
        public Guid Guid { get; set; }
        [Required]
        public string Name { get; set; }
        public string? Description { get; set; }
        public DateTime Created { get; set; }
        public Guid CreatedBy { get; set; }
        public DateTime? Updated { get; set; }
        public Guid? UpdatedBy { get; set; }
        public DateTime? Deleted { get; set; }
        public Guid? DeletedBy { get; set; }
    }
}
