using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Collections.Generic;

namespace FourSPM_WebService.Data.EF.FourSPM
{
    public enum DeliverableTypeEnum
    {
        Task = 0,
        NonDeliverable = 1,
        DeliverableICR = 2,
        Deliverable = 3
    }

    public enum DepartmentEnum
    {
        Administration = 0,
        Design = 1,
        Engineering = 2,
        Management = 3
    }

    public class DELIVERABLE
    {
        [Key]
        public Guid GUID { get; set; }

        public Guid GUID_PROJECT { get; set; }

        [StringLength(2)]
        [RegularExpression(@"[0-9][0-9]", ErrorMessage = "AREA_NUMBER must be 2 digits")]
        public string? AREA_NUMBER { get; set; }

        [Required]
        [StringLength(2)]
        [RegularExpression(@"[A-Z][A-Z]", ErrorMessage = "DISCIPLINE must be 2 uppercase letters")]
        public required string DISCIPLINE { get; set; }

        [Required]
        [StringLength(3)]
        [RegularExpression(@"[A-Z][A-Z][A-Z]", ErrorMessage = "DOCUMENT_TYPE must be 3 uppercase letters")]
        public required string DOCUMENT_TYPE { get; set; }

        public DepartmentEnum DEPARTMENT_ID { get; set; }

        public DeliverableTypeEnum DELIVERABLE_TYPE_ID { get; set; }

        public Guid? GUID_DELIVERABLE_GATE { get; set; }

        [Required]
        [StringLength(50)]
        public required string INTERNAL_DOCUMENT_NUMBER { get; set; }

        [StringLength(100)]
        public string? CLIENT_DOCUMENT_NUMBER { get; set; }

        [Required]
        [StringLength(255)]
        public required string DOCUMENT_TITLE { get; set; }

        [Required]
        [Column(TypeName = "decimal(10,2)")]
        public decimal BUDGET_HOURS { get; set; }

        [Required]
        [Column(TypeName = "decimal(10,2)")]
        public decimal VARIATION_HOURS { get; set; }

        [StringLength(50)]
        public string? BOOKING_CODE { get; set; }

        [Required]
        [Column(TypeName = "decimal(15,2)")]
        public decimal TOTAL_COST { get; set; }

        [Required]
        public DateTime CREATED { get; set; }

        [Required]
        public Guid CREATEDBY { get; set; }

        public DateTime? UPDATED { get; set; }

        public Guid? UPDATEDBY { get; set; }

        public DateTime? DELETED { get; set; }

        public Guid? DELETEDBY { get; set; }

        [ForeignKey(nameof(GUID_PROJECT))]
        public virtual PROJECT? Project { get; set; }
        
        [ForeignKey(nameof(GUID_DELIVERABLE_GATE))]
        public virtual DELIVERABLE_GATE? DeliverableGate { get; set; }

        public virtual ICollection<PROGRESS> ProgressItems { get; set; } = new List<PROGRESS>();
    }
}
