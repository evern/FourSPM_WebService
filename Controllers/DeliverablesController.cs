using FourSPM_WebService.Data.EF.FourSPM;
using FourSPM_WebService.Data.Interfaces;
using FourSPM_WebService.Data.OData.FourSPM;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OData;
using Microsoft.AspNetCore.OData.Query;
using Microsoft.AspNetCore.OData.Routing;
using Microsoft.AspNetCore.OData.Routing.Attributes;
using Microsoft.AspNetCore.OData.Formatter;
using Microsoft.AspNetCore.OData.Deltas;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Linq.Dynamic.Core;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using System.ComponentModel.DataAnnotations;
using FourSPM_WebService.Models.Shared;

// For access to the VariationStatus enum
using static FourSPM_WebService.Data.EF.FourSPM.VariationStatus;

namespace FourSPM_WebService.Controllers
{
    [Authorize]
    [ODataRouteComponent("odata/v1")]
    public class DeliverablesController : FourSPMODataController
    {
        private readonly IDeliverableRepository _repository;
        private readonly IProjectRepository _projectRepository;
        private readonly ILogger<DeliverablesController> _logger;

        public DeliverablesController(
            IDeliverableRepository repository, 
            IProjectRepository projectRepository, 
            ILogger<DeliverablesController> logger)
        {
            _repository = repository;
            _projectRepository = projectRepository;
            _logger = logger;
        }

        [EnableQuery]
        public async Task<IActionResult> Get()
        {
            var deliverables = await _repository.GetAllAsync();
            var entities = deliverables.Select(d => MapToEntity(d));
            return Ok(entities);
        }

        [EnableQuery]
        public async Task<IActionResult> Get([FromRoute] Guid key)
        {
            var deliverable = await _repository.GetByIdAsync(key);
            if (deliverable == null)
                return NotFound();

            return Ok(MapToEntity(deliverable));
        }

        public async Task<IActionResult> Post([FromBody] DeliverableEntity entity)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var deliverable = new DELIVERABLE
            {
                GUID = entity.Guid,
                GUID_PROJECT = entity.ProjectGuid,
                AREA_NUMBER = entity.AreaNumber,
                DISCIPLINE = entity.Discipline,
                DOCUMENT_TYPE = entity.DocumentType,
                DEPARTMENT_ID = entity.DepartmentId,
                DELIVERABLE_TYPE_ID = entity.DeliverableTypeId,
                GUID_DELIVERABLE_GATE = entity.DeliverableGateGuid,
                INTERNAL_DOCUMENT_NUMBER = entity.InternalDocumentNumber,
                CLIENT_DOCUMENT_NUMBER = entity.ClientDocumentNumber,
                DOCUMENT_TITLE = entity.DocumentTitle,
                BUDGET_HOURS = entity.BudgetHours,
                VARIATION_HOURS = entity.VariationHours,
                TOTAL_COST = entity.TotalCost,
                
                // Set the new variation fields
                VARIATION_STATUS = (int)entity.VariationStatus, // Cast enum to int for DB
                GUID_VARIATION = entity.VariationGuid,
                GUID_ORIGINAL_DELIVERABLE = entity.OriginalDeliverableGuid,
                APPROVED_VARIATION_HOURS = entity.ApprovedVariationHours
            };

            // Calculate booking code if not provided, otherwise use the one from entity
            deliverable.BOOKING_CODE = await CalculateBookingCodeAsync(entity.ProjectGuid, entity.AreaNumber, entity.Discipline, entity.BookingCode);

            var result = await _repository.CreateAsync(deliverable);
            return Created(MapToEntity(result));
        }

        public async Task<IActionResult> Put([FromRoute] Guid key, [FromBody] DeliverableEntity entity)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            if (key != entity.Guid)
                return BadRequest("The ID in the URL must match the ID in the request body");

