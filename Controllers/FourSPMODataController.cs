using FourSPM_WebService.Models.Shared;
using FourSPM_WebService.Models.Session;
using FourSPM_WebService.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OData.Routing.Controllers;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Security.Claims;

namespace FourSPM_WebService.Controllers;
public class FourSPMODataController : ODataController
{
    private ApplicationUser? _currentUser;
    private ApplicationUserProvider? _applicationUserProvider;
    
    /// <summary>
    /// Gets the current ApplicationUser for the request.
    /// This property creates the user directly from HttpContext claims when accessed.
    /// </summary>
    protected ApplicationUser CurrentUser
    {
        get
        {
            // Only create the user once per controller instance
            if (_currentUser != null)
            {
                return _currentUser;
            }
            
            // Create a basic user by default (for unauthenticated requests)
            _currentUser = new ApplicationUser();
            
            // Initialize ApplicationUserProvider if needed
            if (HttpContext?.RequestServices != null)
            {
                _applicationUserProvider ??= HttpContext.RequestServices.GetRequiredService<ApplicationUserProvider>();
            }
            
            // Only process authenticated requests
            if (HttpContext?.User?.Identity?.IsAuthenticated == true && _applicationUserProvider != null)
            {
                try
                {
                    // Get user ID from ApplicationUserProvider for proper DB mapping - we know _applicationUserProvider is not null here
                    var userId = _applicationUserProvider!.GetOrCreateUserFromClaimsAsync(HttpContext.User).GetAwaiter().GetResult();
                    
                    // Create ApplicationUser with the mapped user ID
                    if (userId != Guid.Empty && _currentUser != null)
                    {
                        _currentUser.UserId = userId;
                        _currentUser.UserName = HttpContext.User.FindFirst("name")?.Value ??
                                            HttpContext.User.FindFirst(ClaimTypes.Name)?.Value ??
                                            HttpContext.User.FindFirst("preferred_username")?.Value;
                        
                        _currentUser.Email = HttpContext.User.FindFirst("email")?.Value ??
                                         HttpContext.User.FindFirst(ClaimTypes.Email)?.Value;
                    }
                }
                catch (Exception ex)
                {
                    // Log error but don't throw - use default user instead
                    System.Diagnostics.Debug.WriteLine($"Error creating ApplicationUser from claims: {ex.Message}");
                }
            }
            
            // We already initialized _currentUser earlier, but add a null check to satisfy the compiler
            return _currentUser ?? new ApplicationUser();
        }
    }
    
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