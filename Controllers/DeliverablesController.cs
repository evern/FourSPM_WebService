using FourSPM_WebService.Data.EF.FourSPM;
using FourSPM_WebService.Data.Interfaces;
using FourSPM_WebService.Data.OData.FourSPM;
using FourSPM_WebService.Helpers;
using FourSPM_WebService.Models.Session;
using FourSPM_WebService.Utilities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
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
        public IQueryable<DeliverableEntity> Get()
        {
            // Get the collection from repository directly as IQueryable
            IQueryable<DELIVERABLE> deliverables = _repository.GetAllAsync();
            
            // Apply the mapping expression directly to the IQueryable
            // This allows OData to translate filters to SQL before execution
            var entities = deliverables.Select(DeliverableMapperHelper.GetEntityMappingExpression());
            
            // Return IQueryable directly for optimal OData performance
            // OData will handle filtering/sorting/paging at the database level
            return entities;
        }

        [EnableQuery]
        public async Task<IActionResult> Get([FromRoute] Guid key)
        {
            var deliverable = await _repository.GetByIdAsync(key);
            if (deliverable == null)
                return NotFound();

            var entity = DeliverableMapperHelper.MapToEntity(deliverable);
            if (entity == null)
                return NotFound($"Failed to map deliverable with ID {key}");
                
            return Ok(entity);
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
                
                // Standard deliverable with no variation association
                VARIATION_STATUS = 0, // Standard status
                GUID_VARIATION = null,
                GUID_ORIGINAL_DELIVERABLE = entity.Guid, // Set original GUID to match the deliverable's GUID for proper tracking
                APPROVED_VARIATION_HOURS = entity.ApprovedVariationHours
            };

            // Calculate booking code if not provided, otherwise use the one from entity
            deliverable.BOOKING_CODE = await CalculateBookingCodeAsync(entity.ProjectGuid, entity.AreaNumber, entity.Discipline, entity.BookingCode);

            var result = await _repository.CreateAsync(deliverable);
            return Created(DeliverableMapperHelper.MapToEntity(result));
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
                return Updated(DeliverableMapperHelper.MapToEntity(result));
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
                var updatedEntity = DeliverableMapperHelper.MapToEntity(existingDeliverable);
                if (updatedEntity != null) {
                    delta.CopyChangedValues(updatedEntity);
                }

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
                return Updated(DeliverableMapperHelper.MapToEntity(result));
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error updating deliverable");
                return StatusCode(500, "Internal Server Error - " + ex.Message);
            }
        }

        // Use shared helper method to handle booking code calculation
        private async Task<string> CalculateBookingCodeAsync(Guid projectGuid, string? areaNumber, string? discipline, string? existingBookingCode = null, PROJECT? project = null)
        {
            return await Helpers.BookingCodeHelper.CalculateAsync(
                _projectRepository,
                projectGuid,
                areaNumber,
                discipline,
                existingBookingCode,
                project);
        }

        // Add a new action to suggest an internal document number
        [HttpGet("/odata/v1/Deliverables/SuggestInternalDocumentNumber")]
        public async Task<IActionResult> SuggestInternalDocumentNumber([FromQuery] Guid projectGuid, [FromQuery] string areaNumber, [FromQuery] string discipline, [FromQuery] string documentType, [FromQuery] string deliverableTypeId, [FromQuery] Guid? excludeDeliverableGuid = null)
    {
            _logger?.LogInformation($"Generating suggested internal document number for project {projectGuid}");
            try
            {
                // Get the project details
                var project = await _projectRepository.GetProjectWithClientAsync(projectGuid);
                
                if (project == null)
                {
                    return NotFound($"Project with ID {projectGuid} not found");
                }

                // Get existing deliverables with the same pattern to use for sequence generation
                string baseFormat = DocumentNumberGenerator.CreateBaseFormat(
                    project.Client?.NUMBER ?? string.Empty,
                    project.PROJECT_NUMBER ?? string.Empty,
                    deliverableTypeId,
                    areaNumber,
                    discipline,
                    documentType);
                
                // Get existing deliverables to determine the next sequence number
                var existingDeliverables = await _repository.GetDeliverablesByNumberPatternAsync(
                    projectGuid, baseFormat);
                    
                // Use the utility to build the final document number
                string suggestedNumber = $"{baseFormat}-{DocumentNumberGenerator.DetermineNextSequence(
                    existingDeliverables, excludeDeliverableGuid)}";

                
                return Ok(new { suggestedNumber });
            }
            catch (ArgumentException ex)
            {
                _logger?.LogWarning(ex, "Invalid argument when generating suggested internal document number");
                return BadRequest(ex.Message);
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
                var entityWithProgress = deliverables
                    .Select(d => DeliverableMapperHelper.MapToEntity(d, period))
                    .Where(e => e != null)
                    .Cast<DeliverableEntity>()
                    .ToList();
                
                // Create response with proper count
                var response = new ODataResponse<DeliverableEntity>
                {
                    Value = entityWithProgress,
                    Count = entityWithProgress.Count
                };
                
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error getting deliverables with progress percentages");
                return StatusCode(500, "Internal Server Error - " + ex.Message);
            }
        }
        
        /// <summary>
        /// Gets all deliverables for a specific project
        /// </summary>
        [HttpGet("odata/v1/Deliverables/ByProject/{projectGuid}")]
        public async Task<IActionResult> GetByProject(Guid projectGuid)
        {
            try
            {
                _logger?.LogInformation($"Getting deliverables for project {projectGuid}");
                
                var deliverables = await _repository.GetByProjectIdAsync(projectGuid);
                if (deliverables == null || !deliverables.Any())
                {
                    return Ok(new List<DeliverableEntity>());
                }
                
                var entities = deliverables.Select(d => DeliverableMapperHelper.MapToEntity(d));
                return Ok(entities);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error getting deliverables for project");
                return StatusCode(500, $"Error: {ex.Message}");
            }
        }
    }
}

// NOTE: All variation-related functionality has been moved to VariationDeliverablesController
