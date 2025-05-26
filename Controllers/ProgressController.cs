using FourSPM_WebService.Data.EF.FourSPM;
using FourSPM_WebService.Data.Interfaces;
using FourSPM_WebService.Data.OData.FourSPM;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OData.Query;
using Microsoft.AspNetCore.OData.Routing.Attributes;
using Microsoft.AspNetCore.OData.Routing.Controllers;
using Microsoft.AspNetCore.OData.Deltas;
using Newtonsoft.Json;
using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.OData.Formatter;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using FourSPM_WebService.Models.Shared;
using FourSPM_WebService.Attributes;
using FourSPM_WebService.Data.Constants;

namespace FourSPM_WebService.Controllers
{
    [Authorize]
    [ODataRouteComponent("odata/v1")]
    public class ProgressController : FourSPMODataController
    {
        private readonly IProgressRepository _repository;
        private readonly ILogger<ProgressController> _logger;

        public ProgressController(IProgressRepository repository, ILogger<ProgressController> logger)
        {
            _repository = repository;
            _logger = logger;
        }

        [EnableQuery]
        [RequirePermission(PermissionConstants.ProgressView)]
        public async Task<IActionResult> Get()
        {
            var progressItems = await _repository.GetAllAsync();
            var entities = progressItems.Select(p => MapToEntity(p));
            return Ok(entities);
        }

        [EnableQuery]
        [RequirePermission(PermissionConstants.ProgressView)]
        public async Task<IActionResult> Get([FromRoute] Guid key)
        {
            var progress = await _repository.GetByIdAsync(key);
            if (progress == null)
                return NotFound();

            return Ok(MapToEntity(progress));
        }

        [EnableQuery]
        [HttpGet("odata/v1/GetByDeliverable({deliverableId})")]
        [RequirePermission(PermissionConstants.ProgressView)]
        public async Task<IActionResult> GetByDeliverable([FromRoute] Guid deliverableId)
        {
            var progressItems = await _repository.GetByDeliverableIdAsync(deliverableId);
            var entities = progressItems.Select(p => MapToEntity(p)).ToList();
            
            var response = new ODataResponse<ProgressEntity>
            {
                Value = entities,
                Count = entities.Count
            };

            return Ok(response);
        }

        [RequirePermission(PermissionConstants.ProgressEdit)]
        public async Task<IActionResult> Post([FromBody] ProgressEntity entity)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var progress = new PROGRESS
            {
                GUID_DELIVERABLE = entity.DeliverableGuid,
                PERIOD = entity.Period,
                UNITS = entity.Units
            };

            var result = await _repository.CreateAsync(progress, CurrentUser.UserId);
            return Created(MapToEntity(result));
        }

        [RequirePermission(PermissionConstants.ProgressEdit)]
        public async Task<IActionResult> Put([FromRoute] Guid key, [FromBody] ProgressEntity entity)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            if (key != entity.Guid)
                return BadRequest("The ID in the URL must match the ID in the request body");

