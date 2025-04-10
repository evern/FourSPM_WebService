using FourSPM_WebService.Data.EF.FourSPM;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace FourSPM_WebService.Utilities
{
    /// <summary>
    /// Utility class for generating and managing document numbers
    /// </summary>
    public static class DocumentNumberGenerator
    {
        /// <summary>
        /// Generate a new sequence number based on existing document numbers with the same base format
        /// </summary>
        /// <param name="context">Database context</param>
        /// <param name="baseFormat">Base format of the document number (everything before the sequence number)</param>
        /// <param name="projectGuid">Project GUID to filter deliverables</param>
        /// <param name="excludeDeliverableGuid">Optional GUID to exclude from consideration</param>
        /// <returns>A formatted 3-digit sequence number as string</returns>
        public static async Task<string> GenerateSequenceNumberAsync(FourSPMContext context, string baseFormat, Guid projectGuid, Guid? excludeDeliverableGuid = null)
        {
            // Find the highest sequence number for documents with this format
            var query = context.DELIVERABLEs
                .Where(d => d.GUID_PROJECT == projectGuid && 
                       d.DELETED == null && 
                       d.INTERNAL_DOCUMENT_NUMBER.StartsWith(baseFormat) &&
                       !d.INTERNAL_DOCUMENT_NUMBER.EndsWith("-XXX"));

            // Exclude specific deliverable if requested
            if (excludeDeliverableGuid.HasValue && excludeDeliverableGuid.Value != Guid.Empty)
            {
                query = query.Where(d => d.GUID != excludeDeliverableGuid.Value);
            }

            var existingDeliverables = await query.ToListAsync();
            
            int nextSequence = 1;
            
            if (existingDeliverables.Any())
            {
                foreach (var deliverable in existingDeliverables)
                {
                    if (!string.IsNullOrEmpty(deliverable.INTERNAL_DOCUMENT_NUMBER))
                    {
                        var parts = deliverable.INTERNAL_DOCUMENT_NUMBER.Split('-');
                        if (parts.Length > 0)
                        {
                            var lastPart = parts[parts.Length - 1];
                            if (int.TryParse(lastPart, out int seq) && seq >= nextSequence)
                            {
                                nextSequence = seq + 1;
                            }
                        }
                    }
                }
            }
            
            // Format sequence as 3 digits (001, 002, etc.)
            return nextSequence.ToString().PadLeft(3, '0');
        }

        /// <summary>
        /// Creates a base format for document numbers based on project and deliverable information
        /// </summary>
        /// <param name="clientNumber">Client number</param>
        /// <param name="projectNumber">Project number</param>
        /// <param name="deliverableTypeId">Type of deliverable</param>
        /// <param name="areaNumber">Area number (required for Deliverable type)</param>
        /// <param name="discipline">Discipline code</param>
        /// <param name="documentType">Document type code</param>
        /// <returns>Base format string for document numbers</returns>
        public static string CreateBaseFormat(string clientNumber, string projectNumber, string deliverableTypeId, string areaNumber, string discipline, string documentType)
        {
            // Determine the format based on deliverable type
            // For "Deliverable" type: Client-Project-Area-Discipline-DocumentType-SequentialNumber
            // For other types: Client-Project-Discipline-DocumentType-SequentialNumber
            string baseFormat;
            if (deliverableTypeId == "Deliverable")
            {
                if (string.IsNullOrEmpty(areaNumber))
                {
                    throw new ArgumentException("Area number is required for Deliverable type");
                }
                baseFormat = $"{clientNumber}-{projectNumber}-{areaNumber}";
            }
            else
            {
                baseFormat = $"{clientNumber}-{projectNumber}";
            }
            
            // Add discipline and document type if provided
            if (!string.IsNullOrEmpty(discipline))
            {
                baseFormat += $"-{discipline}";
            }
            
            if (!string.IsNullOrEmpty(documentType))
            {
                baseFormat += $"-{documentType}";
            }

            return baseFormat;
        }

        /// <summary>
        /// Replace the numeric suffix of a document number with "XXX" for variation documents
        /// </summary>
        /// <param name="documentNumber">Original document number</param>
        /// <returns>Document number with XXX suffix</returns>
        public static string ReplaceWithXXXSuffix(string documentNumber)
        {
            return System.Text.RegularExpressions.Regex.Replace(documentNumber, "(-\\d+)$", "-XXX");
        }

        /// <summary>
        /// Get base format from a document number with XXX suffix
        /// </summary>
        /// <param name="documentNumber">Document number ending with -XXX</param>
        /// <returns>Base format (everything before -XXX)</returns>
        public static string GetBaseFormatFromXXX(string documentNumber)
        {
            if (string.IsNullOrEmpty(documentNumber) || !documentNumber.EndsWith("-XXX"))
            {
                throw new ArgumentException("Document number must end with -XXX");
            }

            return documentNumber.Substring(0, documentNumber.Length - 4);
        }

        /// <summary>
        /// Determine the next sequence number based on existing deliverables
        /// </summary>
        /// <param name="existingDeliverables">List of existing deliverables with the same base format</param>
        /// <param name="excludeDeliverableGuid">Optional GUID to exclude from consideration</param>
        /// <returns>A formatted 3-digit sequence number as string</returns>
        public static string DetermineNextSequence(
            IEnumerable<DELIVERABLE> existingDeliverables,
            Guid? excludeDeliverableGuid = null)
        {
            // Start with sequence 1
            int nextSequence = 1;
            
            // Filter out excluded deliverable if needed
            if (excludeDeliverableGuid.HasValue && excludeDeliverableGuid.Value != Guid.Empty)
            {
                existingDeliverables = existingDeliverables.Where(d => d.GUID != excludeDeliverableGuid.Value);
            }
            
            // Find the highest sequence number
            if (existingDeliverables.Any())
            {
                foreach (var deliverable in existingDeliverables)
                {
                    if (!string.IsNullOrEmpty(deliverable.INTERNAL_DOCUMENT_NUMBER))
                    {
                        var parts = deliverable.INTERNAL_DOCUMENT_NUMBER.Split('-');
                        if (parts.Length > 0)
                        {
                            var lastPart = parts[parts.Length - 1];
                            if (int.TryParse(lastPart, out int seq) && seq >= nextSequence)
                            {
                                nextSequence = seq + 1;
                            }
                        }
                    }
                }
            }
            
            // Format sequence as 3 digits (001, 002, etc.)
            return nextSequence.ToString().PadLeft(3, '0');
        }


        /// <summary>
        /// Generate a complete document number based on project and deliverable details
        /// </summary>
        /// <param name="context">Database context</param>
        /// <param name="project">Project entity with client information</param>
        /// <param name="deliverableTypeId">Type of deliverable</param>
        /// <param name="areaNumber">Area number (required for Deliverable type)</param>
        /// <param name="discipline">Discipline code</param>
        /// <param name="documentType">Document type code</param>
        /// <param name="projectGuid">Project GUID</param>
        /// <param name="excludeDeliverableGuid">Optional GUID to exclude from consideration</param>
        /// <returns>A complete document number</returns>
        /// <exception cref="ArgumentException">Thrown when required fields are missing</exception>
        public static async Task<string> GenerateCompleteDocumentNumberAsync(
            FourSPMContext context,
            PROJECT project,
            string deliverableTypeId,
            string areaNumber,
            string discipline,
            string documentType,
            Guid projectGuid,
            Guid? excludeDeliverableGuid = null)
        {
            // Validate project and required fields
            if (project == null)
            {
                throw new ArgumentException($"Project with ID {projectGuid} not found");
            }

            // Get client and project numbers
            string clientNumber = project.Client?.NUMBER ?? string.Empty;
            string projectNumber = project.PROJECT_NUMBER ?? string.Empty;
            
            if (string.IsNullOrEmpty(clientNumber) || string.IsNullOrEmpty(projectNumber))
            {
                throw new ArgumentException("Client number or project number is missing from the project");
            }
            
            // Create the base format for the document number
            string baseFormat = CreateBaseFormat(
                clientNumber, 
                projectNumber, 
                deliverableTypeId, 
                areaNumber, 
                discipline, 
                documentType);
            
            // Generate the sequence number
            string sequenceNumber = await GenerateSequenceNumberAsync(
                context, 
                baseFormat, 
                projectGuid, 
                excludeDeliverableGuid);
            
            // Build the final suggested number
            return $"{baseFormat}-{sequenceNumber}";
        }
    }
}
