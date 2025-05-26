using FourSPM_WebService.Data.EF.FourSPM;
using FourSPM_WebService.Data.Interfaces;
using FourSPM_WebService.Data.OData.FourSPM;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OData.Query;
using Microsoft.AspNetCore.OData.Routing.Attributes;
using Microsoft.AspNetCore.OData.Deltas;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.OData.Formatter;
using System;
using System.Linq;
using System.Threading.Tasks;
using FourSPM_WebService.Models.Session;

namespace FourSPM_WebService.Controllers
{
    [Authorize]
    [ODataRouteComponent("odata/v1")]
    public class VariationsController : FourSPMODataController
    {
        private readonly IVariationRepository _repository;
        private readonly ILogger<VariationsController> _logger;

        public VariationsController(
            IVariationRepository repository, 
            ILogger<VariationsController> logger)
        {
            _repository = repository;
            _logger = logger;
        }

        [EnableQuery]
        public async Task<IActionResult> Get()
        {
            var variations = await _repository.GetAllAsync();
            var entities = variations.Select(v => MapToEntity(v));
            return Ok(entities);
        }

        [EnableQuery]
        public async Task<IActionResult> Get([FromRoute] Guid key)
        {
            var variation = await _repository.GetByIdAsync(key);
            if (variation == null)
                return NotFound();

            return Ok(MapToEntity(variation));
        }

        public async Task<IActionResult> Post([FromBody] VariationEntity entity)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var variation = new VARIATION
            {
                GUID = Guid.NewGuid(),
                GUID_PROJECT = entity.ProjectGuid,
                NAME = entity.Name,
                COMMENTS = entity.Comments,
                SUBMITTED = entity.Submitted,
                SUBMITTEDBY = entity.SubmittedBy,
                CLIENT_APPROVED = entity.ClientApproved,
                CLIENT_APPROVEDBY = entity.ClientApprovedBy
            };

            var result = await _repository.CreateAsync(variation);
            return Created(MapToEntity(result));
        }

        public async Task<IActionResult> Put([FromRoute] Guid key, [FromBody] VariationEntity entity)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            if (key != entity.Guid)
                return BadRequest("The ID in the URL must match the ID in the request body");

