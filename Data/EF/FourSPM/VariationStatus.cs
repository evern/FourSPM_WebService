namespace FourSPM_WebService.Data.EF.FourSPM
{
    /// <summary>
    /// Represents the status of a deliverable in relation to variations
    /// </summary>
    public enum VariationStatus
    {
        /// <summary>
        /// Standard deliverable (not related to a variation)
        /// </summary>
        Standard = 0,
        
        /// <summary>
        /// Deliverable that is part of an unapproved variation
        /// </summary>
        UnapprovedVariation = 1,
        
        /// <summary>
        /// Deliverable that is part of an approved variation
        /// </summary>
        ApprovedVariation = 2,
        
        /// <summary>
        /// Deliverable that is cancelled in an unapproved variation
        /// </summary>
        UnapprovedCancellation = 3,
        
        /// <summary>
        /// Deliverable that is cancelled in an approved variation
        /// </summary>
        ApprovedCancellation = 4
    }
}
