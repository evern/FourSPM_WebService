using FourSPM_WebService.Data.EF.FourSPM;
using FourSPM_WebService.Data.Interfaces;
using FourSPM_WebService.Data.OData.FourSPM;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OData.Deltas;
using FourSPM_WebService.Config;
using FourSPM_WebService.Authorization;
using Microsoft.AspNetCore.OData.Formatter;
using Microsoft.AspNetCore.OData.Query;
using Microsoft.AspNetCore.OData.Routing.Attributes;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace FourSPM_WebService.Controllers
{
    [Authorize]
    [ODataRouteComponent("odata/v1")]
    public class DisciplinesController : FourSPMODataController
    {
        private readonly IDisciplineRepository _repository;
        private readonly ILogger<DisciplinesController> _logger;

        public DisciplinesController(IDisciplineRepository repository, ILogger<DisciplinesController> logger)
        {
            _repository = repository;
            _logger = logger;
        }

        [EnableQuery]
        [RequirePermission(AuthConstants.Permissions.ReadDisciplines)]
        public async Task<IActionResult> Get()
        {
            var disciplines = await _repository.GetAllAsync();
            var entities = disciplines.Select(d => MapToEntity(d));
            return Ok(entities);
        }

        [EnableQuery]
        [RequirePermission(AuthConstants.Permissions.ReadDisciplines)]
        public async Task<IActionResult> Get([FromRoute] Guid key)
        {
            var discipline = await _repository.GetByIdAsync(key);
            if (discipline == null)
                return NotFound();

            return Ok(MapToEntity(discipline));
        }

        [EnableQuery]
        [RequirePermission(AuthConstants.Permissions.ReadDisciplines)]
        [HttpGet("odata/v1/Disciplines/GetByCode(code={code})")]
        public async Task<IActionResult> GetByCode([FromRoute] string code)
        {
            var discipline = await _repository.GetByCodeAsync(code);
            if (discipline == null)
                return NotFound();

            return Ok(MapToEntity(discipline));
        }

        [RequirePermission(AuthConstants.Permissions.WriteDisciplines)]
        public async Task<IActionResult> Post([FromBody] DisciplineEntity entity)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var discipline = new DISCIPLINE
            {
                GUID = entity.Guid,
                CODE = entity.Code,
                NAME = entity.Name ?? string.Empty
            };

            var result = await _repository.CreateAsync(discipline);
            return Created(MapToEntity(result));
        }

        [RequirePermission(AuthConstants.Permissions.WriteDisciplines)]
        public async Task<IActionResult> Put([FromRoute] Guid key, [FromBody] DisciplineEntity entity)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            if (key != entity.Guid)
                return BadRequest("The ID in the URL must match the ID in the request body");

            try
            {
                var discipline = new DISCIPLINE
                {
                    GUID = entity.Guid,
                    CODE = entity.Code,
                    NAME = entity.Name ?? string.Empty
                };

                var result = await _repository.UpdateAsync(discipline);
                return Updated(MapToEntity(result));
            }
            catch (KeyNotFoundException)
            {
                return NotFound();
            }
        }

        [RequirePermission(AuthConstants.Permissions.WriteDisciplines)]
        public async Task<IActionResult> Delete([FromRoute] Guid key, [FromBody] Guid deletedBy)
        {
            var result = await _repository.DeleteAsync(key, deletedBy);
            return result ? NoContent() : NotFound();
        }

        /// <summary>
        /// Partially updates a discipline
        /// </summary>
        /// <param name="key">The GUID of the discipline to update</param>
        /// <param name="delta">The discipline properties to update</param>
        /// <returns>The updated discipline</returns>
        [RequirePermission(AuthConstants.Permissions.WriteDisciplines)]
        public async Task<IActionResult> Patch([FromODataUri] Guid key, [FromBody] Delta<DisciplineEntity> delta)
        {
            try
            {
                _logger?.LogInformation($"Received PATCH request for discipline {key}");

                if (key == Guid.Empty)
                {
                    return BadRequest("Invalid GUID - The discipline ID cannot be empty");
                }

                if (delta == null)
                {
                    _logger?.LogWarning($"Update data is null for discipline {key}");
                    return BadRequest("Update data cannot be null. The request body must contain valid properties to update.");
                }

                // Get the existing discipline
                var existingDiscipline = await _repository.GetByIdAsync(key);
                if (existingDiscipline == null)
                {
                    return NotFound(new { error = "Not Found", message = $"Discipline with ID {key} was not found" });
                }

                // Create a copy of the entity to track changes
                var updatedEntity = MapToEntity(existingDiscipline);
                delta.CopyChangedValues(updatedEntity);

                // Map back to EF tracked DISCIPLINE entity
                existingDiscipline.CODE = updatedEntity.Code;
                existingDiscipline.NAME = updatedEntity.Name;

                var result = await _repository.UpdateAsync(existingDiscipline);
                return Updated(MapToEntity(result));
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error updating discipline");
                return StatusCode(500, new { error = "Internal Server Error", message = ex.Message });
            }
        }

        private static DisciplineEntity MapToEntity(DISCIPLINE discipline)
        {
            return new DisciplineEntity
            {
                Guid = discipline.GUID,
                Code = discipline.CODE,
                Name = discipline.NAME,
                Created = discipline.CREATED,
                CreatedBy = discipline.CREATEDBY,
                Updated = discipline.UPDATED,
                UpdatedBy = discipline.UPDATEDBY,
                Deleted = discipline.DELETED,
                DeletedBy = discipline.DELETEDBY
            };
        }
    }
}