            try
            {
                var variation = new VARIATION
                {
                    GUID = entity.Guid,
                    GUID_PROJECT = entity.ProjectGuid,
                    NAME = entity.Name,
                    COMMENTS = entity.Comments,
                    SUBMITTED = entity.Submitted,
                    SUBMITTEDBY = entity.SubmittedBy,
                    CLIENT_APPROVED = entity.ClientApproved,
                    CLIENT_APPROVEDBY = entity.ClientApprovedBy,
                    CREATED = entity.Created,
                    CREATEDBY = entity.CreatedBy
                };

                var result = await _repository.UpdateAsync(variation);
                return Updated(MapToEntity(result));
            }
            catch (KeyNotFoundException)
            {
                return NotFound();
            }
        }

        public async Task<IActionResult> Delete([FromRoute] Guid key)
        {
            try
            {
                _logger?.LogInformation($"Received delete request for variation {key}");

                // First get the variation to check if it's approved
                var variation = await _repository.GetByIdAsync(key);
                
                if (variation == null)
                {
                    return NotFound($"Variation with ID {key} not found");
                }

                // Check if the variation is approved - prevent deletion of approved variations
                if (variation.CLIENT_APPROVED.HasValue)
                {
                    _logger?.LogWarning($"Attempted to delete approved variation {key}");
                    return BadRequest("Approved variations cannot be deleted. Reject the variation first.");
                }

                // Proceed with deletion since the variation isn't approved
                var result = await _repository.DeleteAsync(key, CurrentUser.UserId ?? Guid.Empty);
                return result ? NoContent() : NotFound();
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, $"Error deleting variation {key}");
                return StatusCode(500, "An error occurred while deleting the variation");
            }
        }

        public async Task<IActionResult> Patch([FromODataUri] Guid key, [FromBody] Delta<VariationEntity> delta)
        {
            try
            {
                _logger?.LogInformation($"Received PATCH request for variation {key}");

                if (key == Guid.Empty)
                {
                    return BadRequest("Invalid GUID");
                }

                if (delta == null)
                {
                    _logger?.LogWarning($"Update data is null for variation {key}");
                    return BadRequest("Update data cannot be null");
                }

                // Get the existing variation
                var existingVariation = await _repository.GetByIdAsync(key);
                if (existingVariation == null)
                {
                    return NotFound("Variation with ID was not found");
                }

                // Create a copy of the entity to track changes
                var updatedEntity = MapToEntity(existingVariation);
                delta.CopyChangedValues(updatedEntity);

                // Map back to VARIATION entity
                existingVariation.GUID_PROJECT = updatedEntity.ProjectGuid;
                existingVariation.NAME = updatedEntity.Name;
                existingVariation.COMMENTS = updatedEntity.Comments;
                existingVariation.SUBMITTED = updatedEntity.Submitted;
                existingVariation.SUBMITTEDBY = updatedEntity.SubmittedBy;
                existingVariation.CLIENT_APPROVED = updatedEntity.ClientApproved;
                existingVariation.CLIENT_APPROVEDBY = updatedEntity.ClientApprovedBy;

                var result = await _repository.UpdateAsync(existingVariation);
                return Updated(MapToEntity(result));
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, $"Error updating variation {key}");
                return StatusCode(500, "Internal Server Error");
            }
        }

        /// <summary>
        /// Approves a variation, updating all variation deliverables to approved status
        /// </summary>
        /// <param name="id">The GUID of the variation to approve</param>
        /// <returns>The updated variation entity with approval information</returns>
        [HttpPost("odata/v1/Variations/ApproveVariation/{id}")]
        public async Task<IActionResult> ApproveVariation([FromRoute] Guid id)
        {
            try
            {
                _logger?.LogInformation($"Received approval request for variation {id}");

                if (id == Guid.Empty)
                {
                    return BadRequest("Invalid variation ID");
                }

                // Check if the variation exists
                if (!await _repository.ExistsAsync(id))
                {
                    return NotFound($"Variation with ID {id} not found");
                }

                // Approve the variation
                var variation = await _repository.ApproveVariationAsync(id);
                
                return Ok(MapToEntity(variation));
            }
            catch (InvalidOperationException ex)
            {
                _logger?.LogError(ex, $"Business rule violation approving variation {id}: {ex.Message}");
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, $"Error approving variation {id}");
                return StatusCode(500, ex.Message);
            }
        }

        /// <summary>
        /// Rejects a previously approved variation, reverting all variation deliverables to unapproved status
        /// </summary>
        /// <param name="id">The GUID of the variation to reject</param>
        /// <returns>The updated variation entity with approval information cleared</returns>
        [HttpPost("odata/v1/Variations/RejectVariation/{id}")]
        public async Task<IActionResult> RejectVariation([FromRoute] Guid id)
        {
            try
            {
                _logger?.LogInformation($"Received rejection request for variation {id}");

                if (id == Guid.Empty)
                {
                    return BadRequest("Invalid variation ID");
                }

                // Check if the variation exists
                if (!await _repository.ExistsAsync(id))
                {
                    return NotFound($"Variation with ID {id} not found");
                }

                // Reject the variation
                var variation = await _repository.RejectVariationAsync(id);
                
                return Ok(MapToEntity(variation));
            }
            catch (InvalidOperationException ex)
            {
                _logger?.LogError(ex, $"Business rule violation rejecting variation {id}: {ex.Message}");
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, $"Error rejecting variation {id}");
                return StatusCode(500, ex.Message);
            }
        }

        private static VariationEntity MapToEntity(VARIATION variation)
        {
            return new VariationEntity
            {
                Guid = variation.GUID,
                ProjectGuid = variation.GUID_PROJECT,
                Name = variation.NAME,
                Comments = variation.COMMENTS,
                Submitted = variation.SUBMITTED,
                SubmittedBy = variation.SUBMITTEDBY,
                ClientApproved = variation.CLIENT_APPROVED,
                ClientApprovedBy = variation.CLIENT_APPROVEDBY,
                Created = variation.CREATED,
                CreatedBy = variation.CREATEDBY,
                Updated = variation.UPDATED,
                UpdatedBy = variation.UPDATEDBY,
                Deleted = variation.DELETED,
                DeletedBy = variation.DELETEDBY
            };
        }
    }
}
