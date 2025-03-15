using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OData.Query;
using Microsoft.AspNetCore.OData.Deltas;
using Microsoft.AspNetCore.Authorization;
using FourSPM_WebService.Data.Interfaces;
using FourSPM_WebService.Data.OData.FourSPM;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.OData.Routing.Attributes;
using FourSPM_WebService.Data.EF.FourSPM;
using Microsoft.AspNetCore.OData.Formatter;
using FourSPM_WebService.Models.Session;

namespace FourSPM_WebService.Controllers;

[Authorize]
[ODataRouteComponent("odata/v1")]
public class ClientsController : FourSPMODataController
{
    private readonly IClientRepository _clientRepository;
    private readonly ILogger<ClientsController> _logger;
    private readonly FourSPMContext _context;
    private readonly ApplicationUser _user;

    public ClientsController(IClientRepository clientRepository, ILogger<ClientsController> logger, FourSPMContext context, ApplicationUser user)
    {
        _clientRepository = clientRepository;
        _logger = logger;
        _context = context;
        _user = user;
    }

    /// <summary>
    /// Retrieves all clients
    /// </summary>
    /// <returns>A list of clients</returns>
    [EnableQuery]
    public async Task<IActionResult> Get()
    {
        try
        {
            var clients = await _clientRepository.GetAllAsync();
            var entities = clients.Select(c => MapToEntity(c));
            return Ok(entities);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving clients");
            return StatusCode(500, ex.Message);
        }
    }

    /// <summary>
    /// Retrieves a client by its GUID
    /// </summary>
    /// <param name="key">The GUID of the client to retrieve</param>
    /// <returns>The client with the specified GUID</returns>
    [EnableQuery]
    public async Task<IActionResult> Get([FromRoute] Guid key)
    {
        try
        {
            _logger.LogInformation($"Fetching client with GUID: {key}");
            var client = await _clientRepository.GetByIdAsync(key);
            
            if (client == null)
            {
                _logger.LogWarning($"Client not found with GUID: {key}");
                return NotFound($"Client with GUID {key} not found");
            }

            _logger.LogInformation($"Successfully retrieved client: {client.NUMBER}");
            return Ok(MapToEntity(client));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error retrieving client with GUID: {key}");
            return StatusCode(500, "Internal server error occurred while retrieving the client");
        }
    }

    /// <summary>
    /// Creates a new client
    /// </summary>
    /// <param name="entity">The client to create</param>
    /// <returns>The created client</returns>
    public async Task<IActionResult> Post([FromBody] ClientEntity entity)
    {
        try
        {
            // Check if client number is unique
            if (!await IsClientNumberUnique(entity.Number, null))
            {
                return BadRequest($"A client with number '{entity.Number}' already exists.");
            }

            // Map to database entity
            var clientToCreate = new CLIENT
            {
                GUID = entity.Guid == Guid.Empty ? Guid.NewGuid() : entity.Guid,
                NUMBER = entity.Number,
                DESCRIPTION = entity.Description,
                CLIENT_CONTACT_NAME = entity.ClientContactName,
                CLIENT_CONTACT_NUMBER = entity.ClientContactNumber,
                CLIENT_CONTACT_EMAIL = entity.ClientContactEmail
            };

            // Use the new CreateAsync method
            var createdClient = await _clientRepository.CreateAsync(clientToCreate);
            return Created($"odata/v1/Clients/{createdClient.GUID}", MapToEntity(createdClient));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating client");
            return StatusCode(500, ex.Message);
        }
    }

    /// <summary>
    /// Deletes a client by its GUID
    /// </summary>
    /// <param name="key">The GUID of the client to delete</param>
    /// <returns>A success message if the client was deleted successfully</returns>
    public async Task<IActionResult> Delete([FromODataUri] Guid key)
    {
        try
        {
            // Use the new DeleteAsync method
            if (await _clientRepository.DeleteAsync(key, _user.UserId!.Value))
            {
                return NoContent();
            }
            else
            {
                return NotFound($"Client with ID {key} not found");
            }
        }
        catch (InvalidOperationException ex)
        {
            // Return a specific error if client has associated projects
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error deleting client with ID {key}");
            return StatusCode(500, ex.Message);
        }
    }

