using System;
using System.ComponentModel.DataAnnotations;

namespace FourSPM_WebService.Data.OData.FourSPM
{
    public class DeliverableEntity
    {
        public Guid ID { get; set; }
        public Guid PROJECT_ID { get; set; }
        [RegularExpression(@"[0-9][0-9][0-9]")]
        public required string CLIENT_NUMBER { get; set; }
        [RegularExpression(@"[0-9][0-9]")]
        public required string PROJECT_NUMBER { get; set; }
        [RegularExpression(@"[0-9][0-9]")]
        public string? AREA_NUMBER { get; set; }
        [RegularExpression(@"[A-Z][A-Z]")]
        public required string DISCIPLINE { get; set; }
        [RegularExpression(@"[A-Z][A-Z][A-Z]")]
        public required string DOCUMENT_TYPE { get; set; }
        public Guid DEPARTMENT_ID { get; set; }
        public Guid DELIVERABLE_TYPE_ID { get; set; }
        public required string INTERNAL_DOCUMENT_NUMBER { get; set; }
        public string? CLIENT_DOCUMENT_NUMBER { get; set; }
        public required string DOCUMENT_TITLE { get; set; }
        public decimal BUDGET_HOURS { get; set; }
        public decimal VARIATION_HOURS { get; set; }
        public decimal TOTAL_HOURS { get; set; }
        public decimal TOTAL_COST { get; set; }
        public required string BOOKING_CODE { get; set; }
        public DateTime CREATED { get; set; }
        public Guid CREATEDBY { get; set; }
        public DateTime? UPDATED { get; set; }
        public Guid? UPDATEDBY { get; set; }
        public DateTime? DELETED { get; set; }
        public Guid? DELETEDBY { get; set; }

        public virtual DepartmentEntity? Department { get; set; }
        public virtual DeliverableTypeEntity? DeliverableType { get; set; }
        public virtual ProjectEntity? Project { get; set; }
    }
}
