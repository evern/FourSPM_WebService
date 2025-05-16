using FourSPM_WebService.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FourSPM_WebService.Controllers
{
    /// <summary>
    /// Controller for testing permission-based authorization
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    public class PermissionTestController : ControllerBase
    {
        private readonly ILogger<PermissionTestController> _logger;

        /// <summary>
        /// Initializes a new instance of <see cref="PermissionTestController"/>
        /// </summary>
        public PermissionTestController(ILogger<PermissionTestController> logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// Public endpoint that can be accessed without authentication
        /// </summary>
        [HttpGet("public")]
        [AllowAnonymous]
        public IActionResult PublicEndpoint()
        {
            return Ok(new { message = "This is a public endpoint that can be accessed without authentication" });
        }

        /// <summary>
        /// Endpoint that requires authentication but no specific permission
        /// </summary>
        [HttpGet("authenticated")]
        [Authorize]
        public IActionResult AuthenticatedEndpoint()
        {
            var userName = User.Identity?.Name ?? "unknown";
            return Ok(new { message = $"Hello, {userName}! This endpoint requires authentication but no specific permission." });
        }

        /// <summary>
        /// Endpoint that requires the Projects.View permission
        /// </summary>
        [HttpGet("projects/view")]
        [RequirePermission(Permissions.ViewProjects)]
        public IActionResult ViewProjects()
        {
            return Ok(new { message = "You have the Projects.View permission" });
        }

        /// <summary>
        /// Endpoint that requires the Projects.Edit permission
        /// </summary>
        [HttpGet("projects/edit")]
        [RequirePermission(Permissions.EditProjects)]
        public IActionResult EditProjects()
        {
            return Ok(new { message = "You have the Projects.Edit permission" });
        }

        /// <summary>
        /// Endpoint that requires the Roles.View permission
        /// </summary>
        [HttpGet("roles/view")]
        [RequirePermission(Permissions.ViewRoles)]
        public IActionResult ViewRoles()
        {
            return Ok(new { message = "You have the Roles.View permission" });
        }

        /// <summary>
        /// Endpoint that requires the Roles.Edit permission
        /// </summary>
        [HttpGet("roles/edit")]
        [RequirePermission(Permissions.EditRoles)]
        public IActionResult EditRoles()
        {
            return Ok(new { message = "You have the Roles.Edit permission" });
        }

        /// <summary>
        /// Endpoint that requires the administrator role
        /// </summary>
        [HttpGet("admin")]
        [Authorize(Policy = "RequireAdminRole")]
        public IActionResult AdminEndpoint()
        {
            return Ok(new { message = "You have the Administrator role" });
        }

        /// <summary>
        /// Endpoint that returns all claims for the current user
        /// </summary>
        [HttpGet("claims")]
        [Authorize]
        public IActionResult GetClaims()
        {
            var claims = User.Claims.Select(c => new { c.Type, c.Value }).ToList();
            return Ok(new { claims });
        }
    }
}
