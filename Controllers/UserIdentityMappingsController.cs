using System;
using System.Linq;
using System.Threading.Tasks;
using FourSPM_WebService.Data.EF.FourSPM;
using FourSPM_WebService.Data.Interfaces;
using FourSPM_WebService.Data.OData.FourSPM;
using FourSPM_WebService.Models.Session;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OData.Query;
using Microsoft.AspNetCore.OData.Routing.Attributes;
using Microsoft.Extensions.Logging;

namespace FourSPM_WebService.Controllers
{
    [Authorize]
    [ODataRouteComponent("odata/v1")]
    public class UserIdentityMappingsController : FourSPMODataController
    {
        private readonly IUserIdentityMappingRepository _repository;
        private readonly ILogger<UserIdentityMappingsController> _logger;

        public UserIdentityMappingsController(
            IUserIdentityMappingRepository repository,
            ILogger<UserIdentityMappingsController> logger)
        {
            _repository = repository;
            _logger = logger;
        }

        [EnableQuery]
        public async Task<IActionResult> Get()
        {
            var mappings = await _repository.GetAllAsync();
            var entities = mappings.Select(m => MapToEntity(m));
            return Ok(entities);
        }

        [EnableQuery]
        public async Task<IActionResult> Get([FromRoute] Guid key)
        {
            var mapping = await _repository.GetByIdAsync(key);
            if (mapping == null)
                return NotFound();

            return Ok(MapToEntity(mapping));
        }

        [HttpPost]
        public async Task<IActionResult> Post([FromBody] UserIdentityMappingEntity entity)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {
                // Check if a mapping with the same email already exists
                if (await _repository.ExistsByEmailAsync(entity.Email))
                {
                    return Conflict($"A user identity mapping with email {entity.Email} already exists");
                }

                var mapping = new USER_IDENTITY_MAPPING
                {
                    GUID = Guid.NewGuid(),
                    USERNAME = entity.Username,
                    EMAIL = entity.Email,
                    CREATED = DateTime.Now,
                    LAST_LOGIN = DateTime.Now
                };

                var createdMapping = await _repository.CreateAsync(mapping);
                return Created(createdMapping.GUID.ToString(), MapToEntity(createdMapping));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating user identity mapping");
                return StatusCode(500, "An error occurred while creating the user identity mapping");
            }
        }

        [HttpPut]
        public async Task<IActionResult> Put([FromRoute] Guid key, [FromBody] UserIdentityMappingEntity entity)
        {
            if (key != entity.Guid)
            {
                return BadRequest("The ID in the URL does not match the ID in the request body");
            }

            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {
                // Check if the mapping exists
                var existingMapping = await _repository.GetByIdAsync(key);
                if (existingMapping == null)
                {
                    return NotFound();
                }

                // Check if trying to update to an email that already exists for another user
                var duplicateMapping = await _repository.GetByEmailAsync(entity.Email);
                if (duplicateMapping != null && duplicateMapping.GUID != key)
                {
                    return Conflict($"Another user identity mapping with email {entity.Email} already exists");
                }

                // Update the mapping
                existingMapping.USERNAME = entity.Username;
                existingMapping.EMAIL = entity.Email;

                var updatedMapping = await _repository.UpdateAsync(existingMapping);
                return Ok(MapToEntity(updatedMapping));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating user identity mapping");
                return StatusCode(500, "An error occurred while updating the user identity mapping");
            }
        }

        [HttpDelete]
        public async Task<IActionResult> Delete([FromRoute] Guid key)
        {
            try
            {
                var mapping = await _repository.GetByIdAsync(key);
                if (mapping == null)
                {
                    return NotFound();
                }

                await _repository.DeleteAsync(key, CurrentUser.UserId ?? Guid.Empty);
                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting user identity mapping");
                return StatusCode(500, "An error occurred while deleting the user identity mapping");
            }
        }

        [HttpGet("ByEmail/{email}")]
        public async Task<IActionResult> GetByEmail(string email)
        {
            var mapping = await _repository.GetByEmailAsync(email);
            if (mapping == null)
                return NotFound();

            return Ok(MapToEntity(mapping));
        }

        [HttpPut("{id}/UpdateLastLogin")]
        public async Task<IActionResult> UpdateLastLogin(Guid id)
        {
            try
            {
                var mapping = await _repository.GetByIdAsync(id);
                if (mapping == null)
                {
                    return NotFound();
                }

                await _repository.UpdateLastLoginAsync(id);
                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating last login timestamp");
                return StatusCode(500, "An error occurred while updating the last login timestamp");
            }
        }

        private static UserIdentityMappingEntity MapToEntity(USER_IDENTITY_MAPPING mapping)
        {
            if (mapping == null) return null!;
            
            return new UserIdentityMappingEntity
            {
                Guid = mapping.GUID,
                Username = mapping.USERNAME,
                Email = mapping.EMAIL,
                Created = mapping.CREATED,
                LastLogin = mapping.LAST_LOGIN,
                Deleted = mapping.DELETED,
                DeletedBy = mapping.DELETEDBY
            };
        }
    }
}
