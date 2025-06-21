using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
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
        public DepartmentEnum DepartmentId { get; set; }
        public DeliverableTypeEnum DeliverableTypeId { get; set; }
        public Guid? DeliverableGateGuid { get; set; }
        [Required]
        public required string InternalDocumentNumber { get; set; }
        public string? ClientDocumentNumber { get; set; }
        [Required]
        public required string DocumentTitle { get; set; }
        public decimal BudgetHours { get; set; }
        public decimal VariationHours { get; set; }
        public decimal TotalHours { get; set; } 
        public decimal TotalCost { get; set; }
        public string? BookingCode { get; set; } = string.Empty;

        #region Progress Calculation Properties
        // These properties are calculated by the backend and not stored in the database

        // Hours earned up to and including the current period
        public decimal CumulativeEarntHours { get; set; }
        // Percentage earned up to and including the current period
        public decimal CumulativeEarntPercentage { get; set; }
        
        // Percentage earned specifically in the current period (difference between cumulative and previous)
        public decimal CurrentPeriodEarntPercentage { get; set; }
        
        // Percentage earned in previous periods (before the current period)
        public decimal PreviousPeriodEarntPercentage { get; set; }
        
        // Hours earned specifically in the current period
        public decimal CurrentPeriodEarntHours { get; set; }
        
        // Total percentage earned across all periods (including future periods)
        public decimal TotalPercentageEarnt { get; set; }
        
        // Total hours earned across all periods
        public decimal TotalEarntHours { get; set; }
        
        // Percentage earned in the next future period (after the current period)
        public decimal FuturePeriodEarntPercentage { get; set; }
        #endregion
        
        public DateTime Created { get; set; }
        public Guid CreatedBy { get; set; }
        public DateTime? Updated { get; set; }
        public Guid? UpdatedBy { get; set; }
        public DateTime? Deleted { get; set; }
        public Guid? DeletedBy { get; set; }

        public virtual ProjectEntity? Project { get; set; }
        public virtual ICollection<ProgressEntity> ProgressItems { get; set; } = new List<ProgressEntity>();
        public virtual DeliverableGateEntity? DeliverableGate { get; set; }
        public virtual VariationEntity? Variation { get; set; }

        // New variation fields
        public VariationStatus VariationStatus { get; set; } = VariationStatus.Standard;
        public Guid? VariationGuid { get; set; }
        public Guid? OriginalDeliverableGuid { get; set; }
        public decimal ApprovedVariationHours { get; set; } = 0;
        
        // UI-ONLY: Not mapped to EF - UI-specific status property (calculated, not stored)
        public string UIStatus { get; set; } = "Original";
        
        // UI-ONLY: Not mapped to EF - Name of the variation this deliverable belongs to (for display)
        public string? VariationName { get; set; }
        
        // UI-ONLY: Not mapped to EF - Custom total hours calculation for variations display only
        public decimal VariationDisplayHours { get; set; }
    }
}
