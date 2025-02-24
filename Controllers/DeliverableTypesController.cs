using FourSPM_WebService.Data.EF.FourSPM;
using FourSPM_WebService.Data.OData.FourSPM;
using FourSPM_WebService.Data.Repositories;
using FourSPM_WebService.Models.Shared;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OData.Query;
using Microsoft.AspNetCore.OData.Routing.Controllers;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace FourSPM_WebService.Controllers
{
    public class DeliverableTypesController : FourSPMODataController
    {
        private readonly IDeliverableTypeRepository _repository;

        public DeliverableTypesController(IDeliverableTypeRepository repository)
        {
            _repository = repository;
        }

        [EnableQuery]
        public async Task<IActionResult> Get()
        {
            var types = await _repository.GetAllAsync();
            var entities = types.Select(t => MapToEntity(t));
            return Ok(entities);
        }

        [EnableQuery]
        public async Task<IActionResult> Get([FromRoute] Guid key)
        {
            var type = await _repository.GetByIdAsync(key);
            if (type == null)
                return NotFound();

            return Ok(MapToEntity(type));
        }

        public async Task<IActionResult> Post([FromBody] DeliverableTypeEntity entity)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var type = new DELIVERABLE_TYPE
            {
                NAME = entity.Name,
                DESCRIPTION = entity.Description,
                CREATEDBY = entity.CreatedBy
            };

            var result = await _repository.CreateAsync(type);
            return Created(MapToEntity(result));
        }

        public async Task<IActionResult> Put([FromRoute] Guid key, [FromBody] DeliverableTypeEntity entity)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            if (key != entity.Guid)
                return BadRequest("The ID in the URL must match the ID in the request body");

            try
            {
                var type = new DELIVERABLE_TYPE
                {
                    GUID = entity.Guid,
                    NAME = entity.Name,
                    DESCRIPTION = entity.Description,
                    UPDATEDBY = entity.UpdatedBy
                };

                var result = await _repository.UpdateAsync(type);
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

        private static DeliverableTypeEntity MapToEntity(DELIVERABLE_TYPE type)
        {
            return new DeliverableTypeEntity
            {
                Guid = type.GUID,
                Name = type.NAME,
                Description = type.DESCRIPTION,
                Created = type.CREATED,
                CreatedBy = type.CREATEDBY,
                Updated = type.UPDATED,
                UpdatedBy = type.UPDATEDBY,
                Deleted = type.DELETED,
                DeletedBy = type.DELETEDBY
            };
        }
    }
}
