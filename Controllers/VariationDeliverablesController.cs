using FourSPM_WebService.Data.EF.FourSPM;
using FourSPM_WebService.Data.Interfaces;
using FourSPM_WebService.Data.OData.FourSPM;
using FourSPM_WebService.Helpers;
using Microsoft.EntityFrameworkCore;
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
    public class VariationDeliverablesController : FourSPMODataController
    {
        private readonly IDeliverableRepository _repository;
        private readonly IProjectRepository _projectRepository;
        private readonly ILogger<VariationDeliverablesController> _logger;

        public VariationDeliverablesController(
            IDeliverableRepository repository, 
            IProjectRepository projectRepository, 
            ILogger<VariationDeliverablesController> logger)
        {
            _repository = repository;
            _projectRepository = projectRepository;
            _logger = logger;
        }

        /// <summary>
        /// Gets variation deliverables with OData query support, filtered by variationId
        /// </summary>
        /// <param name="variationId">Required parameter to filter by a specific variation. Returns merged view of original and variation deliverables.</param>
        [HttpGet]
        [EnableQuery]
        public IQueryable<DeliverableEntity> Get([FromODataUri] Guid variationId)
        {
            _logger?.LogInformation($"Getting merged deliverables for variation {variationId} with IQueryable support");

            // Get merged variation deliverables using the optimized repository method
            // This includes both existing variation deliverables and eligible original deliverables
            IQueryable<DELIVERABLE> deliverables = _repository.GetMergedVariationDeliverables(variationId);
            
            // Map to entities while maintaining IQueryable for OData processing
            var entitiesQuery = deliverables.Select(DeliverableMapperHelper.GetEntityMappingExpression(variationId));
            
            // Return IQueryable directly for optimal OData performance with virtual scrolling
            return entitiesQuery;
        }

        public async Task<IActionResult> Patch([FromODataUri] Guid key, [FromBody] Delta<DeliverableEntity> delta)
        {
            try
            {
                _logger?.LogInformation($"Received PATCH variation request for deliverable {key}");

                // Create entity with required properties (needed for OData serialization)
                var entity = new DeliverableEntity
                {
                    Discipline = string.Empty,
                    DocumentType = string.Empty,
                    DocumentTitle = string.Empty,
                    InternalDocumentNumber = string.Empty
                };
                delta.CopyChangedValues(entity);
                
                // Validate required fields after delta is applied
                if (key == Guid.Empty)
                    return BadRequest("Deliverable ID is required");
                
                if (entity.OriginalDeliverableGuid == Guid.Empty)
                    return BadRequest("Original Deliverable GUID is required");
                    
                if (entity.VariationGuid == Guid.Empty)
                    return BadRequest("Variation GUID is required for variation deliverables");
                
                _logger?.LogInformation($"Processing variation deliverable for variation {entity.VariationGuid}, original deliverable {entity.OriginalDeliverableGuid}");
                
                // Handle different variation scenarios
                return await HandleVariationUpdate(key, entity, delta);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error updating variation deliverable");
                return StatusCode(500, $"Error: {ex.Message}");
            }
        }
        
        #region Helper methods for Patch operation
        private async Task<IActionResult> HandleVariationUpdate(Guid key, DeliverableEntity entity, Delta<DeliverableEntity> delta)
        {
            // Validate request has required GUIDs
            if (!entity.OriginalDeliverableGuid.HasValue)
                return BadRequest("Original deliverable GUID is required");

            // Get original deliverable and validate it exists
            var originalDeliverable = await _repository.GetByIdAsync(entity.OriginalDeliverableGuid.Value);
            if (originalDeliverable == null)
                return NotFound($"Original deliverable with ID {entity.OriginalDeliverableGuid} not found");
            
            // Ensure the entity being updated is associated with a variation
            if (entity.VariationGuid == null || entity.VariationGuid == Guid.Empty)
                return BadRequest("Only deliverables associated with variations can be updated through this endpoint");
            
            // Do not allow updates to approved variations
            bool isApproved = originalDeliverable.VARIATION_STATUS == (int)VariationStatus.ApprovedVariation || 
                            originalDeliverable.VARIATION_STATUS == (int)VariationStatus.ApprovedCancellation;
            if (isApproved)
                return BadRequest("Cannot edit deliverables for approved variations");
            
            // Determine whether this is updating the original deliverable
            bool isOriginal = key == entity.OriginalDeliverableGuid.Value;
            
            // Only use direct update path if:
            // 1. We're updating the original deliverable (not a variation copy)
            // 2. The original deliverable belongs to the same variation
            // This prevents updating deliverables from one variation via another variation
            if (isOriginal && originalDeliverable.GUID_VARIATION == entity.VariationGuid)
            {
                // Original deliverable belongs to this variation - use direct update path
                return await HandleStandardDeliverableUpdate(originalDeliverable, delta);
            }
            
            // Ensure variation GUID is provided when updating a variation copy
            if (!entity.VariationGuid.HasValue || entity.VariationGuid.Value == Guid.Empty)
                return BadRequest("Variation GUID is required when updating a variation deliverable");
            
            // Handle as variation copy update
            return await HandleVariationCopyUpdate(entity, originalDeliverable, delta);
        }

        private async Task<IActionResult> HandleStandardDeliverableUpdate(DELIVERABLE existingDeliverable, Delta<DeliverableEntity> delta)
        {
            // Update deliverable properties from delta
            var updatedEntity = DeliverableMapperHelper.MapToEntity(existingDeliverable, currentVariationGuid: existingDeliverable.GUID_VARIATION);
            if (updatedEntity != null)
            {
                delta.CopyChangedValues(updatedEntity);
                
                // Map changes back to database entity
                UpdateDeliverableProperties(existingDeliverable, updatedEntity);
                
                await _repository.UpdateAsync(existingDeliverable);
                
                // Return updated entity
                var resultEntity = DeliverableMapperHelper.MapToEntity(existingDeliverable, currentVariationGuid: existingDeliverable.GUID_VARIATION);
                return Updated(resultEntity);
            }
            else
            {
                return BadRequest("Failed to map deliverable for update");
            }
        }

        private async Task<IActionResult> HandleVariationCopyUpdate(DeliverableEntity entity, DELIVERABLE originalDeliverable, Delta<DeliverableEntity> delta)
        {
            // Get or create variation copy
            var (errorResult, existingCopy, isNewCopy) = await GetOrCreateVariationCopy(entity, originalDeliverable, delta.GetChangedPropertyNames());
            if (errorResult != null)
                return errorResult;
                
            // Safety check for the existingCopy
            if (existingCopy == null)
            {
                return BadRequest("Failed to create or locate the variation deliverable");
            }
            
            // Update the variation copy with changes
            var updateResult = await UpdateVariationCopy(existingCopy, entity, delta.GetChangedPropertyNames(), isNewCopy);
            
            // Map result to entity and return appropriate response
            var resultEntity = DeliverableMapperHelper.MapToEntity(updateResult, currentVariationGuid: updateResult.GUID_VARIATION);
            
            return isNewCopy 
                ? Created($"odata/v1/VariationDeliverables/{updateResult.GUID}", resultEntity)
                : Updated(resultEntity);
        }

        private void UpdateDeliverableProperties(DELIVERABLE existingDeliverable, DeliverableEntity updatedEntity)
        {
            existingDeliverable.GUID_PROJECT = updatedEntity.ProjectGuid;
            existingDeliverable.AREA_NUMBER = updatedEntity.AreaNumber;
            existingDeliverable.DISCIPLINE = updatedEntity.Discipline ?? string.Empty;
            existingDeliverable.DOCUMENT_TYPE = updatedEntity.DocumentType ?? string.Empty;
            existingDeliverable.DEPARTMENT_ID = updatedEntity.DepartmentId;
            existingDeliverable.DELIVERABLE_TYPE_ID = updatedEntity.DeliverableTypeId;
            existingDeliverable.GUID_DELIVERABLE_GATE = updatedEntity.DeliverableGateGuid;
            existingDeliverable.INTERNAL_DOCUMENT_NUMBER = updatedEntity.InternalDocumentNumber ?? string.Empty;
            existingDeliverable.CLIENT_DOCUMENT_NUMBER = updatedEntity.ClientDocumentNumber ?? string.Empty;
            existingDeliverable.DOCUMENT_TITLE = updatedEntity.DocumentTitle ?? string.Empty;
            existingDeliverable.BUDGET_HOURS = updatedEntity.BudgetHours;
            existingDeliverable.VARIATION_HOURS = updatedEntity.VariationHours;
            existingDeliverable.TOTAL_COST = updatedEntity.TotalCost;
        }
        
        private async Task<(IActionResult? errorResult, DELIVERABLE? existingCopy, bool isNewCopy)> GetOrCreateVariationCopy(
            DeliverableEntity entity, 
            DELIVERABLE originalDeliverable,
            IEnumerable<string> changedProps)
        {
            if (entity.OriginalDeliverableGuid == null || entity.VariationGuid == null)
                return (BadRequest("Missing required guids"), null, false);
                
            // Check if a variation copy already exists
            var existingCopy = await _repository.FindExistingVariationDeliverableAsync(
                entity.OriginalDeliverableGuid.Value, entity.VariationGuid.Value);
            
            bool isNewCopy = existingCopy == null;
            
            if (isNewCopy)
            {
                _logger?.LogInformation($"Creating new variation copy for deliverable {entity.OriginalDeliverableGuid}");
                
                // Determine variation status
                int status = (entity.VariationStatus == VariationStatus.UnapprovedCancellation || 
                            entity.VariationStatus == VariationStatus.ApprovedCancellation) 
                    ? (int)VariationStatus.UnapprovedCancellation 
                    : (int)VariationStatus.UnapprovedVariation;
                
                // Create new variation copy
                existingCopy = await _repository.CreateVariationCopyAsync(
                    originalDeliverable, 
                    entity.VariationGuid.Value, 
                    status);
                    
                if (existingCopy == null)
                    return (BadRequest("Failed to create variation copy"), null, false);
            }
            else if (existingCopy != null)
            {
                _logger?.LogInformation($"Updating existing variation copy {existingCopy.GUID}");
            }
            else
            {
                _logger?.LogWarning("Unexpected condition: isNewCopy is false but existingCopy is null");
                return (BadRequest("Failed to locate or create variation deliverable"), null, false);
            }
            
            return (null, existingCopy, isNewCopy);
        }
        
        private async Task<DELIVERABLE> UpdateVariationCopy(
            DELIVERABLE existingCopy, 
            DeliverableEntity entity, 
            IEnumerable<string> changedProps,
            bool isNewCopy)
        {
            ArgumentNullException.ThrowIfNull(existingCopy);
            
            // Set the primary field we want to update
            existingCopy.VARIATION_HOURS = entity.VariationHours;
            existingCopy.VARIATION_STATUS = (int)VariationStatus.UnapprovedVariation;
            
            // Update booking code if needed
            await UpdateBookingCodeIfNeeded(existingCopy, changedProps, entity, isNewCopy);
            
            // Save changes
            return await _repository.UpdateAsync(existingCopy);
        }
        
        private async Task UpdateBookingCodeIfNeeded(
            DELIVERABLE existingCopy, 
            IEnumerable<string> changedProps, 
            DeliverableEntity entity,
            bool isNewCopy)
        {
            // Check if booking code needs recalculation
            bool shouldUpdateBookingCode = changedProps.Any(p => 
                p == "AreaNumber" || p == "Discipline" || p == "ProjectGuid") || isNewCopy;
            
            if (shouldUpdateBookingCode)
            {
                // Recalculate booking code for consistency
                existingCopy.BOOKING_CODE = await BookingCodeHelper.CalculateAsync(
                    _projectRepository,
                    existingCopy.GUID_PROJECT, 
                    existingCopy.AREA_NUMBER, 
                    existingCopy.DISCIPLINE ?? string.Empty,
                    existingCopy.BOOKING_CODE ?? string.Empty);
            }
            else if (changedProps.Contains("BookingCode") && entity.BookingCode != null)
            {
                // Use explicitly provided booking code if present
                existingCopy.BOOKING_CODE = entity.BookingCode ?? string.Empty;
            }
        }
        
        #endregion

        /// <summary>
        /// Cancels a deliverable by creating a cancellation variation or marking it as deleted
        /// </summary>
        /// <param name="originalDeliverableGuid">The GUID of the original deliverable to cancel</param>
        /// <param name="variationGuid">The GUID of the variation this cancellation belongs to</param>
        /// <returns>The cancelled deliverable with updated status</returns>
        [HttpPost("/odata/v1/VariationDeliverables/CancelDeliverable(originalDeliverableGuid={originalDeliverableGuid},variationGuid={variationGuid})")]
        public async Task<IActionResult> CancelDeliverable([FromODataUri] Guid originalDeliverableGuid, [FromODataUri] Guid variationGuid)
        {
            try
            {
                _logger?.LogInformation($"Cancelling deliverable {originalDeliverableGuid} for variation {variationGuid}");
                
                // Call the repository method to handle the cancellation logic
                var result = await _repository.CancelDeliverableAsync(originalDeliverableGuid, variationGuid);
                
                // Map to entity for response
                var resultEntity = DeliverableMapperHelper.MapToEntity(result, currentVariationGuid: result.GUID_VARIATION);
                return Ok(resultEntity);
            }
            catch (KeyNotFoundException ex)
            {
                _logger?.LogWarning(ex, $"Deliverable not found: {originalDeliverableGuid}");
                return NotFound(ex.Message);
            }
            catch (ArgumentException ex)
            {
                _logger?.LogWarning(ex, $"Invalid argument for cancellation");
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, $"Error cancelling deliverable {originalDeliverableGuid}");
                return StatusCode(500, $"Error: {ex.Message}");
            }
        }

        public async Task<IActionResult> Post([FromBody] DeliverableEntity entity)
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
                    DISCIPLINE = entity.Discipline ?? string.Empty,
                    DOCUMENT_TYPE = entity.DocumentType ?? string.Empty,
                    DEPARTMENT_ID = entity.DepartmentId,
                    DELIVERABLE_TYPE_ID = entity.DeliverableTypeId,
                    GUID_DELIVERABLE_GATE = entity.DeliverableGateGuid,
                    INTERNAL_DOCUMENT_NUMBER = entity.InternalDocumentNumber ?? string.Empty,
                    CLIENT_DOCUMENT_NUMBER = entity.ClientDocumentNumber ?? string.Empty,
                    DOCUMENT_TITLE = entity.DocumentTitle ?? string.Empty,
                    BUDGET_HOURS = entity.BudgetHours,
                    VARIATION_HOURS = entity.VariationHours,
                    APPROVED_VARIATION_HOURS = 0, // New variations start with 0 approved hours
                    TOTAL_COST = entity.TotalCost,
                    BOOKING_CODE = await BookingCodeHelper.CalculateAsync(_projectRepository, entity.ProjectGuid, entity.AreaNumber, entity.Discipline ?? string.Empty, entity.BookingCode ?? string.Empty),
                    
                    // Set variation fields
                    VARIATION_STATUS = (int)VariationStatus.UnapprovedVariation,
                    GUID_VARIATION = entity.VariationGuid,
                    // For new deliverables in a variation, set originalDeliverableGuid to its own GUID
                    GUID_ORIGINAL_DELIVERABLE = deliverableGuid // Set to its own GUID for proper tracking
                };
                
                var result = await _repository.CreateAsync(deliverable);
                var resultEntity = DeliverableMapperHelper.MapToEntity(result, currentVariationGuid: result.GUID_VARIATION);
                return Created($"odata/v1/VariationDeliverables/{result.GUID}", resultEntity);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error creating variation deliverable");
                return StatusCode(500, $"Error: {ex.Message}");
            }
        }
    }
}
