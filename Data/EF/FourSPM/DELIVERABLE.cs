using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FourSPM_WebService.Data.EF.FourSPM
{
    [Table("DELIVERABLES")]
    public class DELIVERABLE
    {
        [Key]
        public Guid ID { get; set; }

        public Guid PROJECT_ID { get; set; }

        [Required]
        [StringLength(3)]
        [RegularExpression(@"[0-9][0-9][0-9]", ErrorMessage = "CLIENT_NUMBER must be 3 digits")]
        public required string CLIENT_NUMBER { get; set; }

        [Required]
        [StringLength(2)]
        [RegularExpression(@"[0-9][0-9]", ErrorMessage = "PROJECT_NUMBER must be 2 digits")]
        public required string PROJECT_NUMBER { get; set; }

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

        public Guid DEPARTMENT_ID { get; set; }

        public Guid DELIVERABLE_TYPE_ID { get; set; }

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

        [Required]
        [Column(TypeName = "decimal(10,2)")]
        public decimal TOTAL_HOURS { get; set; }

        [Required]
        [Column(TypeName = "decimal(15,2)")]
        public decimal TOTAL_COST { get; set; }

        [Required]
        [StringLength(50)]
        public required string BOOKING_CODE { get; set; }

        [Required]
        public DateTime CREATED { get; set; }

        [Required]
        public Guid CREATEDBY { get; set; }

        public DateTime? UPDATED { get; set; }

        public Guid? UPDATEDBY { get; set; }

        public DateTime? DELETED { get; set; }

        public Guid? DELETEDBY { get; set; }

        [ForeignKey(nameof(DEPARTMENT_ID))]
        public virtual DEPARTMENT? Department { get; set; }

        [ForeignKey(nameof(DELIVERABLE_TYPE_ID))]
        public virtual DELIVERABLE_TYPE? DeliverableType { get; set; }

        [ForeignKey(nameof(PROJECT_ID))]
        public virtual PROJECT? Project { get; set; }
    }
}