            try
            {
                var progress = new PROGRESS
                {
                    GUID = entity.Guid,
                    GUID_DELIVERABLE = entity.DeliverableGuid,
                    PERIOD = entity.Period,
                    UNITS = entity.Units
                };

                var result = await _repository.UpdateAsync(progress, CurrentUser.UserId);
                return Updated(MapToEntity(result));
            }
            catch (KeyNotFoundException)
            {
                return NotFound();
            }
        }

        [RequirePermission(PermissionConstants.ProgressEdit)]
        public async Task<IActionResult> Delete([FromRoute] Guid key)
        {
            var result = await _repository.DeleteAsync(key, CurrentUser.UserId ?? Guid.Empty);
            return result ? NoContent() : NotFound();
        }

        /// <summary>
        /// Partially updates a progress record
        /// </summary>
        /// <param name="key">The GUID of the progress record to update</param>
        /// <param name="delta">The progress properties to update</param>
        /// <returns>The updated progress record</returns>
        [RequirePermission(PermissionConstants.ProgressEdit)]
        public async Task<IActionResult> Patch([FromODataUri] Guid key, [FromBody] Delta<ProgressEntity> delta)
        {
            try
            {
                _logger?.LogInformation($"Received PATCH request for progress {key}");

                if (key == Guid.Empty)
                {
                    return BadRequest("Invalid GUID - The progress ID cannot be empty");
                }

                if (delta == null)
                {
                    _logger?.LogWarning($"Update data is null for progress {key}");
                    return BadRequest("Update data cannot be null. The request body must contain valid properties to update.");
                }

                // Get the existing progress
                var existingProgress = await _repository.GetByIdAsync(key);
                if (existingProgress == null)
                {
                    return NotFound("Progress with ID " + key + " was not found");
                }

                // Create a copy of the entity to track changes
                var updatedEntity = MapToEntity(existingProgress);
                delta.CopyChangedValues(updatedEntity);

                // Map back to PROGRESS entity
                existingProgress.GUID_DELIVERABLE = updatedEntity.DeliverableGuid;
                existingProgress.PERIOD = updatedEntity.Period;
                existingProgress.UNITS = updatedEntity.Units;

                var result = await _repository.UpdateAsync(existingProgress, CurrentUser.UserId);
                return Updated(MapToEntity(result));
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error updating progress");
                return StatusCode(500, "Internal Server Error - " + ex.Message);
            }
        }

        /// <summary>
        /// Adds a new progress record if it doesn't exist or updates an existing one
        /// </summary>
        /// <param name="entity">The progress entity to add or update</param>
        /// <returns>The newly created or updated progress record</returns>
        [HttpPost("odata/v1/Progress/AddOrUpdateExisting")]
        [RequirePermission(PermissionConstants.ProgressEdit)]
        public async Task<IActionResult> AddOrUpdateExisting([FromBody] ProgressEntity entity)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                _logger?.LogInformation($"Received AddOrUpdateExisting request for deliverable {entity.DeliverableGuid}, period {entity.Period}");

                // Check if a progress record already exists for this deliverable and period
                var existingItems = await _repository.GetByDeliverableIdAsync(entity.DeliverableGuid);
                var matchingItem = existingItems.FirstOrDefault(p => 
                    p.PERIOD == entity.Period && 
                    p.DELETED == null);

                if (matchingItem != null)
                {
                    // Update existing progress
                    _logger?.LogInformation($"Updating existing progress item {matchingItem.GUID}");

                    matchingItem.UNITS = entity.Units;

                    var result = await _repository.UpdateAsync(matchingItem, CurrentUser.UserId);
                    return Ok(MapToEntity(result));
                }
                else
                {
                    // Create new progress record
                    _logger?.LogInformation($"Creating new progress item for deliverable {entity.DeliverableGuid}");
                    
                    var newProgress = new PROGRESS
                    {
                        GUID = entity.Guid != Guid.Empty ? entity.Guid : Guid.NewGuid(),
                        GUID_DELIVERABLE = entity.DeliverableGuid,
                        PERIOD = entity.Period,
                        UNITS = entity.Units
                    };

                    var result = await _repository.CreateAsync(newProgress, CurrentUser.UserId);
                    // Don't use OData-specific Created() for a custom endpoint
                    return Ok(MapToEntity(result));
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error processing request");
                return StatusCode(500, $"Error: {ex.Message}");
            }
        }

        private static ProgressEntity MapToEntity(PROGRESS progress)
        {
            return new ProgressEntity
            {
                Guid = progress.GUID,
                DeliverableGuid = progress.GUID_DELIVERABLE,
                Period = progress.PERIOD,
                Units = progress.UNITS,
                Created = progress.CREATED,
                CreatedBy = progress.CREATEDBY,
                Updated = progress.UPDATED,
                UpdatedBy = progress.UPDATEDBY,
                Deleted = progress.DELETED,
                DeletedBy = progress.DELETEDBY,
                Deliverable = progress.Deliverable != null ? new DeliverableEntity
                {
                    Guid = progress.Deliverable.GUID,
                    ProjectGuid = progress.Deliverable.GUID_PROJECT,
                    AreaNumber = progress.Deliverable.AREA_NUMBER,
                    Discipline = progress.Deliverable.DISCIPLINE,
                    DocumentType = progress.Deliverable.DOCUMENT_TYPE,
                    InternalDocumentNumber = progress.Deliverable.INTERNAL_DOCUMENT_NUMBER,
                    DocumentTitle = progress.Deliverable.DOCUMENT_TITLE,
                    DepartmentId = progress.Deliverable.DEPARTMENT_ID,
                    DeliverableTypeId = progress.Deliverable.DELIVERABLE_TYPE_ID,
                    BudgetHours = progress.Deliverable.BUDGET_HOURS,
                    VariationHours = progress.Deliverable.VARIATION_HOURS,
                    TotalCost = progress.Deliverable.TOTAL_COST
                } : null
            };
        }
    }
}
