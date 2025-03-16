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
            _logger.LogInformation("Login attempt for user: {Username}", request.Email);
            
            if (request == null)
            {
                _logger.LogError("Login request body is null");
                return BadRequest("Invalid request format");
            }

            _logger.LogInformation("Attempting to find user in database");
            var user = await _context.USERs
                .Where(u => u.USERNAME == request.Email && u.DELETED == null)
                .FirstOrDefaultAsync();

            if (user == null)
            {
                _logger.LogWarning("User not found: {Username}", request.Email);
                return Unauthorized("Invalid username or password");
            }

            if (string.IsNullOrEmpty(request.Password))
            {
                _logger.LogWarning("Empty password provided");
                return BadRequest("Password is required");
            }

            try
            {
                if (!_authService.VerifyPassword(request.Password, user.PASSWORD))
                {
                    _logger.LogWarning("Invalid password for user: {Username}", request.Email);
                    return Unauthorized("Invalid username or password");
                }

                var token = _authService.GenerateJwtToken(user);
                _logger.LogInformation("Login successful for user: {Username}", request.Email);
                
                // Set the token as a cookie
                Response.Cookies.Append("token", token, new CookieOptions
                {
                    HttpOnly = true,
                    Secure = true,
                    SameSite = SameSiteMode.None,
                    Expires = DateTime.UtcNow.AddDays(7),
                    Path = "/"
                });

                return Ok(new { 
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
            catch (Exception authEx)
            {
                _logger.LogError(authEx, "Error during authentication for user: {Username}", request.Email);
                return StatusCode(500, $"Authentication error: {authEx.Message}");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled error during login");
            return StatusCode(500, $"An error occurred: {ex.Message}");
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
                return BadRequest("Current password is required");
            }

            // Get user ID from claims
            var userIdClaim = HttpContext.User.Claims.FirstOrDefault(x => x.Type == ClaimTypes.NameIdentifier);
            if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out var userId))
            {
                return Unauthorized("Invalid token");
            }

            var user = await _context.USERs
                .Where(u => u.GUID == userId && u.DELETED == null)
                .FirstOrDefaultAsync();

            if (user == null)
            {
                return NotFound("User not found");
            }

            // Verify current password
            if (!_authService.VerifyPassword(request.CurrentPassword, user.PASSWORD))
            {
                return BadRequest("Current password is incorrect");
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