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
using Microsoft.EntityFrameworkCore;
using FourSPM_WebService.Attributes;
using FourSPM_WebService.Data.Constants;

namespace FourSPM_WebService.Controllers
{
    [Authorize]
    [ODataRouteComponent("odata/v1")]
    public class AreasController : FourSPMODataController
    {
        private readonly IAreaRepository _repository;
        private readonly ILogger<AreasController> _logger;
        private readonly FourSPMContext _context;

        public AreasController(IAreaRepository repository, ILogger<AreasController> logger, FourSPMContext context)
        {
            _repository = repository;
            _logger = logger;
            _context = context;
        }

        [EnableQuery]
        [RequirePermission(PermissionConstants.AreasView)]
        public async Task<IActionResult> Get()
        {
            var areas = await _repository.GetAllAsync();
            var entities = areas.Select(a => MapToEntity(a));
            return Ok(entities);
        }

        [EnableQuery]
        [RequirePermission(PermissionConstants.AreasView)]
        public async Task<IActionResult> Get([FromRoute] Guid key)
        {
            var area = await _repository.GetByIdAsync(key);
            if (area == null)
                return NotFound();

            return Ok(MapToEntity(area));
        }

        [RequirePermission(PermissionConstants.AreasEdit)]
        public async Task<IActionResult> Post([FromBody] AreaEntity entity)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            // Check if area number is unique within the project
            if (!await IsAreaNumberUniqueInProject(entity.Number, entity.ProjectGuid, null))
            {
                return BadRequest($"An area with number '{entity.Number}' already exists in this project.");
            }

            var area = new AREA
            {
                GUID = entity.Guid,
                GUID_PROJECT = entity.ProjectGuid,
                NUMBER = entity.Number,
                DESCRIPTION = entity.Description
            };

            var result = await _repository.CreateAsync(area, CurrentUser.UserId);
            return Created(MapToEntity(result));
        }

        [RequirePermission(PermissionConstants.AreasEdit)]
        public async Task<IActionResult> Put([FromRoute] Guid key, [FromBody] AreaEntity entity)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            if (key != entity.Guid)
                return BadRequest("The ID in the URL must match the ID in the request body");

            // Check if area number is unique within the project
            if (!await IsAreaNumberUniqueInProject(entity.Number, entity.ProjectGuid, entity.Guid))
            {
                return BadRequest($"An area with number '{entity.Number}' already exists in this project.");
            }

            try
            {
                var area = new AREA
                {
                    GUID = entity.Guid,
                    GUID_PROJECT = entity.ProjectGuid,
                    NUMBER = entity.Number,
                    DESCRIPTION = entity.Description
                };

                var result = await _repository.UpdateAsync(area, CurrentUser.UserId);
                return Updated(MapToEntity(result));
            }
            catch (KeyNotFoundException)
            {
                return NotFound();
            }
        }

        [RequirePermission(PermissionConstants.AreasEdit)]
        public async Task<IActionResult> Delete([FromRoute] Guid key)
        {
            var result = await _repository.DeleteAsync(key, CurrentUser.UserId ?? Guid.Empty);
            return result ? NoContent() : NotFound();
        }

        [RequirePermission(PermissionConstants.AreasEdit)]
        public async Task<IActionResult> Patch([FromODataUri] Guid key, [FromBody] Delta<AreaEntity> delta)
        {
            try
            {
                _logger?.LogInformation($"Received PATCH request for area {key}");

                if (key == Guid.Empty)
                {
                    return BadRequest("Invalid GUID");
                }

                if (delta == null)
                {
                    _logger?.LogWarning($"Update data is null for area {key}");
                    return BadRequest("Update data cannot be null");
                }

                // Get the existing area
                var existingArea = await _repository.GetByIdAsync(key);
                if (existingArea == null)
                {
                    return NotFound("Area with ID was not found");
                }

                // Create a copy of the entity to track changes
                var updatedEntity = MapToEntity(existingArea);
                delta.CopyChangedValues(updatedEntity);

                // Check if area number is unique within the project if it was changed
                if (delta.GetChangedPropertyNames().Contains("Number") && 
                    !await IsAreaNumberUniqueInProject(updatedEntity.Number, updatedEntity.ProjectGuid, key))
                {
                    return BadRequest($"An area with number '{updatedEntity.Number}' already exists in this project.");
                }

                // Map back to AREA entity
                existingArea.GUID_PROJECT = updatedEntity.ProjectGuid;
                existingArea.NUMBER = updatedEntity.Number;
                existingArea.DESCRIPTION = updatedEntity.Description;

                var result = await _repository.UpdateAsync(existingArea, CurrentUser.UserId);
                return Updated(MapToEntity(result));
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, $"Error updating area {key}");
                return StatusCode(500, "Internal Server Error");
            }
        }

        private async Task<bool> IsAreaNumberUniqueInProject(string number, Guid projectGuid, Guid? excludeAreaGuid)
        {
            // Check if an area with the same number already exists in the project
            var query = _context.AREAs
                .Where(a => a.NUMBER == number && a.GUID_PROJECT == projectGuid && a.DELETED == null);
                
            // If we're updating an existing area, exclude it from the uniqueness check
            if (excludeAreaGuid.HasValue)
            {
                query = query.Where(a => a.GUID != excludeAreaGuid.Value);
            }
            
            return await query.CountAsync() == 0;
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
