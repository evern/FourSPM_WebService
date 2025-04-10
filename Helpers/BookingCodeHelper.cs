using FourSPM_WebService.Data.EF.FourSPM;
using FourSPM_WebService.Data.Interfaces;
using System;
using System.Threading.Tasks;

namespace FourSPM_WebService.Helpers
{
    /// <summary>
    /// Helper class for booking code related operations
    /// </summary>
    public static class BookingCodeHelper
    {
        /// <summary>
        /// Calculates a booking code from project, area, and discipline information
        /// </summary>
        /// <param name="projectRepository">The project repository to use if the project needs to be fetched</param>
        /// <param name="projectGuid">The GUID of the project</param>
        /// <param name="areaNumber">The area number</param>
        /// <param name="discipline">The discipline code</param>
        /// <param name="existingBookingCode">Optional existing booking code to use if provided</param>
        /// <param name="project">Optional project entity if already available</param>
        /// <returns>The calculated booking code or empty string if it cannot be calculated</returns>
        public static async Task<string> CalculateAsync(
            IProjectRepository projectRepository,
            Guid projectGuid,
            string? areaNumber,
            string? discipline,
            string? existingBookingCode = null,
            PROJECT? project = null)
        {
            // If existingBookingCode is provided and not empty, use it
            if (!string.IsNullOrEmpty(existingBookingCode))
            {
                return existingBookingCode;
            }

            // Use provided project or fetch it if not provided
            if (project == null)
            {
                project = await projectRepository.GetByIdAsync(projectGuid);
            }
            
            string clientNumber = project?.Client?.NUMBER ?? string.Empty;
            string projectNumber = project?.PROJECT_NUMBER ?? string.Empty;
            
            // Calculate booking code
            return !string.IsNullOrEmpty(clientNumber) && 
                   !string.IsNullOrEmpty(projectNumber) && 
                   !string.IsNullOrEmpty(areaNumber) && 
                   !string.IsNullOrEmpty(discipline)
                ? $"{clientNumber}-{projectNumber}-{areaNumber}-{discipline}"
                : string.Empty;
        }
    }
}
