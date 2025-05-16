using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using FourSPM_WebService.Services;
using System.Security.Claims;
using FourSPM_WebService.Config;
using FourSPM_WebService.Authorization;

namespace FourSPM_WebService.Controllers
{
    /// <summary>
    /// Test controller to demonstrate token validation helpers
    /// This controller will be replaced or modified when implementing actual authentication features
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    public class AuthTestController : ControllerBase
    {
        private readonly ITokenValidationService _tokenService;
        private readonly ILogger<AuthTestController> _logger;

        public AuthTestController(ITokenValidationService tokenService, ILogger<AuthTestController> logger)
        {
            _tokenService = tokenService;
            _logger = logger;
        }

        /// <summary>
        /// Public endpoint that requires no authentication
        /// </summary>
        [HttpGet("public")]
        [AllowAnonymous]
        public IActionResult GetPublic()
        {
            return Ok(new { message = "This is a public endpoint that requires no authentication." });
        }

        /// <summary>
        /// Protected endpoint that requires authentication
        /// </summary>
        [HttpGet("protected")]
        [Authorize]
        public IActionResult GetProtected()
        {
            var userId = _tokenService.GetUserObjectId();
            var username = _tokenService.GetUsername();
            var displayName = _tokenService.GetUserDisplayName();
            var roles = _tokenService.GetUserRoles();

            return Ok(new
            {
                message = "You have accessed a protected endpoint!",
                user = new
                {
                    id = userId,
                    username = username,
                    displayName = displayName,
                    roles = roles
                }
            });
        }

        /// <summary>
        /// Admin endpoint that requires admin permissions
        /// </summary>
        [HttpGet("admin")]
        [RequirePermission(AuthConstants.Permissions.AdminAccess)]
        public IActionResult GetAdmin()
        {
            return Ok(new { message = "You have admin access!" });
        }

        /// <summary>
        /// Project manager endpoint that requires project write permissions
        /// </summary>
        [HttpGet("project-manager")]
        [RequirePermission(AuthConstants.Permissions.WriteProjects)]
        public IActionResult GetProjectManager()
        {
            return Ok(new { message = "You have project manager access!" });
        }

        /// <summary>
        /// User endpoint that requires project read permissions
        /// </summary>
        [HttpGet("user")]
        [RequirePermission(AuthConstants.Permissions.ReadProjects)]
        public IActionResult GetUser()
        {
            return Ok(new { message = "You have user access!" });
        }

        /// <summary>
        /// Reader endpoint that requires basic read permissions
        /// </summary>
        [HttpGet("reader")]
        [RequirePermission(AuthConstants.Permissions.ReadProjects)]
        public IActionResult GetReader()
        {
            return Ok(new { message = "You have reader access!" });
        }

        /// <summary>
        /// Endpoint that requires the permission to read projects (using permission-based authorization)
        /// </summary>
        [HttpGet("permission/view-projects")]
        [RequirePermission(AuthConstants.Permissions.ReadProjects)]
        public IActionResult GetWithViewProjectsPermission()
        {
            return Ok(new { message = "You have permission to view projects!" });
        }

        /// <summary>
        /// Endpoint that requires the permission to write projects (using permission-based authorization)
        /// </summary>
        [HttpGet("permission/edit-projects")]
        [RequirePermission(AuthConstants.Permissions.WriteProjects)]
        public IActionResult GetWithEditProjectsPermission()
        {
            return Ok(new { message = "You have permission to edit projects!" });
        }

        /// <summary>
        /// Endpoint that requires the permission to write roles (using permission-based authorization)
        /// </summary>
        [HttpGet("permission/manage-roles")]
        [RequirePermission(AuthConstants.Permissions.WriteRoles)]
        public IActionResult GetWithManageRolesPermission()
        {
            return Ok(new { message = "You have permission to manage roles!" });
        }

        /// <summary>
        /// Get all claims in the current token
        /// </summary>
        [HttpGet("claims")]
        [Authorize]
        public IActionResult GetClaims()
        {
            var claims = _tokenService.GetAllClaims()
                .Select(c => new { type = c.Type, value = c.Value });

            return Ok(new { claims });
        }
    }
}
