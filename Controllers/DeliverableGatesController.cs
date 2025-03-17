using FourSPM_WebService.Data.EF.FourSPM;
using FourSPM_WebService.Data.Interfaces;
using FourSPM_WebService.Data.OData.FourSPM;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OData.Query;
using Microsoft.AspNetCore.OData.Routing.Attributes;
using Microsoft.AspNetCore.OData.Routing.Controllers;
using Microsoft.AspNetCore.OData.Deltas;
using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.OData.Formatter;

namespace FourSPM_WebService.Controllers
{
    [Authorize]
    [ODataRouteComponent("odata/v1")]
    public class DeliverableGatesController : FourSPMODataController
    {
        private readonly IDeliverableGateRepository _repository;
        private readonly ILogger<DeliverableGatesController> _logger;

        public DeliverableGatesController(IDeliverableGateRepository repository, ILogger<DeliverableGatesController> logger)
        {
            _repository = repository;
            _logger = logger;
        }

        [EnableQuery]
        public async Task<IActionResult> Get()
        {
            var gates = await _repository.GetAllAsync();
            var entities = gates.Select(g => MapToEntity(g));
            return Ok(entities);
        }

        [EnableQuery]
        public async Task<IActionResult> Get([FromRoute] Guid key)
        {
            var gate = await _repository.GetByIdAsync(key);
            if (gate == null)
                return NotFound();

            return Ok(MapToEntity(gate));
        }

        public async Task<IActionResult> Post([FromBody] DeliverableGateEntity entity)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var gate = new DELIVERABLE_GATE
            {
                GUID = entity.Guid,
                NAME = entity.Name,
                MAX_PERCENTAGE = entity.MaxPercentage,
                AUTO_PERCENTAGE = entity.AutoPercentage,
                CREATEDBY = entity.CreatedBy
            };

            var result = await _repository.CreateAsync(gate);
            return Created(MapToEntity(result));
        }

        public async Task<IActionResult> Put([FromRoute] Guid key, [FromBody] DeliverableGateEntity entity)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            if (key != entity.Guid)
                return BadRequest("The ID in the URL must match the ID in the request body");

            try
            {
                var gate = new DELIVERABLE_GATE
                {
                    GUID = entity.Guid,
                    NAME = entity.Name,
                    MAX_PERCENTAGE = entity.MaxPercentage,
                    AUTO_PERCENTAGE = entity.AutoPercentage,
                    UPDATEDBY = entity.UpdatedBy
                };

                var result = await _repository.UpdateAsync(gate);
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

        public async Task<IActionResult> Patch([FromODataUri] Guid key, [FromBody] Delta<DeliverableGateEntity> delta)
        {
            try
            {
                _logger?.LogInformation($"Received PATCH request for deliverable gate {key}");

                if (key == Guid.Empty)
                {
                    return BadRequest("Invalid GUID - The gate ID cannot be empty");
                }

                if (delta == null)
                {
                    _logger?.LogWarning($"Update data is null for gate {key}");
                    return BadRequest("Update data cannot be null. The request body must contain valid properties to update.");
                }

                // Get the existing gate
                var existingGate = await _repository.GetByIdAsync(key);
                if (existingGate == null)
                {
                    return NotFound("Deliverable gate with ID " + key + " was not found");
                }

                // Create a copy of the entity to track changes
                var updatedEntity = MapToEntity(existingGate);
                delta.CopyChangedValues(updatedEntity);

                // Map back to DELIVERABLE_GATE entity
                var gateToUpdate = new DELIVERABLE_GATE
                {
                    GUID = updatedEntity.Guid,
                    NAME = updatedEntity.Name,
                    MAX_PERCENTAGE = updatedEntity.MaxPercentage,
                    AUTO_PERCENTAGE = updatedEntity.AutoPercentage,
                    UPDATEDBY = updatedEntity.UpdatedBy
                };

                var result = await _repository.UpdateAsync(gateToUpdate);
                return Updated(MapToEntity(result));
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error updating deliverable gate");
                return StatusCode(500, "Internal Server Error - " + ex.Message);
            }
        }

        private static DeliverableGateEntity MapToEntity(DELIVERABLE_GATE gate)
        {
            return new DeliverableGateEntity
            {
                Guid = gate.GUID,
                Name = gate.NAME,
                MaxPercentage = gate.MAX_PERCENTAGE,
                AutoPercentage = gate.AUTO_PERCENTAGE,
                Created = gate.CREATED,
                CreatedBy = gate.CREATEDBY,
                Updated = gate.UPDATED,
                UpdatedBy = gate.UPDATEDBY,
                Deleted = gate.DELETED,
                DeletedBy = gate.DELETEDBY
            };
        }
    }
}
