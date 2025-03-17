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

    public enum DeliverableGate
    {
        [Display(Name = "Started")]
        Started = 0,

        [Display(Name = "Issued for Checking")]
        IssuedForChecking = 1,

        [Display(Name = "Issued for Client Review")]
        IssuedForClientReview = 2,

        [Display(Name = "Issued for Construction/Use")]
        IssuedForConstructionUse = 3
    }
}
