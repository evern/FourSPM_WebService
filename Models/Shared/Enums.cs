using System.ComponentModel.DataAnnotations;

namespace FourSPM_WebService.Models.Shared.Enums
{
    public enum ProjectStatus
    {
        [Display(Name = "Tender In Progress")]
        TenderInProgress = 0,

        [Display(Name = "Tender Submitted")]
        TenderSubmitted = 1,

        [Display(Name = "Awarded")]
        Awarded = 2,

        [Display(Name = "Closed")]
        Closed = 3,

        [Display(Name = "Cancelled")]
        Cancelled = 4
    }
}
