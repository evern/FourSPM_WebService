using FourSPM_WebService.Models.Shared;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OData.Routing.Controllers;

namespace FourSPM_WebService.Controllers;
public class FourSPMODataController : ODataController
{
    protected IActionResult GetResult<TResult>(OperationResult<TResult> result)
    {
        switch (result.Status)
        {
            case OperationStatus.NoAccess:
                return Unauthorized(result.Message);

            case OperationStatus.NotFound:
                return NotFound(result.Message);

            case OperationStatus.Updated:
                return Updated(result.Result);

            case OperationStatus.Created:
                return Created(result.Result);

            case OperationStatus.Validation:
                return BadRequest(result.Message);

            default:
                return result.Result == null ? NoContent() : Ok(result.Result);
        }
    }

    protected IActionResult GetResult(OperationResult result)
    {
        switch (result.Status)
        {
            case OperationStatus.NoAccess:
                return Unauthorized(result.Message);

            case OperationStatus.NotFound:
                return NotFound(result.Message);

            case OperationStatus.Validation:
                return BadRequest(result.Message);

            default:
                return string.IsNullOrEmpty(result.Message) ? NoContent() : Ok(result.Message);
        }
    }
}