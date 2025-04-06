using System;
using System.ComponentModel.DataAnnotations;

namespace FourSPM_WebService.Data.OData.FourSPM
{
    /// <summary>
    /// Entity used for creating or updating a variation copy of a deliverable
    /// </summary>
    public class DeliverableVariationEntity
    {
        /// <summary>
        /// The GUID of the original deliverable being modified by the variation
        /// </summary>
        [Required]
        public Guid OriginalDeliverableGuid { get; set; }
        
        /// <summary>
        /// The GUID of the variation this deliverable is associated with
        /// </summary>
        [Required]
        public Guid VariationGuid { get; set; }
        
        /// <summary>
        /// The hours to be added/removed by this variation
        /// </summary>
        [Required]
        public decimal VariationHours { get; set; }
        
        /// <summary>
        /// Whether this is a cancellation of the original deliverable
        /// </summary>
        public bool IsCancellation { get; set; } = false;
        
        /// <summary>
        /// Optional document title override for the variation deliverable
        /// </summary>
        public string? DocumentTitle { get; set; }
        
        /// <summary>
        /// Optional document type override for the variation deliverable
        /// </summary>
        public string? DocumentType { get; set; }
        
        /// <summary>
        /// Optional client document number override for the variation deliverable
        /// </summary>
        public string? ClientDocumentNumber { get; set; }
    }
}
