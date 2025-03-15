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

namespace FourSPM_WebService.Controllers;

[Authorize]
[ODataRouteComponent("odata/v1")]
public class ClientsController : FourSPMODataController
{
    private readonly IClientRepository _clientRepository;
    private readonly ILogger<ClientsController> _logger;

    public ClientsController(IClientRepository clientRepository, ILogger<ClientsController> logger)
    {
        _clientRepository = clientRepository;
        _logger = logger;
    }

    /// <summary>
    /// Retrieves all clients
    /// </summary>
    /// <returns>A list of clients</returns>
    [EnableQuery]
    public IActionResult Get()
    {
        try
        {
            return Ok(_clientRepository.ClientQuery());
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>
    /// Retrieves a client by its GUID
    /// </summary>
    /// <param name="key">The GUID of the client to retrieve</param>
    /// <returns>The client with the specified GUID</returns>
    [EnableQuery]
    public IActionResult Get([FromRoute] Guid key)
    {
        try
        {
            _logger.LogInformation($"Fetching client with GUID: {key}");
            var client = _clientRepository.ClientQuery().FirstOrDefault(c => c.Guid == key);
            
            if (client == null)
            {
                _logger.LogWarning($"Client not found with GUID: {key}");
                return NotFound($"Client with GUID {key} not found");
            }

            _logger.LogInformation($"Successfully retrieved client: {client.Number}");
            return Ok(client);
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
    /// <param name="client">The client to create</param>
    /// <returns>The created client</returns>
    public async Task<IActionResult> Post([FromBody] ClientEntity client)
    {
        var result = await _clientRepository.CreateClient(client);
        return GetResult(result);
    }

    /// <summary>
    /// Deletes a client by its GUID
    /// </summary>
    /// <param name="key">The GUID of the client to delete</param>
    /// <returns>A success message if the client was deleted successfully</returns>
    public async Task<IActionResult> Delete([FromRoute] Guid key)
    {
        var result = await _clientRepository.DeleteClient(key);
        return GetResult(result);
    }

    /// <summary>
    /// Updates a client
    /// </summary>
    /// <param name="key">The GUID of the client to update</param>
    /// <param name="update">The client properties to update</param>
    /// <returns>The updated client</returns>
    public async Task<IActionResult> Put([FromRoute] Guid key, [FromBody] ClientEntity update)
    {
        if (key != update.Guid)
        {
            return BadRequest(new { error = "Route key and client GUID do not match" });
        }
        var result = await _clientRepository.UpdateClient(update);
        return GetResult(result);
    }

    /// <summary>
    /// Partially updates a client
    /// </summary>
    /// <param name="key">The GUID of the client to update</param>
    /// <param name="delta">The client properties to update</param>
    /// <returns>The updated client</returns>
    public async Task<IActionResult> Patch([FromRoute] Guid key, [FromBody] Delta<ClientEntity> delta)
    {
        try
        {
            _logger?.LogInformation($"Received PATCH request for client {key}");

            if (key == Guid.Empty)
            {
                return BadRequest(new { error = "Invalid GUID", message = "The client ID cannot be empty" });
            }

            if (delta == null)
            {
                _logger?.LogWarning($"Update data is null for client {key}");
                return BadRequest(new 
                { 
                    error = "Update data cannot be null",
                    message = "The request body must contain valid properties to update."
                });
            }

            // Get the client to update first
            var entity = await _clientRepository.ClientQuery()
                .FirstOrDefaultAsync(x => x.Guid == key);

            if (entity == null)
            {
                return NotFound(new { error = "Not Found", message = $"Client with ID {key} was not found" });
            }

            // Create a copy of the entity to track changes
            var updatedEntity = new ClientEntity();
            delta.CopyChangedValues(updatedEntity);

            // Save the changes
            var updateResult = await _clientRepository.UpdateClientByKey(
                key,
                e => {
                    foreach (var propName in delta.GetChangedPropertyNames())
                    {
                        var prop = typeof(ClientEntity).GetProperty(propName);
                        if (prop != null)
                        {
                            var value = prop.GetValue(updatedEntity);
                            prop.SetValue(e, value);
                        }
                    }
                }
            );

            return GetResult(updateResult);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error updating client");
            return StatusCode(500, new { error = "Internal Server Error", message = ex.Message });
        }
    }
}
