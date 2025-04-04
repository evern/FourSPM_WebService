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
        private readonly ApplicationUser _applicationUser;
        private readonly ILogger<VariationsController> _logger;

        public VariationsController(
            IVariationRepository repository, 
            ApplicationUser applicationUser,
            ILogger<VariationsController> logger)
        {
            _repository = repository;
            _applicationUser = applicationUser;
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
            var result = await _repository.DeleteAsync(key, _applicationUser.UserId ?? Guid.Empty);
            return result ? NoContent() : NotFound();
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
