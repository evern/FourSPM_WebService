using System;
using System.ComponentModel.DataAnnotations;
using System.Collections.Generic;
using FourSPM_WebService.Data.EF.FourSPM;

namespace FourSPM_WebService.Data.OData.FourSPM
{
    public class DeliverableEntity
    {
        [Key]
        [Required]
        public Guid Guid { get; set; }
        public Guid ProjectGuid { get; set; }
        [RegularExpression(@"[0-9][0-9][0-9]")]
        public string? ClientNumber { get; set; }
        [RegularExpression(@"[0-9][0-9]")]
        public string? ProjectNumber { get; set; }
        [RegularExpression(@"[0-9][0-9]")]
        public string? AreaNumber { get; set; }
        [RegularExpression(@"[A-Z][A-Z]")]
        [Required]
        public required string Discipline { get; set; }
        [RegularExpression(@"[A-Z][A-Z][A-Z]")]
        [Required]
        public required string DocumentType { get; set; }
        public Guid DepartmentId { get; set; }
        public DeliverableTypeEnum DeliverableTypeId { get; set; }
        [Required]
        public required string InternalDocumentNumber { get; set; }
        public string? ClientDocumentNumber { get; set; }
        [Required]
        public required string DocumentTitle { get; set; }
        public decimal BudgetHours { get; set; }
        public decimal VariationHours { get; set; }
        public decimal TotalHours { get; set; } 
        public decimal TotalCost { get; set; }
        public string BookingCode { get; set; } = string.Empty; 
        public DateTime Created { get; set; }
        public Guid CreatedBy { get; set; }
        public DateTime? Updated { get; set; }
        public Guid? UpdatedBy { get; set; }
        public DateTime? Deleted { get; set; }
        public Guid? DeletedBy { get; set; }

        public virtual DepartmentEntity? Department { get; set; }
        public virtual ProjectEntity? Project { get; set; }
        public virtual ICollection<ProgressEntity> ProgressItems { get; set; } = new List<ProgressEntity>();
    }
}