            try
            {
                var deliverable = new DELIVERABLE
                {
                    GUID = entity.Guid,
                    GUID_PROJECT = entity.ProjectGuid,
                    AREA_NUMBER = entity.AreaNumber,
                    DISCIPLINE = entity.Discipline,
                    DOCUMENT_TYPE = entity.DocumentType,
                    DEPARTMENT_ID = entity.DepartmentId,
                    DELIVERABLE_TYPE_ID = entity.DeliverableTypeId,
                    GUID_DELIVERABLE_GATE = entity.DeliverableGateGuid,
                    INTERNAL_DOCUMENT_NUMBER = entity.InternalDocumentNumber,
                    CLIENT_DOCUMENT_NUMBER = entity.ClientDocumentNumber,
                    DOCUMENT_TITLE = entity.DocumentTitle,
                    BUDGET_HOURS = entity.BudgetHours,
                    VARIATION_HOURS = entity.VariationHours,
                    TOTAL_COST = entity.TotalCost,
                    
                    // Set the new variation fields
                    VARIATION_STATUS = (int)entity.VariationStatus, // Cast enum to int for DB
                    GUID_VARIATION = entity.VariationGuid,
                    GUID_ORIGINAL_DELIVERABLE = entity.OriginalDeliverableGuid,
                    APPROVED_VARIATION_HOURS = entity.ApprovedVariationHours
                };

                // Calculate booking code if not provided, otherwise use the one from entity
                deliverable.BOOKING_CODE = await CalculateBookingCodeAsync(entity.ProjectGuid, entity.AreaNumber, entity.Discipline, entity.BookingCode);

                var result = await _repository.UpdateAsync(deliverable);
                return Updated(MapToEntity(result));
            }
            catch (KeyNotFoundException)
            {
                return NotFound();
            }
        }

        public async Task<IActionResult> Delete([FromRoute] Guid key, [FromBody] Guid deletedBy)
        {
            var result = await _repository.DeleteAsync(key, deletedBy);
            return result ? NoContent() : NotFound();
        }

        /// <summary>
        /// Partially updates a deliverable
        /// </summary>
        /// <param name="key">The GUID of the deliverable to update</param>
        /// <param name="delta">The deliverable properties to update</param>
        /// <returns>The updated deliverable</returns>
        public async Task<IActionResult> Patch([FromODataUri] Guid key, [FromBody] Delta<DeliverableEntity> delta)
        {
            try
            {
                _logger?.LogInformation($"Received PATCH request for deliverable {key}");

                if (key == Guid.Empty)
                {
                    return BadRequest("Invalid GUID - The deliverable ID cannot be empty");
                }

                if (delta == null)
                {
                    _logger?.LogWarning($"Update data is null for deliverable {key}");
                    return BadRequest("Update data cannot be null. The request body must contain valid properties to update.");
                }

                // Get the existing deliverable
                var existingDeliverable = await _repository.GetByIdAsync(key);
                if (existingDeliverable == null)
                {
                    return NotFound("Deliverable with ID " + key + " was not found");
                }

                // Create a copy of the entity to track changes
                var updatedEntity = MapToEntity(existingDeliverable);
                delta.CopyChangedValues(updatedEntity);

                // Map back to DELIVERABLE entity
                existingDeliverable.GUID_PROJECT = updatedEntity.ProjectGuid;
                existingDeliverable.AREA_NUMBER = updatedEntity.AreaNumber;
                existingDeliverable.DISCIPLINE = updatedEntity.Discipline;
                existingDeliverable.DOCUMENT_TYPE = updatedEntity.DocumentType;
                existingDeliverable.DEPARTMENT_ID = updatedEntity.DepartmentId;
                existingDeliverable.DELIVERABLE_TYPE_ID = updatedEntity.DeliverableTypeId;
                existingDeliverable.GUID_DELIVERABLE_GATE = updatedEntity.DeliverableGateGuid;
                existingDeliverable.INTERNAL_DOCUMENT_NUMBER = updatedEntity.InternalDocumentNumber;
                existingDeliverable.CLIENT_DOCUMENT_NUMBER = updatedEntity.ClientDocumentNumber;
                existingDeliverable.DOCUMENT_TITLE = updatedEntity.DocumentTitle;
                existingDeliverable.BUDGET_HOURS = updatedEntity.BudgetHours;
                existingDeliverable.VARIATION_HOURS = updatedEntity.VariationHours;
                existingDeliverable.TOTAL_COST = updatedEntity.TotalCost;

                // Check if any fields that affect the booking code have changed
                bool shouldUpdateBookingCode = delta.GetChangedPropertyNames().Any(p => 
                    p == "AreaNumber" || p == "Discipline" || p == "ProjectGuid");
                
                if (shouldUpdateBookingCode)
                {
                    // Pass the existing Project navigation property to avoid an extra query
                    existingDeliverable.BOOKING_CODE = await CalculateBookingCodeAsync(
                        updatedEntity.ProjectGuid,
                        updatedEntity.AreaNumber,
                        updatedEntity.Discipline,
                        null,  // No existing booking code to preserve
                        existingDeliverable.Project  // Use navigation property
                    );
                }
                else if (delta.GetChangedPropertyNames().Contains("BookingCode"))
                {
                    // If booking code was explicitly included in the update, use that value
                    existingDeliverable.BOOKING_CODE = updatedEntity.BookingCode;
                }

                var result = await _repository.UpdateAsync(existingDeliverable);
                return Updated(MapToEntity(result));
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error updating deliverable");
                return StatusCode(500, "Internal Server Error - " + ex.Message);
            }
        }

        // Helper method to handle booking code calculation
        private async Task<string> CalculateBookingCodeAsync(Guid projectGuid, string? areaNumber, string? discipline, string? existingBookingCode = null, PROJECT? project = null)
        {
            // If existingBookingCode is provided and not empty, use it
            if (!string.IsNullOrEmpty(existingBookingCode))
            {
                return existingBookingCode;
            }

            // Use provided project or fetch it if not provided
            if (project == null)
            {
                project = await _projectRepository.GetByIdAsync(projectGuid);
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

        // Add a new action to suggest an internal document number
        [HttpGet("/odata/v1/Deliverables/SuggestInternalDocumentNumber")]
        public async Task<IActionResult> SuggestInternalDocumentNumber([FromQuery] Guid projectGuid, [FromQuery] string areaNumber, [FromQuery] string discipline, [FromQuery] string documentType, [FromQuery] string deliverableTypeId, [FromQuery] Guid? excludeDeliverableGuid = null)
        {
            try
            {
                _logger?.LogInformation($"Generating suggested internal document number for project {projectGuid}");
                
                // Get the project details to get client and project numbers
                var project = await _projectRepository.GetProjectWithClientAsync(projectGuid);
                
                if (project == null)
                {
                    return NotFound($"Project with ID {projectGuid} not found");
                }

                // Get client and project numbers
                string clientNumber = project.Client?.NUMBER ?? string.Empty;
                string projectNumber = project.PROJECT_NUMBER ?? string.Empty;
                
                if (string.IsNullOrEmpty(clientNumber) || string.IsNullOrEmpty(projectNumber))
                {
                    return BadRequest("Client number or project number is missing from the project");
                }
                
                // Determine the format based on deliverable type
                // For "Deliverable" type: Client-Project-Area-Discipline-DocumentType-SequentialNumber
                // For other types: Client-Project-Discipline-DocumentType-SequentialNumber
                string baseFormat;
                if (deliverableTypeId == "Deliverable")
                {
                    if (string.IsNullOrEmpty(areaNumber))
                    {
                        return BadRequest("Area number is required for Deliverable type");
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
                
                // Find the highest sequence number for documents with this format
                var existingDeliverables = await _repository.GetDeliverablesByNumberPatternAsync(projectGuid, baseFormat);
                
                // Filter out the excluded deliverable if one was specified
                if (excludeDeliverableGuid.HasValue && excludeDeliverableGuid.Value != Guid.Empty)
                {
                    existingDeliverables = existingDeliverables.Where(d => d.GUID != excludeDeliverableGuid.Value).ToList();
                }
                
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
                string sequenceNumber = nextSequence.ToString().PadLeft(3, '0');
                
                // Build the final suggested number
                string suggestedNumber = $"{baseFormat}-{sequenceNumber}";
                
                return Ok(new { suggestedNumber });
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error generating suggested internal document number");
                return StatusCode(500, "Internal Server Error - " + ex.Message);
            }
        }

        // Endpoint to get deliverables with progress percentages for a specific project and period
        // This follows OData conventions, ensuring proper serialization of enum values as strings
        [HttpGet]
        public async Task<IActionResult> GetWithProgressPercentages([FromODataUri] Guid projectGuid, int period)
        {
            try
            {
                _logger?.LogInformation($"Getting deliverables with progress percentages for project {projectGuid}, period {period}");

                // Get deliverables for the specific project and period
                var deliverables = await _repository.GetByProjectIdAndPeriodAsync(projectGuid, period);

                if (deliverables == null)
                {
                    _logger?.LogWarning($"No deliverables found for project {projectGuid}, period {period}");
                    return Ok(new ODataResponse<DeliverableEntity>
                    {
                        Value = Enumerable.Empty<DeliverableEntity>(),
                        Count = 0
                    });
                }

                // Map entities and apply CalculateProgressPercentages to each one
                var entities = deliverables.Select(d =>
                {
                    var entity = MapToEntity(d);
                    CalculateProgressPercentages(entity, period);
                    return entity;
                }).ToList();
                
                // Create response with proper count
                var response = new ODataResponse<DeliverableEntity>
                {
                    Value = entities,
                    Count = entities.Count
                };
                
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error getting deliverables with progress percentages");
                return StatusCode(500, "Internal Server Error - " + ex.Message);
            }
        }
        
        private static void CalculateProgressPercentages(DeliverableEntity entity, int currentPeriod)
        {
            // Default values for all percentages and hours
            entity.PreviousPeriodEarntPercentage = 0;
            entity.CurrentPeriodEarntPercentage = 0;
            entity.FuturePeriodEarntPercentage = 0;
            entity.CumulativeEarntPercentage = 0;
            entity.TotalPercentageEarnt = 0;
            entity.CurrentPeriodEarntHours = 0;
            entity.TotalEarntHours = 0;
            
            // If no progress items or no hours, we can't calculate percentages
            if (entity.ProgressItems == null || !entity.ProgressItems.Any() || entity.TotalHours <= 0)
            {
                return;
            }

            var validProgressItems = entity.ProgressItems.Where(item => item.Deleted == null).ToList();
            
            // Calculate cumulative percentage earned up to the current period
            var currentPeriodItems = validProgressItems
                .Where(item => item.Period <= currentPeriod)
                .ToList();
                
            if (currentPeriodItems.Any())
            {
                decimal currentPeriodUnits = currentPeriodItems.Sum(item => item.Units);
                entity.CumulativeEarntPercentage = currentPeriodUnits / entity.TotalHours;
            }
            
            // Calculate previous period earned percentage
            var previousPeriodItems = validProgressItems
                .Where(item => item.Period < currentPeriod)
                .ToList();
            
            if (previousPeriodItems.Any())
            {
                decimal previousPeriodUnits = previousPeriodItems.Sum(item => item.Units);
                entity.PreviousPeriodEarntPercentage = previousPeriodUnits / entity.TotalHours;
            }
            
            // Calculate current period percentage (the difference)
            decimal currentPeriodPercentage = Math.Max(0, entity.CumulativeEarntPercentage - entity.PreviousPeriodEarntPercentage);
            entity.CurrentPeriodEarntPercentage = currentPeriodPercentage;
            
            // Calculate earned hours for the current period only
            entity.CurrentPeriodEarntHours = entity.TotalHours * currentPeriodPercentage;
            
            // Calculate total percentage earned (across all periods)
            decimal totalUnits = validProgressItems.Sum(item => item.Units);
            entity.TotalPercentageEarnt = totalUnits / entity.TotalHours;
            entity.TotalEarntHours = entity.TotalHours * entity.TotalPercentageEarnt;
            
            // Calculate future period earned percentage
            var futurePeriodItems = validProgressItems
                .Where(item => item.Period > currentPeriod)
                .ToList();
            
            if (futurePeriodItems.Any())
            {
                // Get the earliest future period
                var minFuturePeriod = futurePeriodItems.Min(item => item.Period);
                var futurePeriodItem = futurePeriodItems.FirstOrDefault(item => item.Period == minFuturePeriod);
                
                if (futurePeriodItem != null)
                {
                    entity.FuturePeriodEarntPercentage = futurePeriodItem.Units / entity.TotalHours;
                }
            }
        }

        private static DeliverableEntity MapToEntity(DELIVERABLE deliverable)
        {
            return MapToEntity(deliverable, 0); // Use 0 as default period when not calculating percentages
        }

        /// <summary>
        /// Sets the UIStatus property of a deliverable entity based on its variation properties
        /// </summary>
        private static void SetUIStatus(DeliverableEntity entity)
        {
            // Calculate and set UIStatus based on variation properties
            if (entity.VariationStatus == VariationStatus.UnapprovedCancellation || 
                entity.VariationStatus == VariationStatus.ApprovedCancellation) {
                entity.UIStatus = "Cancel";
            } else if (entity.VariationGuid.HasValue && 
                     ((entity.OriginalDeliverableGuid.HasValue && entity.Guid == entity.OriginalDeliverableGuid) || 
                     !entity.OriginalDeliverableGuid.HasValue)) {
                // New deliverable created in the variation - either:
                // 1. With self-referencing originalDeliverableGuid (guid == originalDeliverableGuid)
                // 2. Legacy case with no originalDeliverableGuid
                entity.UIStatus = "Add";
            } else if (entity.VariationGuid.HasValue && entity.OriginalDeliverableGuid.HasValue) {
                // Modified deliverable (has both variationGuid and originalDeliverableGuid pointing to different records)
                entity.UIStatus = "Edit";
            } else {
                entity.UIStatus = "Original";
            }
        }
        
        private static DeliverableEntity MapToEntity(DELIVERABLE deliverable, int period)
        {
            // Extract client number and project number from the Project entity if available
            string clientNumber = deliverable.Project?.Client?.NUMBER ?? string.Empty;
            string projectNumber = deliverable.Project?.PROJECT_NUMBER ?? string.Empty;
            
            // Always use the database-stored value for internal document number
            string internalDocumentNumber = deliverable.INTERNAL_DOCUMENT_NUMBER;
            
            decimal totalHours = deliverable.BUDGET_HOURS + deliverable.VARIATION_HOURS;
            
            var validProgressItems = deliverable.ProgressItems
                .Where(p => p.DELETED == null)
                .ToList();
            
            var entity = new DeliverableEntity
            {
                Guid = deliverable.GUID,
                ProjectGuid = deliverable.GUID_PROJECT,
                ClientNumber = clientNumber,
                ProjectNumber = projectNumber,
                AreaNumber = deliverable.AREA_NUMBER,
                Discipline = deliverable.DISCIPLINE,
                DocumentType = deliverable.DOCUMENT_TYPE,
                DepartmentId = deliverable.DEPARTMENT_ID,
                DeliverableTypeId = deliverable.DELIVERABLE_TYPE_ID,
                DeliverableGateGuid = deliverable.GUID_DELIVERABLE_GATE,
                InternalDocumentNumber = internalDocumentNumber,
                ClientDocumentNumber = deliverable.CLIENT_DOCUMENT_NUMBER,
                DocumentTitle = deliverable.DOCUMENT_TITLE,
                BudgetHours = deliverable.BUDGET_HOURS,
                VariationHours = deliverable.VARIATION_HOURS,
                TotalHours = totalHours,
                TotalCost = deliverable.TOTAL_COST,
                BookingCode = deliverable.BOOKING_CODE,
                Created = deliverable.CREATED,
                CreatedBy = deliverable.CREATEDBY,
                Updated = deliverable.UPDATED,
                UpdatedBy = deliverable.UPDATEDBY,
                Deleted = deliverable.DELETED,
                DeletedBy = deliverable.DELETEDBY,
                
                // Map the new variation fields
                VariationStatus = (VariationStatus)deliverable.VARIATION_STATUS, // Cast int to enum
                VariationGuid = deliverable.GUID_VARIATION,
                OriginalDeliverableGuid = deliverable.GUID_ORIGINAL_DELIVERABLE,
                ApprovedVariationHours = deliverable.APPROVED_VARIATION_HOURS,
                Project = deliverable.Project != null ? new ProjectEntity
                {
                    Guid = deliverable.Project.GUID,
                    ClientGuid = deliverable.Project.GUID_CLIENT,
                    ProjectNumber = deliverable.Project.PROJECT_NUMBER,
                    Name = deliverable.Project.NAME,
                    Client = deliverable.Project.Client != null ? new ClientEntity {
                        Guid = deliverable.Project.Client.GUID,
                        Number = deliverable.Project.Client.NUMBER,
                        Description = deliverable.Project.Client.DESCRIPTION
                    } : null
                } : null,
                ProgressItems = validProgressItems.Select(p => new ProgressEntity
                {
                    Guid = p.GUID,
                    DeliverableGuid = p.GUID_DELIVERABLE,
                    Period = p.PERIOD,
                    Units = p.UNITS,
                    Created = p.CREATED,
                    CreatedBy = p.CREATEDBY,
                    Updated = p.UPDATED,
                    UpdatedBy = p.UPDATEDBY,
                    Deleted = p.DELETED,
                    DeletedBy = p.DELETEDBY
                }).ToList(),
                DeliverableGate = deliverable.DeliverableGate != null ? new DeliverableGateEntity
                {
                    Guid = deliverable.DeliverableGate.GUID,
                    Name = deliverable.DeliverableGate.NAME,
                    MaxPercentage = deliverable.DeliverableGate.MAX_PERCENTAGE,
                    AutoPercentage = deliverable.DeliverableGate.AUTO_PERCENTAGE
                } : null
            };
            
            CalculateProgressPercentages(entity, period);
            
            return entity;
        }
        /// <summary>
        /// Gets all deliverables for a specific variation
        /// </summary>
        [HttpGet("odata/v1/Deliverables/ByVariation/{variationId}")]
        public async Task<IActionResult> GetByVariation(Guid variationId)
        {
            try
            {
                _logger?.LogInformation($"Getting deliverables for variation {variationId}");
                
                var deliverables = await _repository.GetByVariationIdAsync(variationId);
                if (deliverables == null || !deliverables.Any())
                {
                    return Ok(new List<DeliverableEntity>());
                }
                
                var entities = deliverables.Select(d => {
                    var entity = MapToEntity(d);
                    SetUIStatus(entity);
                    return entity;
                });
                return Ok(entities);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error getting deliverables for variation");
                return StatusCode(500, $"Error: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Adds or updates a variation copy of an existing deliverable
        /// </summary>
        [HttpPost("odata/v1/Deliverables/AddOrUpdateVariation")]
        public async Task<IActionResult> AddOrUpdateVariation([FromBody] DeliverableEntity entity)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                if (!entity.OriginalDeliverableGuid.HasValue || !entity.VariationGuid.HasValue)
                {
                    return BadRequest("OriginalDeliverableGuid and VariationGuid are required");
                }

                _logger?.LogInformation($"Received AddOrUpdateVariation request for variation {entity.VariationGuid}, original deliverable {entity.OriginalDeliverableGuid}");
                
                // Get the original deliverable to create a copy from or update an existing copy
                var originalDeliverable = await _repository.GetByIdAsync(entity.OriginalDeliverableGuid.Value);
                if (originalDeliverable == null)
                {
                    return NotFound($"Original deliverable with ID {entity.OriginalDeliverableGuid} not found");
                }
                
                // Check if a variation copy already exists for this deliverable and variation
                var existingCopy = await _repository.GetVariationCopyAsync(
                    entity.OriginalDeliverableGuid.Value, 
                    entity.VariationGuid.Value);
                
                if (existingCopy != null)
                {
                    // Update existing variation copy
                    _logger?.LogInformation($"Updating existing variation copy {existingCopy.GUID}");
                    
                    // Only update variation-specific fields
                    existingCopy.VARIATION_HOURS = entity.VariationHours;
                    
                    // Optional overrides if provided
                    if (!string.IsNullOrWhiteSpace(entity.DocumentTitle))
                    {
                        existingCopy.DOCUMENT_TITLE = entity.DocumentTitle;
                    }
                    
                    if (!string.IsNullOrWhiteSpace(entity.DocumentType))
                    {
                        existingCopy.DOCUMENT_TYPE = entity.DocumentType;
                    }
                    
                    if (!string.IsNullOrWhiteSpace(entity.ClientDocumentNumber))
                    {
                        existingCopy.CLIENT_DOCUMENT_NUMBER = entity.ClientDocumentNumber;
                    }
                    
                    // Always recalculate the booking code to ensure consistency
                    // This is important for deliverables created directly in variations
                    // where users might update fields that affect the booking code
                    existingCopy.BOOKING_CODE = await CalculateBookingCodeAsync(
                        existingCopy.GUID_PROJECT, 
                        existingCopy.AREA_NUMBER, 
                        existingCopy.DISCIPLINE, 
                        existingCopy.BOOKING_CODE);
                    
                    // Check if this is a cancellation based on the VariationStatus property
                    if (entity.VariationStatus == VariationStatus.UnapprovedCancellation || 
                        entity.VariationStatus == VariationStatus.ApprovedCancellation)
                    {
                        existingCopy.VARIATION_STATUS = (int)VariationStatus.UnapprovedCancellation;
                    }
                    
                    var result = await _repository.UpdateAsync(existingCopy);
                    var resultEntity = MapToEntity(result);
                    SetUIStatus(resultEntity);
                    return Ok(resultEntity);
                }
                else
                {
                    // Create new variation copy
                    _logger?.LogInformation($"Creating new variation copy for deliverable {entity.OriginalDeliverableGuid}");
                    
                    // Determine the variation status
                    int status = (entity.VariationStatus == VariationStatus.UnapprovedCancellation || 
                                 entity.VariationStatus == VariationStatus.ApprovedCancellation) ? 
                        (int)VariationStatus.UnapprovedCancellation : 
                        (int)VariationStatus.UnapprovedVariation;
                    
                    var newCopy = await _repository.CreateVariationCopyAsync(
                        originalDeliverable, 
                        entity.VariationGuid.Value, 
                        status);
                    
                    // Apply optional overrides if provided
                    if (!string.IsNullOrWhiteSpace(entity.DocumentTitle))
                    {
                        newCopy.DOCUMENT_TITLE = entity.DocumentTitle;
                    }
                    
                    if (!string.IsNullOrWhiteSpace(entity.DocumentType))
                    {
                        newCopy.DOCUMENT_TYPE = entity.DocumentType;
                    }
                    
                    if (!string.IsNullOrWhiteSpace(entity.ClientDocumentNumber))
                    {
                        newCopy.CLIENT_DOCUMENT_NUMBER = entity.ClientDocumentNumber;
                    }
                    
                    // Set the variation hours
                    newCopy.VARIATION_HOURS = entity.VariationHours;
                    await _repository.UpdateAsync(newCopy);
                    
                    var resultEntity = MapToEntity(newCopy);
                    SetUIStatus(resultEntity);
                    return Ok(resultEntity);
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error processing request");
                return StatusCode(500, $"Error: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Creates a new deliverable for a variation
        /// </summary>
        [HttpPost("odata/v1/Deliverables/CreateForVariation")]
        public async Task<IActionResult> CreateForVariation([FromBody] DeliverableEntity entity)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(ModelState);
                
                _logger?.LogInformation($"Received CreateForVariation request for variation {entity.VariationGuid}");
                
                // Validate that this is actually for a variation
                if (entity.VariationGuid == null || entity.VariationGuid == Guid.Empty)
                {
                    return BadRequest("VariationGuid is required for creating a variation deliverable");
                }
                
                // First create a GUID for this new deliverable
                var deliverableGuid = Guid.NewGuid();
        
                // Create domain model from entity
                var deliverable = new DELIVERABLE
                {
                    GUID = deliverableGuid,
                    GUID_PROJECT = entity.ProjectGuid,
                    AREA_NUMBER = entity.AreaNumber,
                    DISCIPLINE = entity.Discipline,
                    DOCUMENT_TYPE = entity.DocumentType,
                    DEPARTMENT_ID = entity.DepartmentId,
                    DELIVERABLE_TYPE_ID = entity.DeliverableTypeId,
                    GUID_DELIVERABLE_GATE = entity.DeliverableGateGuid,
                    INTERNAL_DOCUMENT_NUMBER = entity.InternalDocumentNumber,
                    CLIENT_DOCUMENT_NUMBER = entity.ClientDocumentNumber,
                    DOCUMENT_TITLE = entity.DocumentTitle,
                    BUDGET_HOURS = entity.BudgetHours,
                    VARIATION_HOURS = entity.VariationHours,
                    APPROVED_VARIATION_HOURS = 0, // New variations start with 0 approved hours
                    TOTAL_COST = entity.TotalCost,
                    BOOKING_CODE = await CalculateBookingCodeAsync(entity.ProjectGuid, entity.AreaNumber, entity.Discipline, entity.BookingCode),
                    
                    // Set variation fields
                    VARIATION_STATUS = (int)VariationStatus.UnapprovedVariation,
                    GUID_VARIATION = entity.VariationGuid,
                    // For new deliverables in a variation, set originalDeliverableGuid to its own GUID
                    GUID_ORIGINAL_DELIVERABLE = deliverableGuid // Set to its own GUID for proper tracking
                };
                
                var result = await _repository.CreateAsync(deliverable);
                var resultEntity = MapToEntity(result);
                SetUIStatus(resultEntity); // Set the UI status for the entity
                return Created($"odata/v1/Deliverables/{result.GUID}", resultEntity);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error creating variation deliverable");
                return StatusCode(500, $"Error: {ex.Message}");
            }
        }
    }
}
