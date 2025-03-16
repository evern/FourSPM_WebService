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
using Newtonsoft.Json;

namespace FourSPM_WebService.Controllers
{
    [Authorize]
    [ODataRouteComponent("odata/v1")]
    public class AreasController : FourSPMODataController
    {
        private readonly IAreaRepository _repository;
        private readonly ILogger<AreasController> _logger;

        public AreasController(IAreaRepository repository, ILogger<AreasController> logger)
        {
            _repository = repository;
            _logger = logger;
        }

        public class ODataResponse<T>
        {
            public required IEnumerable<T> Value { get; set; }
            [JsonProperty("@odata.count")]
            public required int Count { get; set; }

            public ODataResponse()
            {
                Value = Enumerable.Empty<T>();
                Count = 0;
            }
        }

        [EnableQuery]
        public async Task<IActionResult> Get()
        {
            var areas = await _repository.GetAllAsync();
            var entities = areas.Select(a => MapToEntity(a));
            return Ok(entities);
        }

        [EnableQuery]
        public async Task<IActionResult> Get([FromRoute] Guid key)
        {
            var area = await _repository.GetByIdAsync(key);
            if (area == null)
                return NotFound();

            return Ok(MapToEntity(area));
        }

        public async Task<IActionResult> Post([FromBody] AreaEntity entity)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var area = new AREA
            {
                GUID = entity.Guid,
                GUID_PROJECT = entity.ProjectGuid,
                NUMBER = entity.Number,
                DESCRIPTION = entity.Description
            };

            var result = await _repository.CreateAsync(area);
            return Created(MapToEntity(result));
        }

        public async Task<IActionResult> Put([FromRoute] Guid key, [FromBody] AreaEntity entity)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            if (key != entity.Guid)
                return BadRequest("The ID in the URL must match the ID in the request body");

            try
            {
                var area = new AREA
                {
                    GUID = entity.Guid,
                    GUID_PROJECT = entity.ProjectGuid,
                    NUMBER = entity.Number,
                    DESCRIPTION = entity.Description
                };

                var result = await _repository.UpdateAsync(area);
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

        public async Task<IActionResult> Patch([FromODataUri] Guid key, [FromBody] Delta<AreaEntity> delta)
        {
            try
            {
                _logger?.LogInformation($"Received PATCH request for area {key}");

                if (key == Guid.Empty)
                {
                    return BadRequest(new { error = "Invalid GUID", message = "The area ID cannot be empty" });
                }

                if (delta == null)
                {
                    _logger?.LogWarning($"Update data is null for area {key}");
                    return BadRequest(new
                    {
                        error = "Update data cannot be null",
                        message = "The request body must contain valid properties to update."
                    });
                }

                // Get the existing area
                var existingArea = await _repository.GetByIdAsync(key);
                if (existingArea == null)
                {
                    return NotFound(new { error = "Not Found", message = $"Area with ID {key} was not found" });
                }

                // Create a copy of the entity to track changes
                var updatedEntity = MapToEntity(existingArea);
                delta.CopyChangedValues(updatedEntity);

                // Map back to AREA entity
                var areaToUpdate = new AREA
                {
                    GUID = updatedEntity.Guid,
                    GUID_PROJECT = updatedEntity.ProjectGuid,
                    NUMBER = updatedEntity.Number,
                    DESCRIPTION = updatedEntity.Description
                };

                var result = await _repository.UpdateAsync(areaToUpdate);
                return Updated(MapToEntity(result));
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, $"Error updating area {key}");
                return StatusCode(500, new { error = "Internal Server Error", message = ex.Message });
            }
        }

        private static AreaEntity MapToEntity(AREA area)
        {
            return new AreaEntity
            {
                Guid = area.GUID,
                ProjectGuid = area.GUID_PROJECT,
                Number = area.NUMBER,
                Description = area.DESCRIPTION,
                Created = area.CREATED,
                CreatedBy = area.CREATEDBY,
                Updated = area.UPDATED,
                UpdatedBy = area.UPDATEDBY,
                Deleted = area.DELETED,
                DeletedBy = area.DELETEDBY
            };
        }
    }
}