    /// <summary>
    /// Updates a client
    /// </summary>
    /// <param name="key">The GUID of the client to update</param>
    /// <param name="update">The client properties to update</param>
    /// <returns>The updated client</returns>
    public async Task<IActionResult> Put([FromODataUri] Guid key, [FromBody] ClientEntity update)
    {
        try
        {
            if (key != update.Guid)
            {
                return BadRequest("Route key and client GUID do not match");
            }
            
            // Check if client number is unique (excluding the current client)
            if (!await IsClientNumberUnique(update.Number, key))
            {
                return BadRequest($"A client with number '{update.Number}' already exists.");
            }

            // Get the existing client to preserve created info and other fields
            var existingClient = await _clientRepository.GetByIdAsync(key);
            if (existingClient == null)
            {
                return NotFound($"Client with ID {key} not found");
            }
            
            // Map to database entity
            var clientToUpdate = new CLIENT
            {
                GUID = update.Guid,
                NUMBER = update.Number,
                DESCRIPTION = update.Description,
                CLIENT_CONTACT_NAME = update.ClientContactName,
                CLIENT_CONTACT_NUMBER = update.ClientContactNumber,
                CLIENT_CONTACT_EMAIL = update.ClientContactEmail
                // Created, Updated, and Deleted info will be handled by the repository
            };

            // Use the new UpdateAsync method
            var updatedClient = await _clientRepository.UpdateAsync(clientToUpdate);
            return Ok(MapToEntity(updatedClient));
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error updating client with ID {key}");
            return StatusCode(500, ex.Message);
        }
    }

    /// <summary>
    /// Partially updates a client
    /// </summary>
    /// <param name="key">The GUID of the client to update</param>
    /// <param name="delta">The client properties to update</param>
    /// <returns>The updated client</returns>
    public async Task<IActionResult> Patch([FromODataUri] Guid key, [FromBody] Delta<ClientEntity> delta)
    {
        try
        {
            _logger?.LogInformation($"Received PATCH request for client {key}");

            if (key == Guid.Empty)
            {
                return BadRequest("The client ID cannot be empty");
            }

            if (delta == null)
            {
                _logger?.LogWarning($"Update data is null for client {key}");
                return BadRequest("The request body must contain valid properties to update.");
            }

            // Get the existing client
            var existingClient = await _clientRepository.GetByIdAsync(key);
            if (existingClient == null)
            {
                return NotFound($"Client with ID {key} was not found");
            }

            // Create a copy of the entity to track changes
            var updatedEntity = MapToEntity(existingClient);
            delta.CopyChangedValues(updatedEntity);
            
            // Check if number is being changed and is unique
            if (delta.GetChangedPropertyNames().Contains("Number") && 
                !await IsClientNumberUnique(updatedEntity.Number, key))
            {
                return BadRequest($"A client with number '{updatedEntity.Number}' already exists.");
            }

            // Map back to CLIENT entity
            var clientToUpdate = new CLIENT
            {
                GUID = updatedEntity.Guid,
                NUMBER = updatedEntity.Number,
                DESCRIPTION = updatedEntity.Description,
                CLIENT_CONTACT_NAME = updatedEntity.ClientContactName,
                CLIENT_CONTACT_NUMBER = updatedEntity.ClientContactNumber,
                CLIENT_CONTACT_EMAIL = updatedEntity.ClientContactEmail,
                CREATED = existingClient.CREATED,
                CREATEDBY = existingClient.CREATEDBY,
                UPDATED = existingClient.UPDATED,
                UPDATEDBY = existingClient.UPDATEDBY,
                DELETED = existingClient.DELETED,
                DELETEDBY = existingClient.DELETEDBY
            };

            // Use the new UpdateAsync method instead of UpdateClient
            var updatedClient = await _clientRepository.UpdateAsync(clientToUpdate);
            return Ok(MapToEntity(updatedClient));
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error updating client");
            return StatusCode(500, ex.Message);
        }
    }
    
    /// <summary>
    /// Checks if a client number is unique in the database
    /// </summary>
    /// <param name="clientNumber">The client number to check</param>
    /// <param name="excludeClientGuid">Optional GUID of the client to exclude from the check (for updates)</param>
    /// <returns>True if the client number is unique, false otherwise</returns>
    private async Task<bool> IsClientNumberUnique(string clientNumber, Guid? excludeClientGuid)
    {
        if (string.IsNullOrEmpty(clientNumber))
            return true; // Empty client numbers are handled by model validation
            
        var query = _context.CLIENTs
            .Where(c => c.NUMBER == clientNumber && c.DELETED == null);
            
        // If we're updating an existing client, exclude it from the uniqueness check
        if (excludeClientGuid.HasValue)
        {
            query = query.Where(c => c.GUID != excludeClientGuid.Value);
        }
        
        return await query.CountAsync() == 0;
    }
    
    /// <summary>
    /// Maps a CLIENT database entity to a ClientEntity OData entity
    /// </summary>
    /// <param name="client">The CLIENT database entity</param>
    /// <returns>A ClientEntity OData entity</returns>
    private ClientEntity MapToEntity(CLIENT client)
    {
        return new ClientEntity
        {
            Guid = client.GUID,
            Number = client.NUMBER,
            Description = client.DESCRIPTION,
            ClientContactName = client.CLIENT_CONTACT_NAME,
            ClientContactNumber = client.CLIENT_CONTACT_NUMBER,
            ClientContactEmail = client.CLIENT_CONTACT_EMAIL,
            Created = client.CREATED,
            CreatedBy = client.CREATEDBY,
            Updated = client.UPDATED,
            UpdatedBy = client.UPDATEDBY,
            Deleted = client.DELETED,
            DeletedBy = client.DELETEDBY
        };
    }
}
