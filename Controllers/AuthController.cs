using FourSPM_WebService.Data.EF.FourSPM;
using FourSPM_WebService.Models.Session;
using FourSPM_WebService.Services;
using FourSPM_WebService.Helpers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace FourSPM_WebService.Controllers.Auth.Controller;

[ApiController]
[Route("api/auth")]
[AllowAnonymous]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly FourSPMContext _context;
    private readonly ILogger<AuthController> _logger;

    public AuthController(
        IAuthService authService,
        FourSPMContext context,
        ILogger<AuthController> logger)
    {
        _authService = authService;
        _context = context;
        _logger = logger;
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        try
        {
            var user = await _context.USERs
                .Where(u => u.USERNAME == request.Email && u.DELETED == null)
                .FirstOrDefaultAsync();

            if (user == null)
            {
                _logger.LogWarning("Failed login attempt for user: {Username}", request.Email);
                return Unauthorized("Invalid username or password");
            }

            if (string.IsNullOrEmpty(request.Password))
            {
                _logger.LogWarning("Empty password attempt for user: {Username}", request.Email);
                return Unauthorized("Invalid username or password");
            }

            if (!_authService.VerifyPassword(request.Password, user.PASSWORD))
            {
                _logger.LogWarning("Failed login attempt for user: {Username}", request.Email);
                return Unauthorized("Invalid username or password");
            }

            var token = _authService.GenerateJwtToken(user);

            // Set the token as a cookie with the following attributes:
            Response.Cookies.Append("token", token, new CookieOptions
            {
                HttpOnly = true, // Prevents JavaScript access to the cookie for security reasons
                Secure = true,   // Ensures the cookie is only sent over HTTPS connections
                SameSite = SameSiteMode.None, // Allows the cookie to be sent with cross-origin requests
                Expires = DateTime.UtcNow.AddDays(7), // Sets the cookie to expire in 7 days
                Path = "/", // The cookie is valid for all paths on the domain
            });

            return Ok(new 
            { 
                token,
                user = new
                {
                    user.GUID,
                    user.USERNAME,
                    user.FIRST_NAME,
                    user.LAST_NAME
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during login for user: {Username}", request.Email);
            return StatusCode(500, "An error occurred during login");
        }
    }

    [HttpPost("create")]
    public async Task<IActionResult> CreateAccount([FromBody] LoginRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            // Check if user already exists
            var existingUser = await _context.USERs
                .Where(u => u.USERNAME == request.Email && u.DELETED == null)
                .FirstOrDefaultAsync();

            if (existingUser != null)
            {
                _logger.LogWarning("Account creation attempt for existing user: {Username}", request.Email);
                return BadRequest("User already exists");
            }

            // Create new user
            var newUser = new USER
            {
                GUID = Guid.NewGuid(),
                USERNAME = request.Email,
                PASSWORD = _authService.HashPassword(request.Password), // Password is now guaranteed to be non-null
                FIRST_NAME = request.FirstName,
                LAST_NAME = request.LastName,
                CREATED = DateTime.UtcNow,
                UPDATED = DateTime.UtcNow
            };

            _context.USERs.Add(newUser);
            await _context.SaveChangesAsync();

            // Generate token for the new user
            var token = _authService.GenerateJwtToken(newUser);

            // Set the token cookie
            Response.Cookies.Append("token", token, new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.None,
                Expires = DateTime.UtcNow.AddDays(7),
                Path = "/"
            });

            return Ok(new
            {
                token,
                user = new
                {
                    newUser.GUID,
                    newUser.USERNAME,
                    newUser.FIRST_NAME,
                    newUser.LAST_NAME
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during account creation for user: {Username}", request.Email);
            return StatusCode(500, "An error occurred during account creation");
        }
    }

    [HttpPost("logout")]
    public IActionResult Logout()
    {
        try
        {
            // Clear the JWT token cookie by setting it to expire immediately
            Response.Cookies.Delete("token", new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.None,
                Path = "/"
            });

            return Ok(new { message = "Logged out successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during logout");
            return StatusCode(500, "An error occurred during logout");
        }
    }

    [HttpPost("reset-password")]
    [AllowAnonymous]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest request)
    {
        try
        {
            _logger.LogInformation("Reset password request received for email: {Email}", request.Email);

            if (!ModelState.IsValid)
            {
                _logger.LogWarning("Invalid model state for reset password request");
                return BadRequest(ModelState);
            }

            // Find user by email
            var user = await _context.USERs
                .Where(u => u.USERNAME == request.Email && u.DELETED == null)
                .FirstOrDefaultAsync();

            if (user == null)
            {
                _logger.LogInformation("User not found for email: {Email}", request.Email);
                // Return success even if user not found to prevent email enumeration
                return Ok(new { message = "If the email exists, a password reset link has been sent." });
            }

            _logger.LogInformation("User found, generating reset token for: {Email}", request.Email);
            // Generate password reset token
            var resetToken = _authService.GeneratePasswordResetToken(user);

            _logger.LogInformation("Reset token generated successfully for: {Email}", request.Email);
            // TODO: Send email with reset token
            // For now, we'll just return the token in the response
            // In production, this should send an email with a link containing the token
            return Ok(new { message = "If the email exists, a password reset link has been sent.", resetToken });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during password reset for email: {Email}", request.Email);
            return StatusCode(500, "An error occurred during password reset");
        }
    }

    [HttpPost("change-password")]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            if (string.IsNullOrEmpty(request.CurrentPassword))
            {
                return BadRequest(new { message = "Current password is required" });
            }

            // Get user ID from claims
            var userIdClaim = HttpContext.User.Claims.FirstOrDefault(x => x.Type == ClaimTypes.NameIdentifier);
            if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out var userId))
            {
                return Unauthorized(new { message = "Invalid token" });
            }

            var user = await _context.USERs
                .Where(u => u.GUID == userId && u.DELETED == null)
                .FirstOrDefaultAsync();

            if (user == null)
            {
                return NotFound(new { message = "User not found" });
            }

            // Verify current password
            if (!_authService.VerifyPassword(request.CurrentPassword, user.PASSWORD))
            {
                return BadRequest(new { message = "Current password is incorrect" });
            }

            // Update password
            user.PASSWORD = _authService.HashPassword(request.Password);
            user.UPDATED = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return Ok(new { message = "Password changed successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during password change");
            return StatusCode(500, "An error occurred while changing the password");
        }
    }
}