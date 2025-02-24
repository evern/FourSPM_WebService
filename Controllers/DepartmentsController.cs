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
    public class DepartmentsController : FourSPMODataController
    {
        private readonly IDepartmentRepository _repository;

        public DepartmentsController(IDepartmentRepository repository)
        {
            _repository = repository;
        }

        [EnableQuery]
        public async Task<IActionResult> Get()
        {
            var departments = await _repository.GetAllAsync();
            var entities = departments.Select(d => MapToEntity(d));
            return Ok(entities);
        }

        [EnableQuery]
        public async Task<IActionResult> Get([FromRoute] Guid key)
        {
            var department = await _repository.GetByIdAsync(key);
            if (department == null)
                return NotFound();

            return Ok(MapToEntity(department));
        }

        public async Task<IActionResult> Post([FromBody] DepartmentEntity entity)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var department = new DEPARTMENT
            {
                NAME = entity.Name,
                DESCRIPTION = entity.Description,
                CREATEDBY = entity.CreatedBy
            };

            var result = await _repository.CreateAsync(department);
            return Created(MapToEntity(result));
        }

        public async Task<IActionResult> Put([FromRoute] Guid key, [FromBody] DepartmentEntity entity)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            if (key != entity.Guid)
                return BadRequest("The ID in the URL must match the ID in the request body");

            try
            {
                var department = new DEPARTMENT
                {
                    GUID = entity.Guid,
                    NAME = entity.Name,
                    DESCRIPTION = entity.Description,
                    UPDATEDBY = entity.UpdatedBy
                };

                var result = await _repository.UpdateAsync(department);
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

        private static DepartmentEntity MapToEntity(DEPARTMENT department)
        {
            return new DepartmentEntity
            {
                Guid = department.GUID,
                Name = department.NAME,
                Description = department.DESCRIPTION,
                Created = department.CREATED,
                CreatedBy = department.CREATEDBY,
                Updated = department.UPDATED,
                UpdatedBy = department.UPDATEDBY,
                Deleted = department.DELETED,
                DeletedBy = department.DELETEDBY
            };
        }
    }
}
